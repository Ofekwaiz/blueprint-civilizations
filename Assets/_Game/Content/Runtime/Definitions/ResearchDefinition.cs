using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Research", fileName = "Research_")]
    public sealed class ResearchDefinition : ContentDefinition
    {
        [Min(0)] [SerializeField] private int raceResourceCost = 1;
        [TextArea] [SerializeField] private string effectRules = "";
        [SerializeField] private bool uniquePerBlueprint = true;
        public int RaceResourceCost => raceResourceCost;
        public string EffectRules => effectRules;
        public bool UniquePerBlueprint => uniquePerBlueprint;
    }
}
