using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.Content.Validation;
using BlueprintCivilizations.Core;
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
        private const string AllRaces = "All races";
        private const string AllTiers = "All tiers";
        private const string AllStates = "All states";

        private enum EnabledFilter { All, Enabled, Disabled }

        private readonly List<ContentDefinition> items = new();
        private readonly Dictionary<string, RaceDefinition> racesByChoice = new(StringComparer.Ordinal);
        private ListView list;
        private ScrollView details;
        private ScrollView validation;
        private ToolbarSearchField search;
        private ToolbarMenu typeMenu;
        private DropdownField raceFilter;
        private DropdownField tierFilter;
        private DropdownField statusFilter;
        private Button enableDisableButton;
        private Type selectedType = typeof(UnitDefinition);
        private RaceDefinition selectedRace;
        private ContentTier? selectedTier;
        private EnabledFilter selectedEnabledState;

        [MenuItem("Tools/Blueprint Civilizations/Content Studio")]
        public static void Open() => GetWindow<ContentStudioWindow>("Content Studio");

        private void OnEnable()
        {
            Undo.undoRedoPerformed -= OnUndoRedo;
            Undo.undoRedoPerformed += OnUndoRedo;
        }

        private void OnDisable() => Undo.undoRedoPerformed -= OnUndoRedo;

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
            raceFilter = rootVisualElement.Q<DropdownField>("race-filter");
            tierFilter = rootVisualElement.Q<DropdownField>("tier-filter");
            statusFilter = rootVisualElement.Q<DropdownField>("status-filter");
            list = rootVisualElement.Q<ListView>("content-list");
            details = rootVisualElement.Q<ScrollView>("details");
            validation = rootVisualElement.Q<ScrollView>("validation");
            enableDisableButton = rootVisualElement.Q<Button>("disable-button");

            ConfigureTypeMenu();
            ConfigureFilters();
            search.RegisterValueChangedCallback(_ => Refresh());
            raceFilter.RegisterValueChangedCallback(change =>
            {
                selectedRace = racesByChoice.TryGetValue(change.newValue, out var race) ? race : null;
                Refresh();
            });
            tierFilter.RegisterValueChangedCallback(change =>
            {
                selectedTier = ParseTier(change.newValue);
                Refresh();
            });
            statusFilter.RegisterValueChangedCallback(change =>
            {
                selectedEnabledState = change.newValue == "Enabled" ? EnabledFilter.Enabled :
                    change.newValue == "Disabled" ? EnabledFilter.Disabled : EnabledFilter.All;
                Refresh();
            });
            rootVisualElement.Q<Button>("create-button").clicked += CreateSelectedType;
            rootVisualElement.Q<Button>("duplicate-button").clicked += DuplicateSelected;
            enableDisableButton.clicked += ToggleEnabledSelected;
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
            list.selectionChanged += selection =>
            {
                var selected = selection.FirstOrDefault() as ContentDefinition;
                if (selected != null) Selection.activeObject = selected;
                ShowDetails(selected);
            };
            list.itemsChosen += selection =>
            {
                if (selection.FirstOrDefault() is ContentDefinition definition)
                {
                    Selection.activeObject = definition;
                    EditorGUIUtility.PingObject(definition);
                }
            };
            details.RegisterCallback<SerializedPropertyChangeEvent>(_ => OnSerializedPropertyChanged());

            RefreshRaceFilter();
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

        private void ConfigureFilters()
        {
            tierFilter.choices = new List<string> { AllTiers, "Tier 1", "Tier 2", "Tier 3", "Tier 4", "Tier 5" };
            tierFilter.SetValueWithoutNotify(AllTiers);
            statusFilter.choices = new List<string> { AllStates, "Enabled", "Disabled" };
            statusFilter.SetValueWithoutNotify(AllStates);
        }

        private void RefreshRaceFilter()
        {
            string previousId = selectedRace == null ? "" : selectedRace.Id;
            racesByChoice.Clear();
            var choices = new List<string> { AllRaces };
            foreach (var race in ContentTools.GetAllDefinitions().OfType<RaceDefinition>()
                         .OrderBy(value => value.DisplayName, StringComparer.OrdinalIgnoreCase))
            {
                string choice = $"{race.DisplayName} [{race.Id}]";
                choices.Add(choice);
                racesByChoice[choice] = race;
            }

            raceFilter.choices = choices;
            var preserved = racesByChoice.FirstOrDefault(pair => pair.Value != null && pair.Value.Id == previousId);
            if (!string.IsNullOrEmpty(preserved.Key))
            {
                selectedRace = preserved.Value;
                raceFilter.SetValueWithoutNotify(preserved.Key);
            }
            else
            {
                selectedRace = null;
                raceFilter.SetValueWithoutNotify(AllRaces);
            }
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

        private void Refresh(ContentDefinition preserveSelection = null)
        {
            if (list == null) return;
            var previous = preserveSelection != null ? preserveSelection : Selected;
            PopulateItems();
            list.Rebuild();

            int selectedIndex = previous == null ? -1 : items.IndexOf(previous);
            if (selectedIndex >= 0) list.SetSelection(selectedIndex);
            else
            {
                list.ClearSelection();
                ShowDetails(null);
            }
        }

        private void RefreshListWithoutRebuildingEditor(ContentDefinition selected)
        {
            if (list == null) return;
            PopulateItems();
            list.Rebuild();
            int selectedIndex = selected == null ? -1 : items.IndexOf(selected);
            if (selectedIndex >= 0) list.SetSelectionWithoutNotify(new[] { selectedIndex });
            else
            {
                list.ClearSelection();
                ShowDetails(null);
            }
        }

        private void PopulateItems()
        {
            items.Clear();
            string query = search?.value ?? "";
            foreach (string guid in AssetDatabase.FindAssets($"t:{selectedType.Name}"))
            {
                var asset = AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(guid));
                if (asset == null || !MatchesFilters(asset, query)) continue;
                items.Add(asset);
            }
            items.Sort((left, right) =>
            {
                int display = string.Compare(left.DisplayName, right.DisplayName, StringComparison.OrdinalIgnoreCase);
                return display != 0 ? display : string.Compare(left.Id, right.Id, StringComparison.Ordinal);
            });
        }

        private bool MatchesFilters(ContentDefinition asset, string query)
        {
            if (!string.IsNullOrWhiteSpace(query))
            {
                string searchable = asset.DisplayName + " " + asset.Id + " " + string.Join(" ", asset.Tags);
                if (searchable.IndexOf(query, StringComparison.OrdinalIgnoreCase) < 0) return false;
            }
            if (selectedEnabledState == EnabledFilter.Enabled && !asset.IsEnabled) return false;
            if (selectedEnabledState == EnabledFilter.Disabled && asset.IsEnabled) return false;
            if (selectedRace != null && GetRace(asset) != selectedRace) return false;
            if (selectedTier.HasValue && GetTier(asset) != selectedTier) return false;
            return true;
        }

        private static RaceDefinition GetRace(ContentDefinition definition)
        {
            return definition switch
            {
                RaceDefinition race => race,
                UnitDefinition unit => unit.Race,
                StructureDefinition structure => structure.Race,
                ResearchDefinition research => research.AffinityRace,
                ArtifactDefinition artifact => artifact.AffinityRace,
                _ => null
            };
        }

        private static ContentTier? GetTier(ContentDefinition definition)
        {
            return definition switch
            {
                UnitDefinition unit => unit.Tier,
                StructureDefinition structure => structure.Tier,
                _ => null
            };
        }

        private static ContentTier? ParseTier(string value)
        {
            return value switch
            {
                "Tier 1" => ContentTier.Tier1,
                "Tier 2" => ContentTier.Tier2,
                "Tier 3" => ContentTier.Tier3,
                "Tier 4" => ContentTier.Tier4,
                "Tier 5" => ContentTier.Tier5,
                _ => null
            };
        }

        private void ShowDetails(ContentDefinition selected)
        {
            details.Clear();
            if (selected == null)
            {
                enableDisableButton.text = "Disable (Recommended)";
                ShowValidationMessage("Select content to see validation.");
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
            if (selected is UnitDefinition) BuildUnitEditor(serialized);
            else details.Add(new InspectorElement(serialized));

            enableDisableButton.text = selected.IsEnabled ? "Disable (Recommended)" : "Enable";
            UpdateValidation(selected);
        }

        private void BuildUnitEditor(SerializedObject serialized)
        {
            AddUnitSection(serialized, "Identity", "displayName", "description", "dataVersion", "isEnabled", "tags", "race", "isNeutral", "tier", "role");
            AddUnitSection(serialized, "Economy and Shop", "goldCost", "poolKind", "shopPoolSize", "baseShopWeight");
            AddUnitSection(serialized, "Production", "productionStats");
            AddUnitSection(serialized, "Combat", "combatStats", "targeting", "laneCompatibility", "movementProfile", "abilities");
            AddUnitSection(serialized, "Blueprint Progression", "permittedPerCopyStatUpgrades", "socketMilestones", "ascensionOneThreshold", "ascensionOneOptions", "ascensionTwoThreshold", "ascensionTwoOptions");
            AddUnitSection(serialized, "Presentation", "icon", "presentation");
        }

        private void AddUnitSection(SerializedObject serialized, string title, params string[] propertyNames)
        {
            var section = new Foldout { text = title, value = true };
            section.AddToClassList("unit-section");
            foreach (string propertyName in propertyNames)
            {
                var property = serialized.FindProperty(propertyName);
                if (property != null) section.Add(new PropertyField(property));
                else section.Add(new HelpBox($"Missing serialized field: {propertyName}", HelpBoxMessageType.Error));
            }
            details.Add(section);
        }

        private void OnSerializedPropertyChanged()
        {
            var selected = Selected;
            if (selected == null) return;
            EditorUtility.SetDirty(selected);
            UpdateValidation(selected);
            details.schedule.Execute(() => RefreshListWithoutRebuildingEditor(selected)).ExecuteLater(50);
        }

        private void UpdateValidation(ContentDefinition selected)
        {
            validation.Clear();
            if (selected == null)
            {
                ShowValidationMessage("The selected asset was deleted or is missing.");
                return;
            }

            var issues = ContentValidator.Validate(selected, ContentTools.GetAllDefinitions(), AssetDatabase.GetAssetPath);
            if (issues.Count == 0)
            {
                ShowValidationMessage("No validation issues.");
                return;
            }

            foreach (var issue in issues)
            {
                var row = new Label($"{issue.Severity} | {issue.FieldName}\n{issue.Message}\nAsset: {issue.AssetPath}\nFix: {issue.SuggestedFix}");
                row.AddToClassList("validation-issue");
                row.AddToClassList("validation-" + issue.Severity.ToString().ToLowerInvariant());
                validation.Add(row);
            }
        }

        private void ShowValidationMessage(string message)
        {
            validation.Clear();
            validation.Add(new Label(message));
        }

        private void CreateSelectedType()
        {
            string folder = ContentTools.GetAuthoringFolder(selectedType);
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
            RefreshRaceFilter();
            Refresh(asset);
        }

        private ContentDefinition Selected => list?.selectedItem as ContentDefinition;

        private void DuplicateSelected()
        {
            var selected = Selected;
            if (selected == null) return;
            string source = AssetDatabase.GetAssetPath(selected);
            string destination = AssetDatabase.GenerateUniqueAssetPath(source.Replace(".asset", "_Copy.asset"));
            if (!AssetDatabase.CopyAsset(source, destination)) return;
            var duplicate = AssetDatabase.LoadAssetAtPath<ContentDefinition>(destination);
            if (duplicate == null) return;
            duplicate.EditorAssignDuplicateIdentity($"{selected.Id}.copy.{Guid.NewGuid():N}", $"{selected.DisplayName} Copy");
            Undo.RegisterCreatedObjectUndo(duplicate, $"Duplicate {selected.DisplayName}");
            EditorUtility.SetDirty(duplicate);
            AssetDatabase.SaveAssets();
            Selection.activeObject = duplicate;
            RefreshRaceFilter();
            Refresh(duplicate);
        }

        private void ToggleEnabledSelected()
        {
            var selected = Selected;
            if (selected == null) return;
            Undo.RecordObject(selected, selected.IsEnabled ? $"Disable {selected.DisplayName}" : $"Enable {selected.DisplayName}");
            selected.EditorSetEnabled(!selected.IsEnabled);
            EditorUtility.SetDirty(selected);
            AssetDatabase.SaveAssets();
            RefreshRaceFilter();
            Refresh(selected);
        }

        private void DeleteSelected()
        {
            var selected = Selected;
            if (selected == null) return;
            if (!EditorUtility.DisplayDialog(
                    "Delete permanently?",
                    $"Delete {selected.DisplayName}? Existing saves may reference ID '{selected.Id}'. Disabling is the recommended, safer action.",
                    "Delete Permanently", "Cancel")) return;
            AssetDatabase.DeleteAsset(AssetDatabase.GetAssetPath(selected));
            AssetDatabase.SaveAssets();
            RefreshRaceFilter();
            Refresh();
        }

        private void OnUndoRedo()
        {
            if (list == null) return;
            RefreshRaceFilter();
            Refresh();
        }
    }
}
