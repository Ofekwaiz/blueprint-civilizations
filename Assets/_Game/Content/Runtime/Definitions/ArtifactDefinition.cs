using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Artifact", fileName = "Artifact_")]
    public sealed class ArtifactDefinition : ContentDefinition
    {
        [Min(0)] [SerializeField] private int raceResourceCost = 3;
        [TextArea] [SerializeField] private string globalEffectRules = "";
        public int RaceResourceCost => raceResourceCost;
        public string GlobalEffectRules => globalEffectRules;
    }
}
