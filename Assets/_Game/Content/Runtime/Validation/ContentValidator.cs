using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Content.Definitions;

namespace BlueprintCivilizations.Content.Validation
{
    public static class ContentValidator
    {
        public static IReadOnlyList<ValidationIssue> Validate(ContentDefinition definition, IEnumerable<ContentDefinition> allDefinitions = null)
        {
            var issues = new List<ValidationIssue>();
            if (definition == null)
            {
                issues.Add(new ValidationIssue(ValidationSeverity.Error, "Definition is null."));
                return issues;
            }
            if (string.IsNullOrWhiteSpace(definition.Id))
                issues.Add(new ValidationIssue(ValidationSeverity.Error, "ID is required."));
            if (string.IsNullOrWhiteSpace(definition.DisplayName))
                issues.Add(new ValidationIssue(ValidationSeverity.Error, "Display name is required."));
            if (allDefinitions != null && !string.IsNullOrWhiteSpace(definition.Id))
            {
                int count = allDefinitions.Count(d => d != null && string.Equals(d.Id, definition.Id, System.StringComparison.OrdinalIgnoreCase));
                if (count > 1) issues.Add(new ValidationIssue(ValidationSeverity.Error, $"Duplicate ID '{definition.Id}'."));
            }
            if (definition is UnitDefinition unit) ValidateUnit(unit, issues);
            return issues;
        }

        private static void ValidateUnit(UnitDefinition unit, List<ValidationIssue> issues)
        {
            if (unit.Race == null) issues.Add(new ValidationIssue(ValidationSeverity.Error, "Unit race is required."));
            if ((int)unit.Tier < 1 || (int)unit.Tier > 5) issues.Add(new ValidationIssue(ValidationSeverity.Error, "Tier must be between 1 and 5."));
            if (unit.GoldCost < 0) issues.Add(new ValidationIssue(ValidationSeverity.Error, "Gold cost cannot be negative."));
            if (unit.ShopPoolSize < 1) issues.Add(new ValidationIssue(ValidationSeverity.Error, "Shop pool size must be at least 1."));
            if (unit.CombatStats.maxHealth <= 0) issues.Add(new ValidationIssue(ValidationSeverity.Error, "Max health must be greater than zero."));
            if (unit.CombatStats.attackDamage < 0) issues.Add(new ValidationIssue(ValidationSeverity.Error, "Attack damage cannot be negative."));
            if (unit.CombatStats.attackInterval <= 0) issues.Add(new ValidationIssue(ValidationSeverity.Error, "Attack interval must be greater than zero."));
            if (unit.ProductionStats.spawnInterval <= 0) issues.Add(new ValidationIssue(ValidationSeverity.Error, "Spawn interval must be greater than zero."));
            if (unit.ProductionStats.maximumPopulation < 1) issues.Add(new ValidationIssue(ValidationSeverity.Error, "Maximum population must be at least 1."));
            if (unit.EvolutionOptions.Any(e => e == null)) issues.Add(new ValidationIssue(ValidationSeverity.Error, "Evolution list contains a missing reference."));
            var duplicateEvolution = unit.EvolutionOptions.Where(e => e != null).GroupBy(e => e.Id).FirstOrDefault(g => g.Count() > 1);
            if (duplicateEvolution != null) issues.Add(new ValidationIssue(ValidationSeverity.Error, $"Evolution '{duplicateEvolution.Key}' is referenced more than once."));
            if (unit.VisualPrefab == null) issues.Add(new ValidationIssue(ValidationSeverity.Warning, "Visual prefab is not assigned; placeholder visuals may be used."));
        }
    }
}
