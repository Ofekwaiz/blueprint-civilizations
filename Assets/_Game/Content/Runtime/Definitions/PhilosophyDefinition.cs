using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    /// <summary>Authored in-run philosophy and its signal vocabulary.</summary>
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Definitions/Philosophy", fileName = "Philosophy_")]
    public sealed class PhilosophyDefinition : ContentDefinition
    {
        [SerializeField] private List<string> signalTags = new();
        [SerializeField] private List<AugmentDefinition> augments = new();

        public IReadOnlyList<string> SignalTags => signalTags.AsReadOnly();
        public IReadOnlyList<AugmentDefinition> Augments => augments.AsReadOnly();
    }
}
