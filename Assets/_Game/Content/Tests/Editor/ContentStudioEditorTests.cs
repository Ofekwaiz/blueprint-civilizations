using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.Editor.ContentStudio;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.Tests
{
    public sealed class ContentStudioEditorTests
    {
        private const string SpiderPath = "Assets/_Game/Content/Assets/Units/Hive/Unit_Hive_Spider.asset";
        private const string BeetlePath = "Assets/_Game/Content/Assets/Units/Hive/Unit_Hive_Beetle.asset";
        private const string TemporaryRoot = "Assets/_Game/Content/Assets/Units/Custom";

        [TearDown]
        public void TearDown()
        {
            Selection.activeObject = null;
            foreach (string folder in AssetDatabase.GetSubFolders(TemporaryRoot)
                         .Where(path => path.Substring(path.LastIndexOf('/') + 1)
                             .StartsWith("__ContentStudioTests_", StringComparison.Ordinal)))
                AssetDatabase.DeleteAsset(folder);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }

        [Test]
        public void SelectingUnitDefinition_CreatesEveryNonEmptyBoundSection()
        {
            var window = CreateWindow();
            try
            {
                var spider = AssetDatabase.LoadAssetAtPath<UnitDefinition>(SpiderPath);
                Select(window, spider);

                ScrollView details = Details(window);
                List<Foldout> sections = details.Query<Foldout>(className: UnitDefinitionEditorBuilder.SectionClass).ToList();
                Assert.That(sections.Select(section => section.text),
                    Is.EquivalentTo(UnitDefinitionEditorBuilder.Sections.Select(section => section.Title)));
                foreach (Foldout section in sections)
                {
                    Assert.That(section.Query<PropertyField>(className: UnitDefinitionEditorBuilder.BoundFieldClass).ToList(),
                        Is.Not.Empty, $"Section '{section.text}' did not construct any bound fields.");
                }
            }
            finally { Close(window); }
        }

        [Test]
        public void UnitDefinitionEditorSchema_EveryExpectedPropertyPathResolves()
        {
            var spider = AssetDatabase.LoadAssetAtPath<UnitDefinition>(SpiderPath);
            var serialized = new SerializedObject(spider);

            foreach (string path in UnitDefinitionEditorBuilder.Sections.SelectMany(section => section.PropertyPaths))
                Assert.That(serialized.FindProperty(path), Is.Not.Null, path);
        }

        [Test]
        public void UnitDefinitionEditorSchema_ProductionAndCombatNestedPathsResolve()
        {
            var spider = AssetDatabase.LoadAssetAtPath<UnitDefinition>(SpiderPath);
            var serialized = new SerializedObject(spider);
            string[] nestedPaths =
            {
                "productionStats.spawnInterval", "productionStats.initialSpawnDelay",
                "productionStats.spawnBatchSize", "productionStats.maximumPopulation",
                "productionStats.spawnPriority", "combatStats.maxHealth", "combatStats.attackDamage",
                "combatStats.attackIntervalSeconds", "combatStats.attackRange", "combatStats.movementSpeed",
                "combatStats.armor", "combatStats.resistance", "targeting.priority",
                "targeting.canTargetGround", "targeting.canTargetFlying"
            };

            foreach (string path in nestedPaths) Assert.That(serialized.FindProperty(path), Is.Not.Null, path);
        }

        [Test]
        public void SwitchingBetweenUnits_RebuildsDetailsForNewSelection()
        {
            var window = CreateWindow();
            try
            {
                Select(window, AssetDatabase.LoadAssetAtPath<UnitDefinition>(SpiderPath));
                Assert.That(Details(window).Q<Label>(className: "stable-id").text, Is.EqualTo("HIVE_SPIDER"));

                Select(window, AssetDatabase.LoadAssetAtPath<UnitDefinition>(BeetlePath));
                Assert.That(Details(window).Q<Label>(className: "stable-id").text, Is.EqualTo("HIVE_BEETLE"));
                Assert.That(Details(window).Q<PropertyField>("unit-field-goldcost").bindingPath,
                    Is.EqualTo("goldCost"));
            }
            finally { Close(window); }
        }

        [Test]
        public void UndoRedo_RebuildsBoundSerializedFieldWithoutLosingSelection()
        {
            string folder = CreateTemporaryFolder();
            UnitDefinition unit = CreateTemporaryUnit(folder, "UNIT_CONTENT_STUDIO_UNDO", "Undo Unit");
            var window = CreateWindow();
            try
            {
                var serialized = new SerializedObject(unit);
                Undo.RecordObject(unit, "Edit Content Studio Gold Cost");
                serialized.FindProperty("goldCost").intValue = 7;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(unit);

                Select(window, unit);
                PropertyField beforeUndo = Details(window).Q<PropertyField>("unit-field-goldcost");
                Assert.That(beforeUndo.bindingPath, Is.EqualTo("goldCost"));
                Assert.That(new SerializedObject(unit).FindProperty("goldCost").intValue, Is.EqualTo(7));

                Undo.PerformUndo();
                PropertyField afterUndo = Details(window).Q<PropertyField>("unit-field-goldcost");
                Assert.That(afterUndo, Is.Not.SameAs(beforeUndo));
                Assert.That(afterUndo.bindingPath, Is.EqualTo("goldCost"));
                Assert.That(new SerializedObject(unit).FindProperty("goldCost").intValue, Is.EqualTo(1));
                Assert.That(List(window).selectedItem, Is.SameAs(unit));

                Undo.PerformRedo();
                PropertyField afterRedo = Details(window).Q<PropertyField>("unit-field-goldcost");
                Assert.That(afterRedo, Is.Not.SameAs(afterUndo));
                Assert.That(afterRedo.bindingPath, Is.EqualTo("goldCost"));
                Assert.That(new SerializedObject(unit).FindProperty("goldCost").intValue, Is.EqualTo(7));
                Assert.That(List(window).selectedItem, Is.SameAs(unit));
            }
            finally
            {
                try { Close(window); }
                finally
                {
                    if (unit != null) Undo.ClearUndo(unit);
                    DeleteTemporaryFolder(folder);
                }
            }
        }

        [Test]
        public void DisabledOrDeletedSelectedUnit_RefreshesWithoutThrowing()
        {
            string folder = CreateTemporaryFolder();
            UnitDefinition unit = CreateTemporaryUnit(folder, "UNIT_CONTENT_STUDIO_DELETE", "Delete Unit");
            var window = CreateWindow();
            try
            {
                Select(window, unit);
                Undo.RecordObject(unit, "Disable Content Studio Test Unit");
                var serialized = new SerializedObject(unit);
                serialized.FindProperty("isEnabled").boolValue = false;
                serialized.ApplyModifiedProperties();
                EditorUtility.SetDirty(unit);
                Assert.DoesNotThrow(() => SetSearch(window, unit.Id));
                Assert.That(((ContentDefinition)List(window).selectedItem).Id, Is.EqualTo(unit.Id));

                SetSearch(window, "no-matching-content");
                Assert.That(Details(window).Query<Foldout>(className: UnitDefinitionEditorBuilder.SectionClass).ToList(),
                    Is.Empty, "Filtering out a selected definition must clear its detail editor.");

                string path = AssetDatabase.GetAssetPath(unit);
                Undo.ClearUndo(unit);
                Assert.That(AssetDatabase.DeleteAsset(path), Is.True);
                AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
                Assert.DoesNotThrow(() => SetSearch(window, "deleted-selected-unit"));
                Assert.That(List(window).selectedItem as ContentDefinition == null, Is.True,
                    "Deleted Unity assets must be treated as fake-null selections.");
                Assert.That(Details(window).Query<Foldout>(className: UnitDefinitionEditorBuilder.SectionClass).ToList(),
                    Is.Empty);
            }
            finally
            {
                try { Close(window); }
                finally { DeleteTemporaryFolder(folder); }
            }
        }

        [Test]
        public void MissingExpectedPropertyPath_ReturnsStructuredDiagnosticAndFallback()
        {
            var wrongAssetType = ScriptableObject.CreateInstance<RaceDefinition>();
            var container = new VisualElement();
            var reported = new List<ContentStudioBindingIssue>();
            try
            {
                UnitDefinitionEditorBuildResult result = UnitDefinitionEditorBuilder.Build(container,
                    new SerializedObject(wrongAssetType), reported.Add);

                Assert.That(result.Success, Is.False);
                Assert.That(reported.Count, Is.EqualTo(result.Issues.Count));
                Assert.That(reported.Select(issue => issue.PropertyPath), Does.Contain("race"));
                ContentStudioBindingIssue raceIssue = reported.Single(issue => issue.PropertyPath == "race");
                Assert.That(raceIssue.AssetType, Is.EqualTo(nameof(RaceDefinition)));
                Assert.That(raceIssue.AssetPath, Is.EqualTo("<unsaved asset>"));
                Assert.That(raceIssue.Message, Does.Contain("Expected serialized property path: 'race'"));
                Assert.That(container.Q<HelpBox>().text, Is.EqualTo(UnitDefinitionEditorBuilder.FallbackMessage));
            }
            finally { UnityEngine.Object.DestroyImmediate(wrongAssetType); }
        }

        [Test]
        public void FilteringList_WhenSelectedUnitStillMatches_PreservesSelectionAndDetails()
        {
            var window = CreateWindow();
            try
            {
                var spider = AssetDatabase.LoadAssetAtPath<UnitDefinition>(SpiderPath);
                Select(window, spider);
                VisualElement originalIdentity = Details(window).Q<VisualElement>(className: "identity-block");

                SetSearch(window, "HIVE_SPIDER");

                Assert.That(List(window).selectedItem, Is.SameAs(spider));
                Assert.That(Details(window).Q<VisualElement>(className: "identity-block"), Is.SameAs(originalIdentity),
                    "A filter refresh should not clear and rebuild valid selected details.");
            }
            finally { Close(window); }
        }

        private static ContentStudioWindow CreateWindow()
        {
            var window = ScriptableObject.CreateInstance<ContentStudioWindow>();
            window.CreateGUI();
            return window;
        }

        private static void Close(ContentStudioWindow window)
        {
            Selection.activeObject = null;
            if (window != null) UnityEngine.Object.DestroyImmediate(window);
        }

        private static void Select(ContentStudioWindow window, ContentDefinition definition)
        {
            ListView list = List(window);
            int index = ((IEnumerable)list.itemsSource).Cast<ContentDefinition>().ToList().IndexOf(definition);
            Assert.That(index, Is.GreaterThanOrEqualTo(0), definition == null ? "<null>" : definition.Id);
            list.SetSelection(index);
        }

        private static ListView List(ContentStudioWindow window) =>
            window.rootVisualElement.Q<ListView>("content-list");

        private static ScrollView Details(ContentStudioWindow window) =>
            window.rootVisualElement.Q<ScrollView>("details");

        private static void SetSearch(ContentStudioWindow window, string value)
            => window.SetSearchQuery(value);

        private static string CreateTemporaryFolder()
        {
            string name = "__ContentStudioTests_" + Guid.NewGuid().ToString("N");
            string folder = TemporaryRoot + "/" + name;
            Assert.That(AssetDatabase.IsValidFolder(TemporaryRoot), Is.True, TemporaryRoot);
            Assert.That(AssetDatabase.CreateFolder(TemporaryRoot, name), Is.Not.Empty);
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            return folder;
        }

        private static UnitDefinition CreateTemporaryUnit(string folder, string id, string displayName)
        {
            var unit = ScriptableObject.CreateInstance<UnitDefinition>();
            unit.EditorInitialize(id + "." + Guid.NewGuid().ToString("N"), displayName);
            AssetDatabase.CreateAsset(unit, folder + "/Unit.asset");
            AssetDatabase.SaveAssets();
            AssetDatabase.ImportAsset(folder + "/Unit.asset", ImportAssetOptions.ForceSynchronousImport);
            return AssetDatabase.LoadAssetAtPath<UnitDefinition>(folder + "/Unit.asset");
        }

        private static void DeleteTemporaryFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder) && !AssetDatabase.DeleteAsset(folder))
                throw new InvalidOperationException($"Unity could not delete temporary Content Studio folder '{folder}'.");
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
        }
    }
}
