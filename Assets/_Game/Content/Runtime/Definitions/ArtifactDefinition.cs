using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Artifact", fileName = "Artifact_")]
    public sealed class ArtifactDefinition : ContentDefinition
    {
        [SerializeField] private RaceDefinition affinityRace;
        [SerializeField] private ContentRarity rarity = ContentRarity.Rare;
        [SerializeField] private ContentCompatibility compatibility = new();
        [SerializeField] private List<ModifierSpec> modifiers = new();
        [SerializeField] private List<TriggerSpec> triggers = new();
        [SerializeField] private bool unique = true;
        [SerializeField] private bool stackable;
        [Min(0)] [SerializeField] private float shopWeight = 1f;
        [SerializeField] private List<ArtifactDefinition> incompatibilities = new();

        public RaceDefinition AffinityRace => affinityRace;
        public ContentRarity Rarity => rarity;
        public ContentCompatibility Compatibility => compatibility;
        public IReadOnlyList<ModifierSpec> Modifiers => modifiers.AsReadOnly();
        public IReadOnlyList<TriggerSpec> Triggers => triggers.AsReadOnly();
        public bool Unique => unique;
        public bool Stackable => stackable;
        public float ShopWeight => shopWeight;
        public IReadOnlyList<ArtifactDefinition> Incompatibilities => incompatibilities.AsReadOnly();
    }
}
