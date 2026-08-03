using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using BlueprintCivilizations.Content.Definitions;
using UnityEditor;
using UnityEditor.UIElements;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.Editor.ContentStudio
{
    /// <summary>Describes one expected UnitDefinition binding that could not be resolved.</summary>
    public sealed class ContentStudioBindingIssue
    {
        public ContentStudioBindingIssue(string assetType, string assetPath, string propertyPath)
        {
            AssetType = assetType;
            AssetPath = assetPath;
            PropertyPath = propertyPath;
            Message = $"Content Studio could not display asset type '{assetType}' at '{assetPath}'. " +
                      $"Expected serialized property path: '{propertyPath}'. " +
                      "Restore the serialized field or update the UnitDefinition editor schema.";
        }

        public string AssetType { get; }
        public string AssetPath { get; }
        public string PropertyPath { get; }
        public string Message { get; }
    }

    /// <summary>Immutable description of one grouped UnitDefinition authoring section.</summary>
    public sealed class UnitDefinitionEditorSection
    {
        public UnitDefinitionEditorSection(string title, params string[] propertyPaths)
        {
            Title = title;
            PropertyPaths = Array.AsReadOnly(propertyPaths ?? Array.Empty<string>());
        }

        public string Title { get; }
        public ReadOnlyCollection<string> PropertyPaths { get; }
    }

    /// <summary>Result of constructing and binding the grouped UnitDefinition editor.</summary>
    public sealed class UnitDefinitionEditorBuildResult
    {
        public UnitDefinitionEditorBuildResult(bool success, IEnumerable<ContentStudioBindingIssue> issues)
        {
            Success = success;
            Issues = Array.AsReadOnly((issues ?? Enumerable.Empty<ContentStudioBindingIssue>()).ToArray());
        }

        public bool Success { get; }
        public ReadOnlyCollection<ContentStudioBindingIssue> Issues { get; }
    }

    /// <summary>Builds the complete serialized UnitDefinition authoring surface.</summary>
    public static class UnitDefinitionEditorBuilder
    {
        public const string FallbackMessage =
            "Unable to display UnitDefinition fields.\nSee Console for missing serialized property paths.";
        public const string SectionClass = "unit-section";
        public const string BoundFieldClass = "unit-bound-field";

        private static readonly ReadOnlyCollection<UnitDefinitionEditorSection> sectionDefinitions =
            Array.AsReadOnly(new[]
            {
                new UnitDefinitionEditorSection("Identity",
                    "displayName", "description", "dataVersion", "isEnabled", "tags", "icon",
                    "race", "isNeutral", "tier", "role"),
                new UnitDefinitionEditorSection("Economy and Shop",
                    "goldCost", "poolKind", "shopPoolSize", "baseShopWeight"),
                new UnitDefinitionEditorSection("Production",
                    "productionStats.spawnInterval", "productionStats.initialSpawnDelay",
                    "productionStats.spawnBatchSize", "productionStats.maximumPopulation",
                    "productionStats.spawnPriority"),
                new UnitDefinitionEditorSection("Combat",
                    "combatStats.maxHealth", "combatStats.attackDamage", "combatStats.attackIntervalSeconds",
                    "combatStats.attackRange", "combatStats.movementSpeed", "combatStats.armor",
                    "combatStats.resistance", "targeting.priority", "targeting.canTargetGround",
                    "targeting.canTargetFlying", "laneCompatibility", "movementProfile", "abilities"),
                new UnitDefinitionEditorSection("Blueprint Progression",
                    "permittedPerCopyStatUpgrades", "socketMilestones.firstSocketCopies",
                    "socketMilestones.secondSocketCopies", "socketMilestones.thirdSocketCopies",
                    "ascensionOneThreshold", "ascensionOneOptions", "ascensionTwoThreshold",
                    "ascensionTwoOptions"),
                new UnitDefinitionEditorSection("Presentation",
                    "presentation.visualPrefab", "presentation.animatorController", "presentation.spawnAudio",
                    "presentation.attackAudio", "presentation.deathAudio", "presentation.spawnVfxPrefab",
                    "presentation.deathVfxPrefab")
            });

        public static ReadOnlyCollection<UnitDefinitionEditorSection> Sections => sectionDefinitions;

        public static UnitDefinitionEditorBuildResult Build(VisualElement container, SerializedObject serialized,
            Action<ContentStudioBindingIssue> reportIssue = null)
        {
            if (container == null) throw new ArgumentNullException(nameof(container));
            if (serialized == null) throw new ArgumentNullException(nameof(serialized));

            var issues = FindMissingProperties(serialized);
            if (issues.Count > 0)
            {
                foreach (var issue in issues) reportIssue?.Invoke(issue);
                container.Add(new HelpBox(FallbackMessage, HelpBoxMessageType.Error));
                return new UnitDefinitionEditorBuildResult(false, issues);
            }

            foreach (var definition in sectionDefinitions)
            {
                var section = new Foldout
                {
                    name = "unit-section-" + ToElementName(definition.Title),
                    text = definition.Title,
                    value = true
                };
                section.AddToClassList(SectionClass);

                foreach (string propertyPath in definition.PropertyPaths)
                {
                    SerializedProperty property = serialized.FindProperty(propertyPath);
                    var field = new PropertyField(property, property.displayName)
                    {
                        name = "unit-field-" + ToElementName(propertyPath),
                        bindingPath = property.propertyPath,
                        tooltip = $"Serialized property: {property.propertyPath}"
                    };
                    field.AddToClassList(BoundFieldClass);
                    section.Add(field);
                }

                container.Add(section);
            }

            // UI Toolkit materializes PropertyField controls only after the complete tree is bound.
            container.Bind(serialized);
            return new UnitDefinitionEditorBuildResult(true, Array.Empty<ContentStudioBindingIssue>());
        }

        public static ReadOnlyCollection<ContentStudioBindingIssue> FindMissingProperties(SerializedObject serialized)
        {
            if (serialized == null) throw new ArgumentNullException(nameof(serialized));
            string assetType = serialized.targetObject == null ? "<missing>" : serialized.targetObject.GetType().Name;
            string assetPath = serialized.targetObject == null ? "<missing>" : AssetDatabase.GetAssetPath(serialized.targetObject);
            if (string.IsNullOrWhiteSpace(assetPath)) assetPath = "<unsaved asset>";

            var issues = sectionDefinitions
                .SelectMany(section => section.PropertyPaths)
                .Where(path => serialized.FindProperty(path) == null)
                .Select(path => new ContentStudioBindingIssue(assetType, assetPath, path))
                .ToArray();
            return Array.AsReadOnly(issues);
        }

        private static string ToElementName(string value) =>
            new(value.Select(character => char.IsLetterOrDigit(character) ? char.ToLowerInvariant(character) : '-').ToArray());
    }
}
