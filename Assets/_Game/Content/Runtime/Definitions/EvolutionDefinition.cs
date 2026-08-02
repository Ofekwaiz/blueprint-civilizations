using System.Collections.Generic;
using BlueprintCivilizations.Core;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Evolution", fileName = "Evolution_")]
    public sealed class EvolutionDefinition : ContentDefinition
    {
        [SerializeField] private AscensionLevel requiredAscension = AscensionLevel.AscensionOne;
        [SerializeField] private string sourceBlueprintId = "";
        [SerializeField] private List<string> grantedTags = new();
        [SerializeField] private List<ModifierSpec> modifiers = new();
        [SerializeField] private List<AbilityDefinition> grantedAbilities = new();
        [SerializeField] private List<EvolutionDefinition> finalEvolutionOptions = new();

        public AscensionLevel RequiredAscension => requiredAscension;
        public string SourceBlueprintId => sourceBlueprintId;
        public IReadOnlyList<string> GrantedTags => grantedTags.AsReadOnly();
        public IReadOnlyList<ModifierSpec> Modifiers => modifiers.AsReadOnly();
        public IReadOnlyList<AbilityDefinition> GrantedAbilities => grantedAbilities.AsReadOnly();
        public IReadOnlyList<EvolutionDefinition> FinalEvolutionOptions => finalEvolutionOptions.AsReadOnly();
    }
}
