using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    /// <summary>Authored milestone choice that applies run-level modifiers or triggers.</summary>
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Augment", fileName = "Augment_")]
    public sealed class AugmentDefinition : ContentDefinition
    {
        [SerializeField] private PhilosophyDefinition philosophy = null;
        [SerializeField] private ContentRarity rarity = ContentRarity.Common;
        [SerializeField] private List<ModifierSpec> modifiers = new();
        [SerializeField] private List<TriggerSpec> triggers = new();

        public PhilosophyDefinition Philosophy => philosophy;
        public ContentRarity Rarity => rarity;
        public IReadOnlyList<ModifierSpec> Modifiers => modifiers.AsReadOnly();
        public IReadOnlyList<TriggerSpec> Triggers => triggers.AsReadOnly();
    }
}
