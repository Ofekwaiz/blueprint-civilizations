using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.Content.Validation;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.Editor.ContentStudio
{
    /// <summary>UI Toolkit authoring surface for all Milestone 0 content definitions.</summary>
    public sealed class ContentStudioWindow : EditorWindow
    {
        private const string LayoutPath = "Assets/_Game/Editor/ContentStudio/ContentStudioWindow.uxml";
        private const string StylePath = "Assets/_Game/Editor/ContentStudio/ContentStudioWindow.uss";

        private readonly List<ContentDefinition> items = new();
        private ListView list;
        private ScrollView details;
        private Label validation;
        private ToolbarSearchField search;
        private ToolbarMenu typeMenu;
        private Type selectedType = typeof(UnitDefinition);

        [MenuItem("Tools/Blueprint Civilizations/Content Studio")]
        public static void Open() => GetWindow<ContentStudioWindow>("Content Studio");

        public void CreateGUI()
        {
            rootVisualElement.Clear();
            var layout = AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(LayoutPath);
            if (layout == null)
            {
                rootVisualElement.Add(new HelpBox($"Content Studio layout is missing: {LayoutPath}", HelpBoxMessageType.Error));
                return;
            }

            layout.CloneTree(rootVisualElement);
            var style = AssetDatabase.LoadAssetAtPath<StyleSheet>(StylePath);
            if (style != null) rootVisualElement.styleSheets.Add(style);

            search = rootVisualElement.Q<ToolbarSearchField>("search");
            typeMenu = rootVisualElement.Q<ToolbarMenu>("type-menu");
            list = rootVisualElement.Q<ListView>("content-list");
            details = rootVisualElement.Q<ScrollView>("details");
            validation = rootVisualElement.Q<Label>("validation");

            ConfigureTypeMenu();
            search.RegisterValueChangedCallback(_ => Refresh());
            rootVisualElement.Q<Button>("create-button").clicked += CreateSelectedType;
            rootVisualElement.Q<Button>("duplicate-button").clicked += DuplicateSelected;
            rootVisualElement.Q<Button>("disable-button").clicked += DisableSelected;
            rootVisualElement.Q<Button>("delete-button").clicked += DeleteSelected;
            rootVisualElement.Q<Button>("validate-button").clicked += ContentTools.ValidateAll;
            rootVisualElement.Q<Button>("catalog-button").clicked += ContentTools.RebuildDefaultCatalog;

            list.itemsSource = items;
            list.fixedItemHeight = 24;
            list.makeItem = () => new Label();
            list.bindItem = (element, index) =>
            {
                var item = items[index];
                ((Label)element).text = item == null ? "<missing>" : $"{item.DisplayName}  [{item.Id}]";
                element.EnableInClassList("disabled-content", item != null && !item.IsEnabled);
            };
            list.selectionChanged += selection => ShowDetails(selection.FirstOrDefault() as ContentDefinition);
            list.itemsChosen += selection =>
            {
                if (selection.FirstOrDefault() is ContentDefinition definition)
                {
                    Selection.activeObject = definition;
                    EditorGUIUtility.PingObject(definition);
                }
            };

            Refresh();
        }

        private void ConfigureTypeMenu()
        {
            AddType("Units", typeof(UnitDefinition));
            AddType("Races", typeof(RaceDefinition));
            AddType("Nexus", typeof(NexusDefinition));
            AddType("Structures", typeof(StructureDefinition));
            AddType("Research", typeof(ResearchDefinition));
            AddType("Artifacts", typeof(ArtifactDefinition));
            AddType("Evolutions", typeof(EvolutionDefinition));
            AddType("Abilities", typeof(AbilityDefinition));
            AddType("Philosophies", typeof(PhilosophyDefinition));
            AddType("Augments", typeof(AugmentDefinition));
            AddType("Configuration", typeof(GameBalanceConfigurationDefinition));
            typeMenu.text = "Units";
        }

        private void AddType(string label, Type type)
        {
            typeMenu.menu.AppendAction(label, _ =>
            {
                selectedType = type;
                typeMenu.text = label;
                Refresh();
            });
        }

        private void Refresh()
        {
            if (list == null) return;
            items.Clear();
            string query = search?.value ?? "";
            foreach (string guid in AssetDatabase.FindAssets($"t:{selectedType.Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                if (!string.IsNullOrWhiteSpace(query) &&
                    !(asset.DisplayName + asset.Id + string.Join(" ", asset.Tags)).Contains(query, StringComparison.OrdinalIgnoreCase)) continue;
                items.Add(asset);
            }
            items.Sort((left, right) => string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase));
            list.Rebuild();
        }

        private void ShowDetails(ContentDefinition selected)
        {
            details.Clear();
            if (selected == null)
            {
                validation.text = "Select content to see validation.";
                return;
            }

            var identity = new VisualElement();
            identity.AddToClassList("identity-block");
            identity.Add(new Label("Stable ID"));
            var idValue = new Label(selected.Id);
            idValue.AddToClassList("stable-id");
            identity.Add(idValue);
            details.Add(identity);

            var serialized = new SerializedObject(selected);
            var inspector = new InspectorElement(serialized);
            inspector.RegisterCallback<SerializedPropertyChangeEvent>(_ => UpdateValidation(selected));
            details.Add(inspector);
            UpdateValidation(selected);
        }

        private void UpdateValidation(ContentDefinition selected)
        {
            var all = ContentTools.GetAllDefinitions();
            validation.text = string.Join("\n", ContentValidator.Validate(selected, all, AssetDatabase.GetAssetPath)
                .Select(issue => issue.ToString()).DefaultIfEmpty("No validation issues."));
        }

        private void CreateSelectedType()
        {
            const string folder = "Assets/_Game/Content/Assets/Custom";
            ContentTools.EnsureFolder(folder);
            var asset = ScriptableObject.CreateInstance(selectedType) as ContentDefinition;
            if (asset == null) return;
            string label = selectedType.Name.Replace("Definition", "");
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{label}_New.asset");
            asset.EditorInitialize($"{label.ToLowerInvariant()}.custom.{Guid.NewGuid():N}", $"New {label}");
            AssetDatabase.CreateAsset(asset, path);
            Undo.RegisterCreatedObjectUndo(asset, $"Create {label}");
            AssetDatabase.SaveAssets();
            Selection.activeObject = asset;
            Refresh();
        }

        private ContentDefinition Selected => list?.selectedItem as ContentDefinition;

        private void DuplicateSelected()
        {
            if (Selected == null) return;
            string source = AssetDatabase.GetAssetPath(Selected);
            string destination = AssetDatabase.GenerateUniqueAssetPath(source.Replace(".asset", "_Copy.asset"));
            if (!AssetDatabase.CopyAsset(source, destination)) return;
            var duplicate = AssetDatabase.LoadAssetAtPath<ContentDefinition>(destination);
            duplicate.EditorAssignDuplicateIdentity($"{Selected.Id}.copy.{Guid.NewGuid():N}", $"{Selected.DisplayName} Copy");
            Undo.RegisterCreatedObjectUndo(duplicate, $"Duplicate {Selected.DisplayName}");
            EditorUtility.SetDirty(duplicate);
            AssetDatabase.SaveAssets();
            Selection.activeObject = duplicate;
            Refresh();
        }

        private void DisableSelected()
        {
            if (Selected == null) return;
            Undo.RecordObject(Selected, $"Disable {Selected.DisplayName}");
            Selected.EditorSetEnabled(false);
            EditorUtility.SetDirty(Selected);
            AssetDatabase.SaveAssets();
            UpdateValidation(Selected);
            list.Rebuild();
        }

        private void DeleteSelected()
        {
            if (Selected == null) return;
            if (!EditorUtility.DisplayDialog(
                    "Delete permanently?",
                    $"Delete {Selected.DisplayName}? Existing saves may reference ID '{Selected.Id}'. Disabling is safer and can be undone.",
                    "Delete", "Cancel")) return;
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Selected));
            AssetDatabase.SaveAssets();
            Refresh();
        }
    }
}
