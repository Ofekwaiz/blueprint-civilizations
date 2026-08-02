using System;
using System.Collections.Generic;
using BlueprintCivilizations.Core;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [Serializable]
    public sealed class UnitCombatStats
    {
        [Min(1)] [SerializeField] private float maxHealth = 30;
        [Min(0)] [SerializeField] private float attackDamage = 5;
        [Min(0.01f)] [SerializeField] private float attacksPerSecond = 1f;
        [Min(0)] [SerializeField] private float attackRange = 1f;
        [Min(0)] [SerializeField] private float movementSpeed = 2f;
        [SerializeField] private float armor;
        [SerializeField] private float resistance;

        public float MaxHealth => maxHealth;
        public float AttackDamage => attackDamage;
        public float AttacksPerSecond => attacksPerSecond;
        public float AttackIntervalSeconds => attacksPerSecond <= 0 ? float.PositiveInfinity : 1f / attacksPerSecond;
        public float AttackRange => attackRange;
        public float MovementSpeed => movementSpeed;
        public float Armor => armor;
        public float Resistance => resistance;
    }

    [Serializable]
    public sealed class UnitProductionStats
    {
        [Min(0.05f)] [SerializeField] private float spawnInterval = 6f;
        [Min(1)] [SerializeField] private int maximumPopulation = 3;
        [Min(0)] [SerializeField] private float initialSpawnDelay;
        [Min(1)] [SerializeField] private int spawnBatchSize = 1;
        [SerializeField] private int spawnPriority;

        public float SpawnInterval => spawnInterval;
        public int MaximumPopulation => maximumPopulation;
        public float InitialSpawnDelay => initialSpawnDelay;
        public int SpawnBatchSize => spawnBatchSize;
        public int SpawnPriority => spawnPriority;
    }

    [Serializable]
    public sealed class UnitTargetingProfile
    {
        [SerializeField] private TargetPriority priority = TargetPriority.Nearest;
        [SerializeField] private bool canTargetGround = true;
        [SerializeField] private bool canTargetFlying;

        public TargetPriority Priority => priority;
        public bool CanTargetGround => canTargetGround;
        public bool CanTargetFlying => canTargetFlying;
    }

    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Unit", fileName = "Unit_")]
    public sealed class UnitDefinition : ContentDefinition
    {
        [SerializeField] private RaceDefinition race;
        [SerializeField] private bool isNeutral;
        [SerializeField] private ContentTier tier = ContentTier.Tier1;
        [Min(0)] [SerializeField] private int goldCost = 1;
        [SerializeField] private ContentPoolKind poolKind = ContentPoolKind.PrivateRace;
        [Min(1)] [SerializeField] private int shopPoolSize = 18;
        [Min(0)] [SerializeField] private float baseShopWeight = 1f;
        [SerializeField] private string role = "";
        [SerializeField] private UnitCombatStats combatStats = new();
        [SerializeField] private UnitProductionStats productionStats = new();
        [SerializeField] private LaneCompatibility laneCompatibility = LaneCompatibility.Any;
        [SerializeField] private MovementProfileKind movementProfile = MovementProfileKind.Ground;
        [SerializeField] private UnitTargetingProfile targeting = new();
        [SerializeField] private List<AbilityDefinition> abilities = new();
        [Min(1)] [SerializeField] private int ascensionOneThreshold = 5;
        [Min(1)] [SerializeField] private int ascensionTwoThreshold = 10;
        [SerializeField] private List<EvolutionDefinition> ascensionOneOptions = new();
        [SerializeField] private List<EvolutionDefinition> ascensionTwoOptions = new();
        [SerializeField] private GameObject visualPrefab;

        public RaceDefinition Race => race;
        public bool IsNeutral => isNeutral;
        public ContentTier Tier => tier;
        public int GoldCost => goldCost;
        public ContentPoolKind PoolKind => poolKind;
        public int ShopPoolSize => shopPoolSize;
        public float BaseShopWeight => baseShopWeight;
        public string Role => role;
        public UnitCombatStats CombatStats => combatStats;
        public UnitProductionStats ProductionStats => productionStats;
        public LaneCompatibility LaneCompatibility => laneCompatibility;
        public MovementProfileKind MovementProfile => movementProfile;
        public UnitTargetingProfile Targeting => targeting;
        public IReadOnlyList<AbilityDefinition> Abilities => abilities.AsReadOnly();
        public int AscensionOneThreshold => ascensionOneThreshold;
        public int AscensionTwoThreshold => ascensionTwoThreshold;
        public IReadOnlyList<EvolutionDefinition> AscensionOneOptions => ascensionOneOptions.AsReadOnly();
        public IReadOnlyList<EvolutionDefinition> AscensionTwoOptions => ascensionTwoOptions.AsReadOnly();
        public GameObject VisualPrefab => visualPrefab;
    }
}
