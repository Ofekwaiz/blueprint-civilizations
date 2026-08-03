using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueprintCivilizations.Blueprints;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.Content.Validation;
using BlueprintCivilizations.Core;
using BlueprintCivilizations.Editor.ContentStudio;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.Tests
{
    public sealed class ContentFoundationTests
    {
        private const string CatalogPath = "Assets/_Game/Content/Assets/Configuration/GameContentCatalog.asset";
        private const string StudioLayoutPath = "Assets/_Game/Editor/ContentStudio/ContentStudioWindow.uxml";
        private const string StudioStylePath = "Assets/_Game/Editor/ContentStudio/ContentStudioWindow.uss";

        [Test]
        public void RebuildIndex_WhenIdsCollide_RejectsDuplicateIds()
        {
            var first = Create<UnitDefinition>("UNIT_TEST_SAME", "First");
            var second = Create<UnitDefinition>("UNIT_TEST_SAME", "Second");
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            catalog.EditorSetDefinitions(new ContentDefinition[] { first, second });

            Assert.Throws<InvalidOperationException>(() => catalog.RebuildIndex());
            Destroy(first, second, catalog);
        }

        [Test]
        public void TryGet_WhenStableIdExists_ResolvesWithoutUsingDisplayName()
        {
            var unit = CreateValidUnit("UNIT_TEST_LOOKUP", "Renamable Display Name", out var race);
            var catalog = CreateCatalog(unit);

            Assert.That(catalog.TryGet<UnitDefinition>("UNIT_TEST_LOOKUP", out var result), Is.True);
            Assert.That(result, Is.SameAs(unit));
            Assert.That(catalog.TryGet<UnitDefinition>(unit.DisplayName, out _), Is.False);
            Destroy(unit, race, catalog);
        }

        [Test]
        public void TryGet_WhenIdIsMissing_ReturnsActionableError()
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            catalog.EditorSetDefinitions(Array.Empty<ContentDefinition>());

            Assert.That(catalog.TryGet<UnitDefinition>("UNIT_DOES_NOT_EXIST", out _, out string error), Is.False);
            Assert.That(error, Does.Contain("UNIT_DOES_NOT_EXIST"));
            Assert.That(error, Does.Contain("Rebuild"));
            Assert.Throws<KeyNotFoundException>(() => catalog.GetRequired<UnitDefinition>("UNIT_DOES_NOT_EXIST"));
            Destroy(catalog);
        }

        [Test]
        public void RuntimeQueries_WhenContentIsDisabled_ExcludeItUnlessExplicitlyIncluded()
        {
            var unit = CreateValidUnit("UNIT_TEST_DISABLED", "Disabled Unit", out var race);
            var catalog = CreateCatalog(unit);
            unit.EditorSetEnabled(false);

            Assert.That(catalog.TryGet<UnitDefinition>(unit.Id, out _), Is.False);
            Assert.That(catalog.TryGet<UnitDefinition>(unit.Id, out var disabled, true), Is.True);
            Assert.That(disabled, Is.SameAs(unit));
            Assert.That(catalog.GetDefinitions<UnitDefinition>(), Is.Empty);
            Assert.That(catalog.GetDefinitions<UnitDefinition>(true), Does.Contain(unit));
            Destroy(unit, race, catalog);
        }

        [Test]
        public void GetUnits_WhenRaceIsSpecified_ReturnsOnlyThatRace()
        {
            var hive = Create<RaceDefinition>("RACE_TEST_HIVE", "Hive");
            var humans = Create<RaceDefinition>("RACE_TEST_HUMANS", "Humans");
            var hiveUnit = CreateValidUnit("UNIT_TEST_HIVE", "Hive Unit", hive);
            var humanUnit = CreateValidUnit("UNIT_TEST_HUMAN", "Human Unit", humans);
            var catalog = CreateCatalog(humanUnit, hiveUnit);

            Assert.That(catalog.GetUnits(hive).Single(), Is.SameAs(hiveUnit));
            Destroy(hive, humans, hiveUnit, humanUnit, catalog);
        }

        [Test]
        public void GetUnits_WhenTierIsSpecified_ReturnsOnlyThatTier()
        {
            var race = Create<RaceDefinition>("RACE_TEST_TIER", "Tier Race");
            var tierOne = CreateValidUnit("UNIT_TEST_TIER_1", "Tier One", race);
            var tierThree = CreateValidUnit("UNIT_TEST_TIER_3", "Tier Three", race);
            SetEnum(tierThree, "tier", (int)ContentTier.Tier3 - 1);
            var catalog = CreateCatalog(tierThree, tierOne);

            Assert.That(catalog.GetUnits(tier: ContentTier.Tier3).Single(), Is.SameAs(tierThree));
            Destroy(race, tierOne, tierThree, catalog);
        }

        [Test]
        public void GetByTags_WhenAllTagsAreRequired_ReturnsMatchingDefinitions()
        {
            var race = Create<RaceDefinition>("RACE_TEST_TAGS", "Tag Race");
            var swarm = CreateValidUnit("UNIT_TEST_SWARM", "Swarm", race);
            var tank = CreateValidUnit("UNIT_TEST_TANK", "Tank", race);
            SetTags(swarm, "Hive", "Organic", "Swarm");
            SetTags(tank, "Hive", "Organic", "Frontline");
            var catalog = CreateCatalog(tank, swarm);

            Assert.That(catalog.GetByTags<UnitDefinition>(new[] { "organic", "swarm" }).Single(), Is.SameAs(swarm));
            Destroy(race, swarm, tank, catalog);
        }

        [Test]
        public void EditorSetDefinitions_WhenInputOrderVaries_StoresDeterministicIdOrder()
        {
            var zulu = Create<UnitDefinition>("UNIT_ZULU", "First Display");
            var alpha = Create<UnitDefinition>("UNIT_ALPHA", "Second Display");
            var catalog = CreateCatalog(zulu, alpha);

            CollectionAssert.AreEqual(new[] { "UNIT_ALPHA", "UNIT_ZULU" }, catalog.Definitions.Select(value => value.Id).ToArray());
            Destroy(zulu, alpha, catalog);
        }

        [Test]
        public void EditorInitialize_WhenIdWasAlreadyAssigned_LeavesStableIdUnchanged()
        {
            var unit = Create<UnitDefinition>("UNIT_TEST_IMMUTABLE", "Immutable");

            Assert.Throws<InvalidOperationException>(() => unit.EditorInitialize("UNIT_TEST_CHANGED", "Changed"));
            Assert.That(unit.Id, Is.EqualTo("UNIT_TEST_IMMUTABLE"));
            Destroy(unit);
        }

        [Test]
        public void Validate_WhenUnitIsValid_ReturnsNoBlockingIssues()
        {
            var unit = CreateValidUnit("UNIT_TEST_VALID", "Valid Unit", out var race);

            var blocking = ContentValidator.Validate(unit, new ContentDefinition[] { unit, race })
                .Where(issue => issue.Severity is ValidationSeverity.Error or ValidationSeverity.Critical);
            Assert.That(blocking, Is.Empty);
            Destroy(unit, race);
        }

        [Test]
        public void Validate_WhenUnitFieldsAreInvalid_ReportsExactFields()
        {
            var unit = CreateValidUnit("UNIT_TEST_INVALID", "Invalid Unit", out var race);
            SetInt(unit, "goldCost", -1);
            SetInt(unit, "shopPoolSize", 0);
            SetFloat(unit, "productionStats.spawnInterval", 0);
            SetInt(unit, "productionStats.maximumPopulation", 0);
            SetFloat(unit, "combatStats.attackDamage", -2);
            SetFloat(unit, "combatStats.attackRange", -1);

            var fields = ContentValidator.Validate(unit, new ContentDefinition[] { unit, race })
                .Where(issue => issue.Severity is ValidationSeverity.Error or ValidationSeverity.Critical)
                .Select(issue => issue.FieldName)
                .ToArray();
            Assert.That(fields, Does.Contain("goldCost"));
            Assert.That(fields, Does.Contain("shopPoolSize"));
            Assert.That(fields, Does.Contain("productionStats.spawnInterval"));
            Assert.That(fields, Does.Contain("productionStats.maximumPopulation"));
            Assert.That(fields, Does.Contain("combatStats.attackDamage"));
            Assert.That(fields, Does.Contain("combatStats.attackRange"));
            Destroy(unit, race);
        }

        [Test]
        public void Validate_WhenAscensionThresholdsAreNotOrdered_ReportsProgressionField()
        {
            var unit = CreateValidUnit("UNIT_TEST_ASCENSION", "Ascension Unit", out var race);
            SetInt(unit, "ascensionOneThreshold", 10);
            SetInt(unit, "ascensionTwoThreshold", 5);

            Assert.That(ContentValidator.Validate(unit).Any(issue => issue.FieldName == "ascensionTwoThreshold" && issue.Severity == ValidationSeverity.Error), Is.True);
            Destroy(unit, race);
        }

        [Test]
        public void Validate_WhenEvolutionListContainsMissingReference_ReportsExactList()
        {
            var unit = CreateValidUnit("UNIT_TEST_EVOLUTION", "Evolution Unit", out var race);
            SetMissingReference(unit, "ascensionOneOptions");

            Assert.That(ContentValidator.Validate(unit).Any(issue => issue.FieldName == "ascensionOneOptions" && issue.Severity == ValidationSeverity.Error), Is.True);
            Destroy(unit, race);
        }

        [Test]
        public void Validate_WhenEvolutionIsReferencedTwice_ReportsDuplicateReference()
        {
            var unit = CreateValidUnit("UNIT_TEST_DUPLICATE_EVOLUTION", "Duplicate Evolution Unit", out var race);
            var evolution = Create<EvolutionDefinition>("EVOLUTION_TEST_DUPLICATE", "Duplicate Evolution");
            SetObjectArray(unit, "ascensionOneOptions", evolution, evolution);

            Assert.That(ContentValidator.Validate(unit).Any(issue => issue.FieldName == "ascensionOneOptions" && issue.Message.Contains("more than once")), Is.True);
            Destroy(unit, race, evolution);
        }

        [Test]
        public void ModifyRuntimeState_WhenAssociatedDefinitionExists_DoesNotMutateAuthoredDefinition()
        {
            var unit = CreateValidUnit("UNIT_TEST_STATE_SEPARATION", "State Separation", out var race);
            float authoredHealth = unit.CombatStats.MaxHealth;
            var state = new UnitBlueprintState(unit.Id, "PLAYER_TEST")
            {
                CopiesPurchased = 7,
                AscensionLevel = AscensionLevel.AscensionOne,
                AssignedLane = BlueprintLane.Left,
                AssignedStance = BlueprintStance.Defense
            };
            var board = new BlueprintBoardState("PLAYER_TEST", 3, new[] { state });
            var placement = new BlueprintPlacementService(board);
            Assert.That(placement.Execute(BlueprintCommands.ActivateBlueprint(unit.Id, 2)).Success, Is.True);
            state.ChosenEvolutionIds.Add("EVOLUTION_TEST");
            state.SelectedPerCopyStatUpgradeIds.Add("refinement.hp");
            state.AttachedResearchIds.Add("RESEARCH_TEST");

            Assert.That(unit.Id, Is.EqualTo("UNIT_TEST_STATE_SEPARATION"));
            Assert.That(unit.CombatStats.MaxHealth, Is.EqualTo(authoredHealth));
            Assert.That(typeof(UnitBlueprintState).GetFields(BindingFlags.Instance | BindingFlags.Public)
                .Any(field => typeof(ContentDefinition).IsAssignableFrom(field.FieldType)), Is.False);
            Assert.That(typeof(UnitBlueprintState).GetProperties()
                .Any(property => typeof(ContentDefinition).IsAssignableFrom(property.PropertyType)), Is.False);
            Destroy(unit, race);
        }

        [Test]
        public void RuntimeDefinitionSurface_WhenInspected_HasNoPublicStatMutationPath()
        {
            Assert.That(typeof(UnitCombatStats).GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
            Assert.That(typeof(UnitProductionStats).GetFields(BindingFlags.Instance | BindingFlags.Public), Is.Empty);
            Assert.That(typeof(UnitCombatStats).GetProperty(nameof(UnitCombatStats.MaxHealth))?.SetMethod, Is.Null);
            Assert.That(typeof(UnitProductionStats).GetProperty(nameof(UnitProductionStats.SpawnInterval))?.SetMethod, Is.Null);
        }

        [Test]
        public void CreateSamples_WhenRunTwice_DoesNotCreateDuplicateCanonicalIds()
        {
            ContentTools.CreateSamples();
            var firstPaths = GetCanonicalAssetPaths();
            ContentTools.CreateSamples();
            var secondPaths = GetCanonicalAssetPaths();

            CollectionAssert.AreEquivalent(firstPaths, secondPaths);
            Assert.That(GetAllAuthoredDefinitions().GroupBy(value => value.Id, StringComparer.OrdinalIgnoreCase).All(group => group.Count() == 1), Is.True);
        }

        [Test]
        public void CanonicalSampleContent_WhenValidated_HasNoBlockingIssues()
        {
            var definitions = GetAllAuthoredDefinitions();
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
        public void DefaultCatalog_WhenLoaded_ContainsEveryAuthoredDefinition()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(CatalogPath);
            Assert.That(catalog, Is.Not.Null);
            Assert.That(catalog.Definitions.Count, Is.EqualTo(GetAllAuthoredDefinitions().Count));
            Assert.DoesNotThrow(catalog.RebuildIndex);
        }

        [Test]
        public void ContentStudioAssets_WhenLoaded_UseUxmlAndUss()
        {
            Assert.That(AssetDatabase.LoadAssetAtPath<VisualTreeAsset>(StudioLayoutPath), Is.Not.Null);
            Assert.That(AssetDatabase.LoadAssetAtPath<StyleSheet>(StudioStylePath), Is.Not.Null);
        }

        private static List<ContentDefinition> GetAllAuthoredDefinitions()
        {
            return AssetDatabase.FindAssets("t:ContentDefinition", new[] { ContentTools.ContentRoot })
                .Select(guid => AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(definition => definition != null)
                .ToList();
        }

        private static string[] GetCanonicalAssetPaths()
        {
            string[] ids = { "RACE_HIVE", "HIVE_LARVA", "HIVE_SPIDER", "HIVE_BEETLE" };
            return GetAllAuthoredDefinitions().Where(definition => ids.Contains(definition.Id))
                .Select(AssetDatabase.GetAssetPath).OrderBy(path => path, StringComparer.Ordinal).ToArray();
        }

        private static T Create<T>(string id, string displayName) where T : ContentDefinition
        {
            var definition = ScriptableObject.CreateInstance<T>();
            definition.EditorInitialize(id, displayName);
            SetString(definition, "description", $"Test definition for {displayName}.");
            return definition;
        }

        private static UnitDefinition CreateValidUnit(string id, string displayName, out RaceDefinition race)
        {
            race = Create<RaceDefinition>($"RACE_{id}", $"Race for {displayName}");
            return CreateValidUnit(id, displayName, race);
        }

        private static UnitDefinition CreateValidUnit(string id, string displayName, RaceDefinition race)
        {
            var unit = Create<UnitDefinition>(id, displayName);
            SetObject(unit, "race", race);
            return unit;
        }

        private static GameContentCatalog CreateCatalog(params ContentDefinition[] definitions)
        {
            var catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
            catalog.EditorSetDefinitions(definitions);
            return catalog;
        }

        private static void SetMissingReference(UnityEngine.Object asset, string propertyName)
        {
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty(propertyName);
            property.arraySize = 1;
            property.GetArrayElementAtIndex(0).objectReferenceValue = null;
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObjectArray(UnityEngine.Object asset, string propertyName, params UnityEngine.Object[] values)
        {
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetTags(UnityEngine.Object asset, params string[] tags)
        {
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty("tags");
            property.arraySize = tags.Length;
            for (int index = 0; index < tags.Length; index++) property.GetArrayElementAtIndex(index).stringValue = tags[index];
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetObject(UnityEngine.Object asset, string propertyName, UnityEngine.Object value) =>
            Set(asset, propertyName, property => property.objectReferenceValue = value);
        private static void SetString(UnityEngine.Object asset, string propertyName, string value) =>
            Set(asset, propertyName, property => property.stringValue = value);
        private static void SetInt(UnityEngine.Object asset, string propertyName, int value) =>
            Set(asset, propertyName, property => property.intValue = value);
        private static void SetFloat(UnityEngine.Object asset, string propertyName, float value) =>
            Set(asset, propertyName, property => property.floatValue = value);
        private static void SetEnum(UnityEngine.Object asset, string propertyName, int value) =>
            Set(asset, propertyName, property => property.enumValueIndex = value);

        private static void Set(UnityEngine.Object asset, string propertyName, Action<SerializedProperty> assign)
        {
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty(propertyName);
            Assert.That(property, Is.Not.Null, propertyName);
            assign(property);
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void Destroy(params UnityEngine.Object[] objects)
        {
            foreach (var value in objects.Where(value => value != null)) UnityEngine.Object.DestroyImmediate(value);
        }
    }
}
