using BlueprintCivilizations.Core;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Evolution", fileName = "Evolution_")]
    public sealed class EvolutionDefinition : ContentDefinition
    {
        [SerializeField] private AscensionLevel requiredAscension = AscensionLevel.AscensionOne;
        [SerializeField] private float healthMultiplier = 1f;
        [SerializeField] private float damageMultiplier = 1f;
        [SerializeField] private float spawnIntervalMultiplier = 1f;
        [TextArea] [SerializeField] private string rulesText = "";
        public AscensionLevel RequiredAscension => requiredAscension;
        public float HealthMultiplier => healthMultiplier;
        public float DamageMultiplier => damageMultiplier;
        public float SpawnIntervalMultiplier => spawnIntervalMultiplier;
        public string RulesText => rulesText;
    }
}
