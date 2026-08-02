using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    /// <summary>Reusable authored gameplay behavior composed from modifiers and triggers.</summary>
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Ability", fileName = "Ability_")]
    public sealed class AbilityDefinition : ContentDefinition
    {
        [SerializeField] private List<ModifierSpec> modifiers = new();
        [SerializeField] private List<TriggerSpec> triggers = new();
        [TextArea] [SerializeField] private string presentationSummary = "";

        public IReadOnlyList<ModifierSpec> Modifiers => modifiers.AsReadOnly();
        public IReadOnlyList<TriggerSpec> Triggers => triggers.AsReadOnly();
        public string PresentationSummary => presentationSummary;
    }
}
