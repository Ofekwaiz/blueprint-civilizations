using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Content.Definitions;

namespace BlueprintCivilizations.Content.Validation
{
    /// <summary>Validates definition identity, references, and type-specific authoring rules.</summary>
    public static class ContentValidator
    {
        public static IReadOnlyList<ValidationIssue> Validate(
            ContentDefinition definition,
            IEnumerable<ContentDefinition> allDefinitions = null,
            Func<ContentDefinition, string> assetPathResolver = null)
        {
            var all = (allDefinitions ?? Enumerable.Empty<ContentDefinition>()).Where(value => value != null).ToList();
            if (definition != null && all.All(value => value != definition)) all.Add(definition);
            return Validate(definition, new ValidationContext(all, assetPathResolver));
        }

        public static IReadOnlyList<ValidationIssue> Validate(ContentDefinition definition, ValidationContext context)
        {
            var issues = new List<ValidationIssue>();
            if (definition == null)
            {
                issues.Add(new ValidationIssue(
                    ValidationSeverity.Critical, "<missing>", "", "definition", "Definition is null.",
                    "Remove the missing catalog entry or restore the referenced asset."));
                return issues;
            }

            ValidateCommon(definition, context, issues);
            switch (definition)
            {
                case UnitDefinition unit: ValidateUnit(unit, context, issues); break;
                case StructureDefinition structure: ValidateStructure(structure, context, issues); break;
                case RaceDefinition race: ValidateRace(race, context, issues); break;
                case ResearchDefinition research: ValidateResearch(research, context, issues); break;
                case ArtifactDefinition artifact: ValidateArtifact(artifact, context, issues); break;
                case EvolutionDefinition evolution: ValidateEvolution(evolution, context, issues); break;
                case AbilityDefinition ability: ValidateAbility(ability, context, issues); break;
                case NexusDefinition nexus: ValidateNexus(nexus, context, issues); break;
                case PhilosophyDefinition philosophy: ValidatePhilosophy(philosophy, context, issues); break;
                case AugmentDefinition augment: ValidateAugment(augment, context, issues); break;
                case GameBalanceConfigurationDefinition configuration: ValidateConfiguration(configuration, context, issues); break;
            }

            return issues;
        }

        public static IReadOnlyList<ValidationIssue> ValidateAll(
            IEnumerable<ContentDefinition> definitions,
            Func<ContentDefinition, string> assetPathResolver = null)
        {
            var all = definitions?.Where(value => value != null).ToList() ?? new List<ContentDefinition>();
            var context = new ValidationContext(all, assetPathResolver);
            return all.SelectMany(definition => Validate(definition, context)).ToList();
        }

        private static void ValidateCommon(ContentDefinition definition, ValidationContext context, List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(definition.Id))
                Add(definition, context, issues, ValidationSeverity.Error, "id", "ID is required.", "Assign a stable ID in Content Studio.");
            if (string.IsNullOrWhiteSpace(definition.DisplayName))
                Add(definition, context, issues, ValidationSeverity.Error, "displayName", "Display name is required.", "Enter a player-facing name.");
            if (string.IsNullOrWhiteSpace(definition.Description))
                Add(definition, context, issues, ValidationSeverity.Error, "description", "Description is required.", "Enter player-facing rules text or a concise content description.");
            if (definition.DataVersion < 1)
                Add(definition, context, issues, ValidationSeverity.Error, "dataVersion", "Data version must be at least 1.", "Set the schema version to 1 or later.");

            if (!string.IsNullOrWhiteSpace(definition.Id))
            {
                int count = context.AllDefinitions.Count(candidate => candidate != null &&
                    string.Equals(candidate.Id, definition.Id, StringComparison.OrdinalIgnoreCase));
                if (count > 1)
                    Add(definition, context, issues, ValidationSeverity.Critical, "id", $"Duplicate ID '{definition.Id}'.", "Assign a new stable ID to the duplicate asset.");
            }

            if (definition.Tags.Any(string.IsNullOrWhiteSpace))
                Add(definition, context, issues, ValidationSeverity.Warning, "tags", "Tag list contains an empty value.", "Remove empty tag entries.");
            if (definition.Tags.Where(tag => !string.IsNullOrWhiteSpace(tag)).GroupBy(tag => tag, StringComparer.OrdinalIgnoreCase).Any(group => group.Count() > 1))
                Add(definition, context, issues, ValidationSeverity.Warning, "tags", "Tag list contains duplicates.", "Keep each tag once.");
        }

        private static void ValidateUnit(UnitDefinition unit, ValidationContext context, List<ValidationIssue> issues)
        {
            if (!unit.IsNeutral && unit.Race == null)
                Add(unit, context, issues, ValidationSeverity.Error, "race", "A non-neutral unit requires a race.", "Assign its owning race.");
            if (unit.IsNeutral && unit.PoolKind != ContentPoolKind.SharedNeutral)
                Add(unit, context, issues, ValidationSeverity.Error, "poolKind", "A neutral unit must use the shared neutral pool.", "Set Pool Kind to Shared Neutral.");
            if ((int)unit.Tier < 1 || (int)unit.Tier > 5)
                Add(unit, context, issues, ValidationSeverity.Error, "tier", "Tier must be between 1 and 5.", "Choose a valid content tier.");
            if (unit.GoldCost < 0) Add(unit, context, issues, ValidationSeverity.Error, "goldCost", "Gold cost cannot be negative.", "Set a non-negative cost.");
            if (unit.ShopPoolSize < 1) Add(unit, context, issues, ValidationSeverity.Error, "shopPoolSize", "Shop pool size must be at least 1.", "Use the configured tier pool size.");
            if (unit.BaseShopWeight <= 0) Add(unit, context, issues, ValidationSeverity.Error, "baseShopWeight", "Shop weight must be greater than zero.", "Set a positive offer weight.");
            if (unit.CombatStats == null)
                Add(unit, context, issues, ValidationSeverity.Critical, "combatStats", "Combat stats are missing.", "Restore the combat stat block.");
            else
            {
                if (unit.CombatStats.MaxHealth <= 0) Add(unit, context, issues, ValidationSeverity.Error, "combatStats.maxHealth", "Max health must be greater than zero.", "Set a positive value.");
                if (unit.CombatStats.AttackDamage < 0) Add(unit, context, issues, ValidationSeverity.Error, "combatStats.attackDamage", "Attack damage cannot be negative.", "Set a non-negative value.");
                if (unit.CombatStats.AttacksPerSecond <= 0) Add(unit, context, issues, ValidationSeverity.Error, "combatStats.attacksPerSecond", "Attacks per second must be greater than zero.", "Set a positive attack rate.");
            }
            if (unit.ProductionStats == null)
                Add(unit, context, issues, ValidationSeverity.Critical, "productionStats", "Production stats are missing.", "Restore the production stat block.");
            else
            {
                if (unit.ProductionStats.SpawnInterval <= 0) Add(unit, context, issues, ValidationSeverity.Error, "productionStats.spawnInterval", "Spawn interval must be greater than zero.", "Set a positive interval.");
                if (unit.ProductionStats.MaximumPopulation < 1) Add(unit, context, issues, ValidationSeverity.Error, "productionStats.maximumPopulation", "Maximum population must be at least 1.", "Set a legal population cap.");
                if (unit.ProductionStats.SpawnBatchSize < 1) Add(unit, context, issues, ValidationSeverity.Error, "productionStats.spawnBatchSize", "Spawn batch size must be at least 1.", "Set a legal batch size.");
            }
            if (unit.AscensionOneThreshold < 1 || unit.AscensionTwoThreshold <= unit.AscensionOneThreshold)
                Add(unit, context, issues, ValidationSeverity.Error, "ascensionTwoThreshold", "Ascension thresholds must be positive and ordered.", "Use prototype thresholds 5 and 10 unless configuration changes them.");
            ValidateReferences(unit, context, issues, "abilities", unit.Abilities);
            ValidateReferences(unit, context, issues, "ascensionOneOptions", unit.AscensionOneOptions);
            ValidateReferences(unit, context, issues, "ascensionTwoOptions", unit.AscensionTwoOptions);
            if (unit.AscensionOneOptions.Count == 0)
                Add(unit, context, issues, ValidationSeverity.Warning, "ascensionOneOptions", "No Ascension I evolution is assigned.", "Author the legal evolution paths before progression is implemented.");
            if (unit.VisualPrefab == null)
                Add(unit, context, issues, ValidationSeverity.Warning, "visualPrefab", "Visual prefab is not assigned.", "Assign a presentation prefab before runtime presentation work.");
        }

        private static void ValidateStructure(StructureDefinition structure, ValidationContext context, List<ValidationIssue> issues)
        {
            if (!structure.IsNeutral && structure.Race == null) Add(structure, context, issues, ValidationSeverity.Error, "race", "A non-neutral structure requires a race.", "Assign its owning race.");
            if (structure.IsNeutral && structure.PoolKind != ContentPoolKind.SharedNeutral) Add(structure, context, issues, ValidationSeverity.Error, "poolKind", "A neutral structure must use the shared neutral pool.", "Set Pool Kind to Shared Neutral.");
            if (structure.GoldCost < 0) Add(structure, context, issues, ValidationSeverity.Error, "goldCost", "Gold cost cannot be negative.", "Set a non-negative cost.");
            if (structure.ShopPoolSize < 1) Add(structure, context, issues, ValidationSeverity.Error, "shopPoolSize", "Shop pool size must be at least 1.", "Use the configured tier pool size.");
            if (structure.BaseHealth <= 0) Add(structure, context, issues, ValidationSeverity.Error, "baseHealth", "Base health must be greater than zero.", "Set a positive durability value.");
            if (structure.MaximumPopulation < 1) Add(structure, context, issues, ValidationSeverity.Error, "maximumPopulation", "Maximum population must be at least 1.", "Use 1 unless the structure explicitly supports more.");
            if (structure.AdjacencyModifiers.Count == 0 && structure.Abilities.Count == 0)
                Add(structure, context, issues, ValidationSeverity.Warning, "adjacencyModifiers", "Structure has no structured effect.", "Add an adjacency modifier or ability module.");
            ValidateModifiers(structure, context, issues, "adjacencyModifiers", structure.AdjacencyModifiers);
        }

        private static void ValidateRace(RaceDefinition race, ValidationContext context, List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(race.UniqueResourceName)) Add(race, context, issues, ValidationSeverity.Error, "uniqueResourceName", "Race resource name is required.", "Enter the Bible-defined resource name.");
            if (race.Nexus == null) Add(race, context, issues, ValidationSeverity.Error, "nexus", "Race Nexus definition is required.", "Assign the race's Nexus definition.");
            if (race.StartingUnit == null) Add(race, context, issues, ValidationSeverity.Error, "startingUnit", "Starting unit is required.", "Assign the Bible-defined starting blueprint.");
        }

        private static void ValidateResearch(ResearchDefinition research, ValidationContext context, List<ValidationIssue> issues)
        {
            if (research.Modifiers.Count == 0 && research.Triggers.Count == 0) Add(research, context, issues, ValidationSeverity.Error, "modifiers", "Research has no structured effect.", "Add at least one modifier or trigger.");
            if (research.ShopWeight <= 0) Add(research, context, issues, ValidationSeverity.Error, "shopWeight", "Shop weight must be greater than zero.", "Set a positive offer weight.");
            ValidateModifiers(research, context, issues, "modifiers", research.Modifiers);
            ValidateTriggers(research, context, issues, research.Triggers);
        }

        private static void ValidateArtifact(ArtifactDefinition artifact, ValidationContext context, List<ValidationIssue> issues)
        {
            if (artifact.Modifiers.Count == 0 && artifact.Triggers.Count == 0) Add(artifact, context, issues, ValidationSeverity.Error, "modifiers", "Artifact has no structured effect.", "Add at least one modifier or trigger.");
            if (artifact.Unique && artifact.Stackable) Add(artifact, context, issues, ValidationSeverity.Error, "stackable", "An artifact cannot be both unique and stackable.", "Choose one acquisition model.");
            if (artifact.ShopWeight <= 0) Add(artifact, context, issues, ValidationSeverity.Error, "shopWeight", "Shop weight must be greater than zero.", "Set a positive offer weight.");
            ValidateModifiers(artifact, context, issues, "modifiers", artifact.Modifiers);
            ValidateTriggers(artifact, context, issues, artifact.Triggers);
        }

        private static void ValidateEvolution(EvolutionDefinition evolution, ValidationContext context, List<ValidationIssue> issues)
        {
            if (string.IsNullOrWhiteSpace(evolution.SourceBlueprintId)) Add(evolution, context, issues, ValidationSeverity.Error, "sourceBlueprintId", "Source blueprint ID is required.", "Enter the canonical blueprint ID.");
            if (evolution.RequiredAscension == BlueprintCivilizations.Core.AscensionLevel.Base) Add(evolution, context, issues, ValidationSeverity.Error, "requiredAscension", "Evolution cannot require the base rank.", "Choose Ascension One or Two.");
            if (evolution.Modifiers.Count == 0 && evolution.GrantedAbilities.Count == 0 && evolution.GrantedTags.Count == 0)
                Add(evolution, context, issues, ValidationSeverity.Error, "modifiers", "Evolution does not change behavior, tags, or statistics.", "Add a structured identity change.");
            ValidateModifiers(evolution, context, issues, "modifiers", evolution.Modifiers);
        }

        private static void ValidateAbility(AbilityDefinition ability, ValidationContext context, List<ValidationIssue> issues)
        {
            if (ability.Modifiers.Count == 0 && ability.Triggers.Count == 0) Add(ability, context, issues, ValidationSeverity.Error, "modifiers", "Ability has no structured behavior.", "Add a modifier or trigger.");
            ValidateModifiers(ability, context, issues, "modifiers", ability.Modifiers);
            ValidateTriggers(ability, context, issues, ability.Triggers);
        }

        private static void ValidateNexus(NexusDefinition nexus, ValidationContext context, List<ValidationIssue> issues)
        {
            if (nexus.BaseHealth <= 0) Add(nexus, context, issues, ValidationSeverity.Error, "baseHealth", "Nexus health must be greater than zero.", "Set a positive value.");
            if (nexus.RegenerationDelaySeconds < 0) Add(nexus, context, issues, ValidationSeverity.Error, "regenerationDelaySeconds", "Regeneration delay cannot be negative.", "Set zero or a positive delay.");
        }

        private static void ValidatePhilosophy(PhilosophyDefinition philosophy, ValidationContext context, List<ValidationIssue> issues)
        {
            if (philosophy.SignalTags.Count == 0) Add(philosophy, context, issues, ValidationSeverity.Warning, "signalTags", "Philosophy has no signal tags.", "Add the Bible-defined investment signals.");
        }

        private static void ValidateAugment(AugmentDefinition augment, ValidationContext context, List<ValidationIssue> issues)
        {
            if (augment.Philosophy == null) Add(augment, context, issues, ValidationSeverity.Warning, "philosophy", "Augment has no philosophy affinity.", "Assign a philosophy or document it as adaptive/universal.");
            if (augment.Modifiers.Count == 0 && augment.Triggers.Count == 0) Add(augment, context, issues, ValidationSeverity.Error, "modifiers", "Augment has no structured effect.", "Add a modifier or trigger.");
            ValidateModifiers(augment, context, issues, "modifiers", augment.Modifiers);
            ValidateTriggers(augment, context, issues, augment.Triggers);
        }

        private static void ValidateConfiguration(GameBalanceConfigurationDefinition configuration, ValidationContext context, List<ValidationIssue> issues)
        {
            if (configuration.SimulationTicksPerSecond <= 0) Add(configuration, context, issues, ValidationSeverity.Error, "simulationTicksPerSecond", "Simulation tick rate must be greater than zero.", "Use a positive deterministic tick rate.");
            if (configuration.ShopTierOdds.Count != 5) Add(configuration, context, issues, ValidationSeverity.Error, "shopTierOdds", "Prototype configuration requires one shop-odds row for each of five levels.", "Author levels 1 through 5.");
            foreach (var row in configuration.ShopTierOdds.Where(row => row != null))
            {
                if (row.TierPercentages.Count != 5 || Math.Abs(row.TierPercentages.Sum() - 100f) > 0.01f)
                    Add(configuration, context, issues, ValidationSeverity.Error, "shopTierOdds", $"Level {row.CivilizationLevel} odds must contain five values totaling 100.", "Correct the tier probability row.");
            }
            if (configuration.ShopTierOdds.Where(row => row != null).GroupBy(row => row.CivilizationLevel).Any(group => group.Count() > 1))
                Add(configuration, context, issues, ValidationSeverity.Error, "shopTierOdds", "Civilization level appears more than once.", "Keep one odds row per level.");
        }

        private static void ValidateReferences<T>(ContentDefinition owner, ValidationContext context, List<ValidationIssue> issues, string fieldName, IReadOnlyList<T> references)
            where T : ContentDefinition
        {
            if (references.Any(value => value == null))
                Add(owner, context, issues, ValidationSeverity.Error, fieldName, "List contains a missing reference.", "Remove the empty entry or restore the referenced asset.");
            var duplicate = references.Where(value => value != null).GroupBy(value => value.Id, StringComparer.OrdinalIgnoreCase).FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
                Add(owner, context, issues, ValidationSeverity.Error, fieldName, $"Definition '{duplicate.Key}' is referenced more than once.", "Remove the duplicate reference.");
            if (references.Any(value => value != null && !value.IsEnabled))
                Add(owner, context, issues, ValidationSeverity.Warning, fieldName, "List references disabled content.", "Remove the reference or explicitly re-enable the dependency.");
        }

        private static void ValidateModifiers(ContentDefinition owner, ValidationContext context, List<ValidationIssue> issues, string fieldName, IReadOnlyList<ModifierSpec> modifiers)
        {
            if (modifiers.Any(modifier => modifier == null))
                Add(owner, context, issues, ValidationSeverity.Error, fieldName, "Modifier list contains a missing entry.", "Remove the empty entry or author the modifier.");
            foreach (var modifier in modifiers.Where(modifier => modifier != null))
            {
                if (string.IsNullOrWhiteSpace(modifier.TargetSelector)) Add(owner, context, issues, ValidationSeverity.Error, fieldName, "Modifier target selector is required.", "Choose a deterministic target selector.");
                if (string.IsNullOrWhiteSpace(modifier.Stat)) Add(owner, context, issues, ValidationSeverity.Error, fieldName, "Modifier statistic is required.", "Choose the statistic changed by this modifier.");
                if (modifier.DurationSeconds < 0) Add(owner, context, issues, ValidationSeverity.Error, fieldName, "Modifier duration cannot be negative.", "Use zero for a scope-defined duration or a positive number of seconds.");
            }
        }

        private static void ValidateTriggers(ContentDefinition owner, ValidationContext context, List<ValidationIssue> issues, IReadOnlyList<TriggerSpec> triggers)
        {
            if (triggers.Any(trigger => trigger == null))
                Add(owner, context, issues, ValidationSeverity.Error, "triggers", "Trigger list contains a missing entry.", "Remove the empty entry or author the trigger.");
            foreach (var trigger in triggers.Where(trigger => trigger != null))
            {
                if (trigger.Probability is < 0 or > 1) Add(owner, context, issues, ValidationSeverity.Error, "triggers.probability", "Trigger probability must be between 0 and 1.", "Enter a normalized probability.");
                if (trigger.EveryNthEvent < 1) Add(owner, context, issues, ValidationSeverity.Error, "triggers.everyNthEvent", "Trigger cadence must be at least 1.", "Use 1 for every event.");
                if (trigger.CooldownSeconds < 0) Add(owner, context, issues, ValidationSeverity.Error, "triggers.cooldownSeconds", "Trigger cooldown cannot be negative.", "Use zero or a positive cooldown.");
                if (trigger.Modifiers.Count == 0) Add(owner, context, issues, ValidationSeverity.Error, "triggers.modifiers", "Trigger has no actions.", "Add at least one structured trigger action.");
                ValidateModifiers(owner, context, issues, "triggers.modifiers", trigger.Modifiers);
            }
        }

        private static void Add(
            ContentDefinition definition,
            ValidationContext context,
            List<ValidationIssue> issues,
            ValidationSeverity severity,
            string fieldName,
            string message,
            string suggestedFix)
        {
            issues.Add(new ValidationIssue(
                severity,
                string.IsNullOrWhiteSpace(definition?.Id) ? "<missing-id>" : definition.Id,
                context?.GetAssetPath(definition) ?? "",
                fieldName,
                message,
                suggestedFix));
        }
    }
}
