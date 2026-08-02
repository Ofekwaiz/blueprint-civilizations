using System.Collections.Generic;
using BlueprintCivilizations.Core;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Structure", fileName = "Structure_")]
    public sealed class StructureDefinition : ContentDefinition
    {
        [SerializeField] private RaceDefinition race = null;
        [SerializeField] private bool isNeutral = false;
        [SerializeField] private ContentTier tier = ContentTier.Tier1;
        [Min(0)] [SerializeField] private int goldCost = 2;
        [SerializeField] private ContentPoolKind poolKind = ContentPoolKind.PrivateRace;
        [Min(1)] [SerializeField] private int shopPoolSize = 18;
        [Min(0)] [SerializeField] private float baseShopWeight = 1f;
        [SerializeField] private bool spawnsOnBattlefield = true;
        [SerializeField] private LaneCompatibility laneCompatibility = LaneCompatibility.Any;
        [Min(1)] [SerializeField] private int maximumPopulation = 1;
        [Min(0)] [SerializeField] private float reconstructionInterval = 0;
        [Min(1)] [SerializeField] private float baseHealth = 100;
        [SerializeField] private float armor = 0;
        [SerializeField] private float resistance = 0;
        [SerializeField] private List<ModifierSpec> adjacencyModifiers = new();
        [SerializeField] private List<AbilityDefinition> abilities = new();
        [SerializeField] private List<EvolutionDefinition> evolutionOptions = new();
        [SerializeField] private ContentCompatibility researchCompatibility = new();
        [TextArea] [SerializeField] private string rulesSummary = "";

        public RaceDefinition Race => race;
        public bool IsNeutral => isNeutral;
        public ContentTier Tier => tier;
        public int GoldCost => goldCost;
        public ContentPoolKind PoolKind => poolKind;
        public int ShopPoolSize => shopPoolSize;
        public float BaseShopWeight => baseShopWeight;
        public bool SpawnsOnBattlefield => spawnsOnBattlefield;
        public LaneCompatibility LaneCompatibility => laneCompatibility;
        public int MaximumPopulation => maximumPopulation;
        public float ReconstructionInterval => reconstructionInterval;
        public float BaseHealth => baseHealth;
        public float Armor => armor;
        public float Resistance => resistance;
        public IReadOnlyList<ModifierSpec> AdjacencyModifiers => adjacencyModifiers.AsReadOnly();
        public IReadOnlyList<AbilityDefinition> Abilities => abilities.AsReadOnly();
        public IReadOnlyList<EvolutionDefinition> EvolutionOptions => evolutionOptions.AsReadOnly();
        public ContentCompatibility ResearchCompatibility => researchCompatibility;
        public string RulesSummary => rulesSummary;
    }
}
