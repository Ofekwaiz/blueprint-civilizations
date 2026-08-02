using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.Content.Validation;
using BlueprintCivilizations.Core;
using UnityEditor;
using UnityEngine;

namespace BlueprintCivilizations.Editor.ContentStudio
{
    /// <summary>Menu commands and shared editor operations for authored content.</summary>
    public static class ContentTools
    {
        private const float PrototypeSpawnIntervalSeconds = 6f;

        public const string ContentRoot = "Assets/_Game/Content/Assets";
        public const string ConfigurationFolder = ContentRoot + "/Configuration";
        public const string DefaultCatalogPath = ConfigurationFolder + "/GameContentCatalog.asset";

        [MenuItem("Tools/Blueprint Civilizations/Validate All Content")]
        public static void ValidateAll()
        {
            var all = GetAllDefinitions();
            var issues = ContentValidator.ValidateAll(all, AssetDatabase.GetAssetPath);
            foreach (var issue in issues)
            {
                var definition = all.FirstOrDefault(candidate => candidate.Id == issue.DefinitionId);
                if (issue.Severity is ValidationSeverity.Error or ValidationSeverity.Critical)
                    Debug.LogError(issue.ToString(), definition);
                else if (issue.Severity == ValidationSeverity.Warning)
                    Debug.LogWarning(issue.ToString(), definition);
                else
                    Debug.Log(issue.ToString(), definition);
            }

            int blocking = issues.Count(issue => issue.Severity is ValidationSeverity.Error or ValidationSeverity.Critical);
            EditorUtility.DisplayDialog(
                "Content Validation",
                $"Validated {all.Count} assets. Blocking issues: {blocking}. Warnings: {issues.Count(issue => issue.Severity == ValidationSeverity.Warning)}. See Console for details.",
                "OK");
        }

        [MenuItem("Tools/Blueprint Civilizations/Rebuild Default Content Catalog")]
        public static void RebuildDefaultCatalog()
        {
            EnsureFolder(ConfigurationFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<GameContentCatalog>(DefaultCatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<GameContentCatalog>();
                AssetDatabase.CreateAsset(catalog, DefaultCatalogPath);
                Undo.RegisterCreatedObjectUndo(catalog, "Create content catalog");
            }

            Undo.RecordObject(catalog, "Rebuild content catalog");
            catalog.EditorSetDefinitions(GetAllDefinitions());
            catalog.RebuildIndex();
            EditorUtility.SetDirty(catalog);
            AssetDatabase.SaveAssets();
            Selection.activeObject = catalog;
            Debug.Log($"Rebuilt catalog with {catalog.Definitions.Count} definitions.", catalog);
        }

        [MenuItem("Tools/Blueprint Civilizations/Create or Repair Prototype Sample Content")]
        public static void CreateSamples()
        {
            EnsureFolder(ConfigurationFolder);

            var larvaMote = Create<AbilityDefinition>("Abilities/Hive/Ability_Hive_LarvaBiomassMote.asset", "ABILITY_HIVE_LARVA_BIOMASS_MOTE", "Biomass Mote", "On death, has a 20% chance to leave a Biomass Mote worth 0.25 Biomass.");
            SetTrigger(larvaMote, TriggerEventType.OnDeath, "ownerRaceResource", ModifierOperation.FlatAdd, 0.25f, 0, 1, 0, 0.2f);

            var spiderSlow = Create<AbilityDefinition>("Abilities/Hive/Ability_Hive_WebSlow.asset", "ABILITY_HIVE_WEB_SLOW", "Web Slow", "Every third attack applies 15% slow for 2 seconds.");
            SetTrigger(spiderSlow, TriggerEventType.OnAttack, "target.moveSpeed", ModifierOperation.PercentMultiply, -15, 2, 0, 0, 1, 3);

            var beetleCarapace = Create<AbilityDefinition>("Abilities/Hive/Ability_Hive_ShellCarapace.asset", "ABILITY_HIVE_SHELL_CARAPACE", "Shell Carapace", "For the first 4 seconds after spawning, gain 25 Armor.");
            SetTrigger(beetleCarapace, TriggerEventType.OnSpawn, "self.armor", ModifierOperation.FlatAdd, 25, 4, 0, 1);

            var nexus = Create<NexusDefinition>("Races/Hive/Nexus_Hive_BroodQueen.asset", "NEXUS_HIVE_BROOD_QUEEN", "Brood Queen", "Hive Nexus and adaptation engine.");
            SetFloat(nexus, "baseHealth", 1000);
            SetFloat(nexus, "regenerationDelaySeconds", 5);

            var race = Create<RaceDefinition>("Races/Hive/Race_Hive.asset", "RACE_HIVE", "Hive", "Adaptive biological civilization focused on replacement, evolution, poison, armor, flyers, parasites, and creep support.");
            SetString(race, "uniqueResourceName", "Biomass");
            SetColor(race, "identityColor", new Color32(0x54, 0x82, 0x35, 0xFF));
            SetObject(race, "nexus", nexus);
            SetTags(race, "Hive", "Organic");

            var larva = Create<UnitDefinition>("Units/Hive/Unit_Hive_Larva.asset", "HIVE_LARVA", "Larva Brood", "Melee brood. On death, may leave a Biomass Mote worth 0.25 Biomass.");
            ConfigureUnit(larva, race, new UnitSeed(1, 1, 70, 7, 1f, 6, "Melee"), larvaMote, "Hive", "Organic", "Swarm", "Melee");

            var spider = Create<UnitDefinition>("Units/Hive/Unit_Hive_Spider.asset", "HIVE_SPIDER", "Web Spider", "Ranged brood. Every third attack applies a 15% slow for 2 seconds.");
            ConfigureUnit(spider, race, new UnitSeed(1, 2, 95, 12, 0.85f, 4, "Ranged"), spiderSlow, "Hive", "Organic", "Ranged");

            var beetle = Create<UnitDefinition>("Units/Hive/Unit_Hive_Beetle.asset", "HIVE_BEETLE", "Shell Beetle", "Melee tank. Gains 25 Armor for the first 4 seconds after spawning.");
            ConfigureUnit(beetle, race, new UnitSeed(1, 2, 180, 13, 0.65f, 3, "Melee tank"), beetleCarapace, "Hive", "Organic", "Frontline");

            var venom = Create<EvolutionDefinition>("Evolutions/Hive/Evolution_Hive_Spider_Venom.asset", "EVOLUTION_HIVE_SPIDER_VENOM", "Venom Spider", "Ascension I path that adds a poison damage-over-time identity.");
            SetString(venom, "sourceBlueprintId", "HIVE_SPIDER");
            SetEnum(venom, "requiredAscension", (int)AscensionLevel.AscensionOne);
            SetTags(venom, "Hive", "Organic", "Poison");
            SetModifierList(venom, "modifiers", new ModifierSeed("self", "poisonPower", ModifierOperation.PercentAdd, 15));
            SetObjectArray(spider, "ascensionOneOptions", venom);

            var creep = Create<StructureDefinition>("Structures/Hive/Structure_Hive_CreepTumor.asset", "HIVE_STR_01", "Creep Tumor", "Adjacent Organic blueprints gain move and spawn speed; its battlefield aura slows enemies.");
            SetObject(creep, "race", race);
            SetEnum(creep, "tier", (int)ContentTier.Tier1 - 1);
            SetInt(creep, "goldCost", 1);
            SetInt(creep, "shopPoolSize", 18);
            SetFloat(creep, "baseHealth", 120);
            SetString(creep, "rulesSummary", "Adjacent Organic blueprints gain +8% move speed and +5% spawn speed. Battlefield aura slows enemies 5%.");
            SetModifierList(creep, "adjacencyModifiers",
                new ModifierSeed("adjacent.tag:Organic", "moveSpeed", ModifierOperation.PercentAdd, 8),
                new ModifierSeed("adjacent.tag:Organic", "spawnSpeed", ModifierOperation.PercentAdd, 5));
            SetTags(creep, "Hive", "Organic", "Structure", "Creep");

            var acidBlood = Create<ResearchDefinition>("Research/Hive/Research_Hive_AcidBlood.asset", "HIVE_RES_01", "Acid Blood", "On death, deal 8% maximum-HP magic damage nearby, capped against bosses.");
            SetObject(acidBlood, "affinityRace", race);
            SetEnum(acidBlood, "rarity", (int)ContentRarity.Race);
            SetTrigger(acidBlood, TriggerEventType.OnDeath, "nearbyEnemies.magicDamageFromSourceMaxHealth", ModifierOperation.PercentAdd, 8, 0, 1, 0);
            SetTags(acidBlood, "Hive", "Organic", "DeathTrigger");

            var hiveHeart = Create<ArtifactDefinition>("Artifacts/Hive/Artifact_Hive_LivingHiveHeart.asset", "HIVE_ART_01", "Living Hive Heart", "All Hive spawn intervals are reduced by 6%; Nexus health is reduced by 8%.");
            SetObject(hiveHeart, "affinityRace", race);
            SetEnum(hiveHeart, "rarity", (int)ContentRarity.Race);
            SetModifierList(hiveHeart, "modifiers",
                new ModifierSeed("all.tag:Hive", "spawnInterval", ModifierOperation.PercentMultiply, -6, 0, ModifierDurationScope.PermanentRun),
                new ModifierSeed("ownerNexus", "maxHealth", ModifierOperation.PercentMultiply, -8, 0, ModifierDurationScope.PermanentRun));
            SetTags(hiveHeart, "Hive", "Artifact", "Swarm");

            SetObject(race, "startingUnit", larva);
            SetObjectArray(race, "permittedUnits", larva, spider, beetle);
            SetObjectArray(race, "permittedStructures", creep);
            SetObjectArray(race, "permittedResearch", acidBlood);
            SetObjectArray(race, "permittedArtifacts", hiveHeart);
            SetObjectArray(race, "ruleModules", larvaMote, spiderSlow, beetleCarapace);

            var config = Create<GameBalanceConfigurationDefinition>("Configuration/Config_GameBalance.asset", "CONFIG_GAME_BALANCE_V0_1", "Prototype Game Balance", "Authoritative configurable starting values for match, economy, shop, and combat timing.");
            ConfigureShopOdds(config);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            RebuildDefaultCatalog();
            Debug.Log("Canonical Milestone 0 prototype content created or repaired.", race);
        }

        public static List<ContentDefinition> GetAllDefinitions()
        {
            return AssetDatabase.FindAssets("t:ContentDefinition")
                .Select(guid => AssetDatabase.LoadAssetAtPath<ContentDefinition>(AssetDatabase.GUIDToAssetPath(guid)))
                .Where(definition => definition != null)
                .OrderBy(definition => definition.Id, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        public static void EnsureFolder(string path)
        {
            string current = "Assets";
            foreach (string part in path.Split('/').Skip(1))
            {
                string next = current + "/" + part;
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, part);
                current = next;
            }
        }

        public static string GetAuthoringFolder(Type definitionType)
        {
            if (definitionType == typeof(UnitDefinition)) return ContentRoot + "/Units/Custom";
            if (definitionType == typeof(StructureDefinition)) return ContentRoot + "/Structures/Custom";
            if (definitionType == typeof(ResearchDefinition)) return ContentRoot + "/Research/Custom";
            if (definitionType == typeof(ArtifactDefinition)) return ContentRoot + "/Artifacts/Custom";
            if (definitionType == typeof(EvolutionDefinition)) return ContentRoot + "/Evolutions/Custom";
            if (definitionType == typeof(AbilityDefinition)) return ContentRoot + "/Abilities/Custom";
            if (definitionType == typeof(PhilosophyDefinition) || definitionType == typeof(AugmentDefinition)) return ContentRoot + "/Philosophies/Custom";
            if (definitionType == typeof(GameBalanceConfigurationDefinition)) return ConfigurationFolder;
            if (definitionType == typeof(RaceDefinition) || definitionType == typeof(NexusDefinition)) return ContentRoot + "/Races/Custom";
            return ContentRoot + "/Custom";
        }

        private static T Create<T>(string fileName, string id, string displayName, string description) where T : ContentDefinition
        {
            string path = ContentRoot + "/" + fileName;
            var asset = FindById<T>(id) ?? AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset == null)
            {
                EnsureFolder(path.Substring(0, path.LastIndexOf('/')));
                asset = ScriptableObject.CreateInstance<T>();
                asset.EditorInitialize(id, displayName);
                AssetDatabase.CreateAsset(asset, path);
                Undo.RegisterCreatedObjectUndo(asset, $"Create {displayName}");
            }
            SetString(asset, "displayName", displayName);
            SetString(asset, "description", description);
            SetInt(asset, "dataVersion", 1);
            SetBool(asset, "isEnabled", true);
            return asset;
        }

        private static T FindById<T>(string id) where T : ContentDefinition
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(guid => AssetDatabase.LoadAssetAtPath<T>(AssetDatabase.GUIDToAssetPath(guid)))
                .FirstOrDefault(asset => asset != null && string.Equals(asset.Id, id, StringComparison.OrdinalIgnoreCase));
        }

        private static void ConfigureUnit(UnitDefinition unit, RaceDefinition race, UnitSeed seed, AbilityDefinition ability, params string[] tags)
        {
            SetObject(unit, "race", race);
            SetBool(unit, "isNeutral", false);
            SetEnum(unit, "tier", seed.Tier - 1);
            SetInt(unit, "goldCost", seed.Cost);
            SetEnum(unit, "poolKind", (int)ContentPoolKind.PrivateRace);
            SetInt(unit, "shopPoolSize", 18);
            SetFloat(unit, "baseShopWeight", 1);
            SetString(unit, "role", seed.Role);
            SetFloat(unit, "combatStats.maxHealth", seed.Health);
            SetFloat(unit, "combatStats.attackDamage", seed.Damage);
            SetFloat(unit, "combatStats.attackIntervalSeconds", 1f / seed.AttacksPerSecond);
            SetFloat(unit, "productionStats.spawnInterval", PrototypeSpawnIntervalSeconds);
            SetInt(unit, "productionStats.maximumPopulation", seed.MaximumPopulation);
            SetInt(unit, "productionStats.spawnBatchSize", 1);
            SetObjectArray(unit, "abilities", ability);
            SetPerCopyUpgrades(unit);
            SetTags(unit, tags);
        }

        private static void SetPerCopyUpgrades(UnitDefinition unit)
        {
            var serialized = new SerializedObject(unit);
            var upgrades = serialized.FindProperty("permittedPerCopyStatUpgrades");
            var seeds = new[]
            {
                new UpgradeSeed("refinement.hp", "Reinforced Pattern", "maxHealth", ModifierOperation.PercentAdd, 8),
                new UpgradeSeed("refinement.damage", "Sharpened Pattern", "attackDamage", ModifierOperation.PercentAdd, 8),
                new UpgradeSeed("refinement.spawn", "Accelerated Pattern", "spawnInterval", ModifierOperation.PercentMultiply, -6),
                new UpgradeSeed("refinement.population", "Expanded Pattern", "maximumPopulation", ModifierOperation.FlatAdd, 1),
                new UpgradeSeed("refinement.movement", "Mobile Pattern", "movementSpeed", ModifierOperation.PercentAdd, 5)
            };
            upgrades.arraySize = seeds.Length;
            for (int index = 0; index < seeds.Length; index++)
            {
                var upgrade = upgrades.GetArrayElementAtIndex(index);
                upgrade.FindPropertyRelative("id").stringValue = seeds[index].Id;
                upgrade.FindPropertyRelative("displayName").stringValue = seeds[index].DisplayName;
                upgrade.FindPropertyRelative("maximumSelections").intValue = 3;
                var modifier = upgrade.FindPropertyRelative("modifier");
                modifier.FindPropertyRelative("targetSelector").stringValue = "self";
                modifier.FindPropertyRelative("stat").stringValue = seeds[index].Stat;
                modifier.FindPropertyRelative("operation").enumValueIndex = (int)seeds[index].Operation;
                modifier.FindPropertyRelative("value").floatValue = seeds[index].Value;
                modifier.FindPropertyRelative("durationScope").enumValueIndex = (int)ModifierDurationScope.PlanningSnapshot;
                modifier.FindPropertyRelative("durationSeconds").floatValue = 0;
            }
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(unit);
        }

        private static void ConfigureShopOdds(GameBalanceConfigurationDefinition configuration)
        {
            var serialized = new SerializedObject(configuration);
            var rows = serialized.FindProperty("shopTierOdds");
            float[][] values =
            {
                new float[] { 100, 0, 0, 0, 0 },
                new float[] { 70, 30, 0, 0, 0 },
                new float[] { 45, 40, 15, 0, 0 },
                new float[] { 25, 35, 30, 10, 0 },
                new float[] { 10, 25, 35, 23, 7 }
            };
            rows.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++)
            {
                var row = rows.GetArrayElementAtIndex(index);
                row.FindPropertyRelative("civilizationLevel").intValue = index + 1;
                var percentages = row.FindPropertyRelative("tierPercentages");
                percentages.arraySize = 5;
                for (int tier = 0; tier < 5; tier++) percentages.GetArrayElementAtIndex(tier).floatValue = values[index][tier];
            }
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(configuration);
        }

        private static void SetTrigger(ContentDefinition asset, TriggerEventType eventType, string stat, ModifierOperation operation, float value, float duration, int maximumTriggers, float cooldown, float probability = 1, int everyNthEvent = 1)
        {
            var serialized = new SerializedObject(asset);
            var triggers = serialized.FindProperty("triggers");
            triggers.arraySize = 1;
            var trigger = triggers.GetArrayElementAtIndex(0);
            trigger.FindPropertyRelative("eventType").enumValueIndex = (int)eventType;
            trigger.FindPropertyRelative("probability").floatValue = probability;
            trigger.FindPropertyRelative("everyNthEvent").intValue = everyNthEvent;
            trigger.FindPropertyRelative("cooldownSeconds").floatValue = cooldown;
            trigger.FindPropertyRelative("maximumTriggers").intValue = maximumTriggers;
            SetModifierArray(trigger.FindPropertyRelative("modifiers"), new[] { new ModifierSeed("eventTarget", stat, operation, value, duration, ModifierDurationScope.CombatDynamic) });
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private static void SetModifierList(ContentDefinition asset, string propertyName, params ModifierSeed[] seeds)
        {
            var serialized = new SerializedObject(asset);
            SetModifierArray(serialized.FindProperty(propertyName), seeds);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private static void SetModifierArray(SerializedProperty array, IReadOnlyList<ModifierSeed> seeds)
        {
            array.arraySize = seeds.Count;
            for (int index = 0; index < seeds.Count; index++)
            {
                var element = array.GetArrayElementAtIndex(index);
                element.FindPropertyRelative("targetSelector").stringValue = seeds[index].Target;
                element.FindPropertyRelative("stat").stringValue = seeds[index].Stat;
                element.FindPropertyRelative("operation").enumValueIndex = (int)seeds[index].Operation;
                element.FindPropertyRelative("value").floatValue = seeds[index].Value;
                element.FindPropertyRelative("durationScope").enumValueIndex = (int)seeds[index].DurationScope;
                element.FindPropertyRelative("durationSeconds").floatValue = seeds[index].Duration;
            }
        }

        private static void SetTags(ContentDefinition asset, params string[] values)
        {
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty("tags");
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).stringValue = values[index];
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private static void SetObjectArray(ContentDefinition asset, string propertyName, params UnityEngine.Object[] values)
        {
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty(propertyName);
            property.arraySize = values.Length;
            for (int index = 0; index < values.Length; index++) property.GetArrayElementAtIndex(index).objectReferenceValue = values[index];
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private static void SetString(UnityEngine.Object asset, string propertyName, string value) => Set(asset, propertyName, property => property.stringValue = value);
        private static void SetInt(UnityEngine.Object asset, string propertyName, int value) => Set(asset, propertyName, property => property.intValue = value);
        private static void SetFloat(UnityEngine.Object asset, string propertyName, float value) => Set(asset, propertyName, property => property.floatValue = value);
        private static void SetBool(UnityEngine.Object asset, string propertyName, bool value) => Set(asset, propertyName, property => property.boolValue = value);
        private static void SetEnum(UnityEngine.Object asset, string propertyName, int value) => Set(asset, propertyName, property => property.enumValueIndex = value);
        private static void SetColor(UnityEngine.Object asset, string propertyName, Color value) => Set(asset, propertyName, property => property.colorValue = value);
        private static void SetObject(UnityEngine.Object asset, string propertyName, UnityEngine.Object value) => Set(asset, propertyName, property => property.objectReferenceValue = value);

        private static void Set(UnityEngine.Object asset, string propertyName, Action<SerializedProperty> assign)
        {
            var serialized = new SerializedObject(asset);
            var property = serialized.FindProperty(propertyName);
            if (property == null) throw new InvalidOperationException($"Serialized property '{propertyName}' was not found on {asset.GetType().Name}.");
            assign(property);
            serialized.ApplyModifiedProperties();
            EditorUtility.SetDirty(asset);
        }

        private readonly struct ModifierSeed
        {
            public ModifierSeed(string target, string stat, ModifierOperation operation, float value, float duration = 0, ModifierDurationScope durationScope = ModifierDurationScope.PlanningSnapshot)
            {
                Target = target;
                Stat = stat;
                Operation = operation;
                Value = value;
                Duration = duration;
                DurationScope = durationScope;
            }

            public string Target { get; }
            public string Stat { get; }
            public ModifierOperation Operation { get; }
            public float Value { get; }
            public float Duration { get; }
            public ModifierDurationScope DurationScope { get; }
        }

        private readonly struct UnitSeed
        {
            public UnitSeed(int tier, int cost, float health, float damage, float attacksPerSecond, int maximumPopulation, string role)
            {
                Tier = tier;
                Cost = cost;
                Health = health;
                Damage = damage;
                AttacksPerSecond = attacksPerSecond;
                MaximumPopulation = maximumPopulation;
                Role = role;
            }

            public int Tier { get; }
            public int Cost { get; }
            public float Health { get; }
            public float Damage { get; }
            public float AttacksPerSecond { get; }
            public int MaximumPopulation { get; }
            public string Role { get; }
        }

        private readonly struct UpgradeSeed
        {
            public UpgradeSeed(string id, string displayName, string stat, ModifierOperation operation, float value)
            {
                Id = id;
                DisplayName = displayName;
                Stat = stat;
                Operation = operation;
                Value = value;
            }

            public string Id { get; }
            public string DisplayName { get; }
            public string Stat { get; }
            public ModifierOperation Operation { get; }
            public float Value { get; }
        }
    }
}
