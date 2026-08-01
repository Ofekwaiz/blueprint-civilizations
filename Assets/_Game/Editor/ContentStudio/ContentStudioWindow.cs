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
    public sealed class ContentStudioWindow : EditorWindow
    {
        private readonly List<ContentDefinition> items = new();
        private ListView list;
        private VisualElement details;
        private Label validation;
        private ToolbarSearchField search;
        private Type selectedType = typeof(UnitDefinition);

        [MenuItem("Tools/Blueprint Civilizations/Content Studio")]
        public static void Open() => GetWindow<ContentStudioWindow>("Content Studio");

        public void CreateGUI()
        {
            rootVisualElement.style.flexDirection = FlexDirection.Column;
            var toolbar = new Toolbar();
            search = new ToolbarSearchField();
            search.RegisterValueChangedCallback(_ => Refresh());
            toolbar.Add(search);
            var typeMenu = new ToolbarMenu { text = "Units" };
            AddType(typeMenu, "Units", typeof(UnitDefinition));
            AddType(typeMenu, "Races", typeof(RaceDefinition));
            AddType(typeMenu, "Structures", typeof(StructureDefinition));
            AddType(typeMenu, "Research", typeof(ResearchDefinition));
            AddType(typeMenu, "Artifacts", typeof(ArtifactDefinition));
            AddType(typeMenu, "Evolutions", typeof(EvolutionDefinition));
            toolbar.Add(typeMenu);
            toolbar.Add(new ToolbarButton(CreateSelectedType) { text = "Create" });
            toolbar.Add(new ToolbarButton(DuplicateSelected) { text = "Duplicate" });
            toolbar.Add(new ToolbarButton(DisableSelected) { text = "Disable" });
            toolbar.Add(new ToolbarButton(DeleteSelected) { text = "Delete Permanently" });
            rootVisualElement.Add(toolbar);

            var split = new TwoPaneSplitView(0, 290, TwoPaneSplitViewOrientation.Horizontal);
            list = new ListView(items, 22, () => new Label(), (e, i) => ((Label)e).text = items[i] == null ? "<missing>" : $"{items[i].DisplayName}  [{items[i].Id}]");
            list.selectionChanged += selection => ShowDetails(selection.FirstOrDefault() as ContentDefinition);
            details = new ScrollView();
            split.Add(list); split.Add(details);
            rootVisualElement.Add(split);
            validation = new Label("Select content to see validation.");
            validation.style.whiteSpace = WhiteSpace.Normal;
            validation.style.minHeight = 44;
            rootVisualElement.Add(validation);
            Refresh();
        }

        private void AddType(ToolbarMenu menu, string label, Type type)
        {
            menu.menu.AppendAction(label, _ => { selectedType = type; menu.text = label; Refresh(); });
        }

        private void Refresh()
        {
            items.Clear();
            string[] guids = AssetDatabase.FindAssets($"t:{selectedType.Name}");
            foreach (string guid in guids)
            {
                var asset = AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null) continue;
                if (!string.IsNullOrWhiteSpace(search?.value) && !(asset.DisplayName + asset.Id).Contains(search.value, StringComparison.OrdinalIgnoreCase)) continue;
                items.Add(asset);
            }
            items.Sort((a,b) => string.Compare(a.DisplayName, b.DisplayName, StringComparison.OrdinalIgnoreCase));
            list?.Rebuild();
        }

        private void ShowDetails(ContentDefinition selected)
        {
            details.Clear();
            if (selected == null) return;
            var serialized = new SerializedObject(selected);
            var inspector = new InspectorElement(serialized);
            details.Add(inspector);
            UpdateValidation(selected);
        }

        private void UpdateValidation(ContentDefinition selected)
        {
            var all = AssetDatabase.FindAssets("t:ContentDefinition").Select(g => AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(g)));
            validation.text = string.Join("\n", ContentValidator.Validate(selected, all).Select(i => i.ToString()).DefaultIfEmpty("No validation issues."));
        }

        private void CreateSelectedType()
        {
            const string folder = "Assets/_Game/Content/Definitions";
            EnsureFolder(folder);
            var asset = CreateInstance(selectedType) as ContentDefinition;
            string label = selectedType.Name.Replace("Definition", "");
            string path = AssetDatabase.GenerateUniqueAssetPath($"{folder}/{label}_New.asset");
            string generatedId = $"{label.ToLowerInvariant()}.new.{Guid.NewGuid():N}";
            asset.EditorInitialize(generatedId, $"New {label}");
            AssetDatabase.CreateAsset(asset, path);
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
            AssetDatabase.CopyAsset(source, destination); AssetDatabase.SaveAssets(); Refresh();
        }
        private void DisableSelected()
        {
            if (Selected == null) return;
            var so = new SerializedObject(Selected); so.FindProperty("isEnabled").boolValue = false; so.ApplyModifiedProperties(); EditorUtility.SetDirty(Selected); AssetDatabase.SaveAssets(); UpdateValidation(Selected);
        }
        private void DeleteSelected()
        {
            if (Selected == null) return;
            if (!EditorUtility.DisplayDialog("Delete permanently?", $"Delete {Selected.DisplayName}? Existing saves may reference ID '{Selected.Id}'. Disabling is safer.", "Delete", "Cancel")) return;
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(Selected)); AssetDatabase.SaveAssets(); Refresh();
        }
        private static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1)) { string next = current + "/" + part; if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part); current = next; }
        }
    }
}
