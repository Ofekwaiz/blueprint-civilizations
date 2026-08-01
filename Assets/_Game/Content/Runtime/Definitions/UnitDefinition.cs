using System;
using System.Collections.Generic;
using BlueprintCivilizations.Core;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [Serializable]
    public sealed class UnitCombatStats
    {
        [Min(1)] public float maxHealth = 30;
        [Min(0)] public float attackDamage = 5;
        [Min(0.05f)] public float attackInterval = 1f;
        [Min(0)] public float attackRange = 1f;
        [Min(0)] public float movementSpeed = 2f;
    }

    [Serializable]
    public sealed class UnitProductionStats
    {
        [Min(0.05f)] public float spawnInterval = 6f;
        [Min(1)] public int maximumPopulation = 3;
    }

    [Serializable]
    public sealed class UnitTargetingProfile
    {
        public TargetPriority priority = TargetPriority.UnitsFirst;
        public bool canTargetGround = true;
        public bool canTargetFlying = false;
    }

    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Unit", fileName = "Unit_")]
    public sealed class UnitDefinition : ContentDefinition
    {
        [SerializeField] private RaceDefinition race;
        [SerializeField] private ContentTier tier = ContentTier.Tier1;
        [Min(0)] [SerializeField] private int goldCost = 1;
        [Min(1)] [SerializeField] private int shopPoolSize = 20;
        [SerializeField] private UnitCombatStats combatStats = new();
        [SerializeField] private UnitProductionStats productionStats = new();
        [SerializeField] private LaneCompatibility laneCompatibility = LaneCompatibility.Any;
        [SerializeField] private UnitTargetingProfile targeting = new();
        [TextArea] [SerializeField] private string abilityRules = "";
        [SerializeField] private List<EvolutionDefinition> evolutionOptions = new();
        [SerializeField] private GameObject visualPrefab;

        public RaceDefinition Race => race;
        public ContentTier Tier => tier;
        public int GoldCost => goldCost;
        public int ShopPoolSize => shopPoolSize;
        public UnitCombatStats CombatStats => combatStats;
        public UnitProductionStats ProductionStats => productionStats;
        public LaneCompatibility LaneCompatibility => laneCompatibility;
        public UnitTargetingProfile Targeting => targeting;
        public string AbilityRules => abilityRules;
        public IReadOnlyList<EvolutionDefinition> EvolutionOptions => evolutionOptions;
        public GameObject VisualPrefab => visualPrefab;
    }
}
