using BlueprintCivilizations.Core;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Structure", fileName = "Structure_")]
    public sealed class StructureDefinition : ContentDefinition
    {
        [SerializeField] private RaceDefinition race;
        [SerializeField] private ContentTier tier = ContentTier.Tier1;
        [Min(0)] [SerializeField] private int goldCost = 2;
        [Min(1)] [SerializeField] private float baseHealth = 100;
        [TextArea] [SerializeField] private string adjacencyRules = "";
        [TextArea] [SerializeField] private string battlefieldRules = "";
        public RaceDefinition Race => race;
        public ContentTier Tier => tier;
        public int GoldCost => goldCost;
        public float BaseHealth => baseHealth;
        public string AdjacencyRules => adjacencyRules;
        public string BattlefieldRules => battlefieldRules;
    }
}
