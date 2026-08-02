using System;
using System.Linq;
using System.Reflection;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.Content.Validation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.Content.Tests
{
    public sealed class ContentFoundationTests
    {
        private const string CatalogPath = "Assets/_Game/Content/Assets/Configuration/GameContentCatalog.asset";
        private const string StudioLayoutPath = "Assets/_Game/Editor/ContentStudio/ContentStudioWindow.uxml";
        private const string StudioStylePath = "Assets/_Game/Editor/ContentStudio/ContentStudioWindow.uss";

        [Test]
        public void CatalogRejectsDuplicateIds()
        {
            var first = Create<UnitDefinition>("UNIT_TEST_SAME", "First");
            var second = Create<UnitDefinition>("UNIT_TEST_SAME", "Second");
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            catalog.EditorSetDefinitions(new ContentDefinition[] { first, second });

            Assert.Throws<InvalidOperationException>(() => catalog.RebuildIndex());
            Destroy(first, second, catalog);
        }

        [Test]
        public void CatalogResolvesByStableIdAndFiltersDisabledContent()
        {
            var unit = Create<UnitDefinition>("UNIT_TEST_ONE", "One");
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            catalog.EditorSetDefinitions(new[] { unit });

            Assert.That(catalog.TryGet<UnitDefinition>("UNIT_TEST_ONE", out var enabled), Is.True);
            Assert.That(enabled, Is.SameAs(unit));

            unit.EditorSetEnabled(false);
            Assert.That(catalog.TryGet<UnitDefinition>("UNIT_TEST_ONE", out _), Is.False);
            Assert.That(catalog.TryGet<UnitDefinition>("UNIT_TEST_ONE", out var disabled, true), Is.True);
            Assert.That(disabled, Is.SameAs(unit));
            Destroy(unit, catalog);
        }

        [Test]
        public void StableIdCanOnlyBeInitializedOnce()
        {
            var unit = Create<UnitDefinition>("UNIT_TEST_IMMUTABLE", "Immutable");

            Assert.Throws<InvalidOperationException>(() => unit.EditorInitialize("UNIT_TEST_CHANGED", "Changed"));
            Assert.That(unit.Id, Is.EqualTo("UNIT_TEST_IMMUTABLE"));
            Destroy(unit);
        }

        [Test]
        public void RuntimeStatSurfaceHasNoPublicMutationPath()
        {
            Assert.That(typeof(UnitCombatStats).GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
            Assert.That(typeof(UnitProductionStats).GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
            Assert.That(typeof(UnitCombatStats).GetProperty(nameof(UnitCombatStats.MaxHealth))?.SetMethod, Is.Null);
            Assert.That(typeof(UnitProductionStats).GetProperty(nameof(UnitProductionStats.SpawnInterval))?.SetMethod, Is.Null);
        }

        [Test]
        public void UnitValidationReportsMissingRaceWithActionableMetadata()
        {
            var unit = Create<UnitDefinition>("UNIT_TEST_NO_RACE", "No Race");
            var issue = ContentValidator.Validate(unit).First(result => result.FieldName == "race");

            Assert.That(issue.Severity, Is.EqualTo(ValidationSeverity.Error));
            Assert.That(issue.DefinitionId, Is.EqualTo("UNIT_TEST_NO_RACE"));
            Assert.That(issue.SuggestedFix, Is.Not.Empty);
            Destroy(unit);
        }

        [Test]
        public void CanonicalPrototypeContentHasNoBlockingValidationIssues()
        {
            var definitions = AssetDatabase.FindAssets("t:ContentDefinition", new[] { "Assets/_Game/Content/Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(definition => definition != null)
                .ToList();

            Assert.That(definitions.Select(definition => definition.Id), Does.Contain("RACE_HIVE"));
            Assert.That(definitions.Select(definition => definition.Id), Does.Contain("HIVE_LARVA"));
            Assert.That(definitions.Select(definition => definition.Id), Does.Contain("HIVE_SPIDER"));
            Assert.That(definitions.Select(definition => definition.Id), Does.Contain("HIVE_BEETLE"));

            var blocking = ContentValidator.ValidateAll(definitions, AssetDatabase.GetAssetPath)
                .Where(issue => issue.Severity is ValidationSeverity.Error or ValidationSeverity.Critical)
                .ToList();
            Assert.That(blocking, Is.Empty, string.Join("\n", blocking));
        }

        [Test]
        public void DefaultCatalogContainsEveryAuthoredDefinition()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);

            var definitions = AssetDatabase.FindAssets("t:ContentDefinition", new[] { "Assets/_Game/Content/Assets" })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(definition => definition != null)
                .ToList();
            Assert.That(catalog.Definitions.Count, Is.EqualTo(definitions.Count));
            Assert.DoesNotThrow(catalog.RebuildIndex);
        }

        [Test]
        public void ContentStudioUsesUxmlAndUssAssets()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(StudioLayoutPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<StyleSheet>(StudioStylePath), Is.Not.Null);
        }

        private static T Create<T>(string id, string displayName) where T : ContentDefinition
        {
            var definition = ScriptableObject.CreateInstance<T>();
            definition.EditorInitialize(id, displayName);
            return definition;
        }

        private static void Destroy(params UnityEngine.Object[] objects)
        {
            foreach (var value in objects.Where(value => value != null)) UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
