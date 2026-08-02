using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    /// <summary>Authored Nexus identity, base statistics, and race-specific rule modules.</summary>
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Nexus", fileName = "Nexus_")]
    public sealed class NexusDefinition : ContentDefinition
    {
        [Min(1)] [SerializeField] private float baseHealth = 1000;
        [SerializeField] private float baseArmor = 0;
        [SerializeField] private float baseResistance = 0;
        [Min(0)] [SerializeField] private float regenerationDelaySeconds = 5;
        [SerializeField] private List<AbilityDefinition> ruleModules = new();

        public float BaseHealth => baseHealth;
        public float BaseArmor => baseArmor;
        public float BaseResistance => baseResistance;
        public float RegenerationDelaySeconds => regenerationDelaySeconds;
        public IReadOnlyList<AbilityDefinition> RuleModules => ruleModules.AsReadOnly();
    }
}
