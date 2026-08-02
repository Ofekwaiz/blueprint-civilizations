using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Research", fileName = "Research_")]
    public sealed class ResearchDefinition : ContentDefinition
    {
        [SerializeField] private RaceDefinition affinityRace;
        [SerializeField] private ContentRarity rarity = ContentRarity.Common;
        [SerializeField] private ContentCompatibility compatibility = new();
        [SerializeField] private List<ModifierSpec> modifiers = new();
        [SerializeField] private List<TriggerSpec> triggers = new();
        [SerializeField] private EffectStackRule stackRule = EffectStackRule.Unique;
        [SerializeField] private bool duplicatesAllowed;
        [SerializeField] private bool canBeMoved = true;
        [Min(0)] [SerializeField] private int reassignmentCost;
        [Min(0)] [SerializeField] private float shopWeight = 1f;

        public RaceDefinition AffinityRace => affinityRace;
        public ContentRarity Rarity => rarity;
        public ContentCompatibility Compatibility => compatibility;
        public IReadOnlyList<ModifierSpec> Modifiers => modifiers.AsReadOnly();
        public IReadOnlyList<TriggerSpec> Triggers => triggers.AsReadOnly();
        public EffectStackRule StackRule => stackRule;
        public bool DuplicatesAllowed => duplicatesAllowed;
        public bool CanBeMoved => canBeMoved;
        public int ReassignmentCost => reassignmentCost;
        public float ShopWeight => shopWeight;
    }
}
