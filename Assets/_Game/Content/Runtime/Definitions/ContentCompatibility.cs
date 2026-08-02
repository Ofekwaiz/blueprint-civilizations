using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    /// <summary>Reusable race and tag compatibility rules for socketable or global content.</summary>
    [Serializable]
    public sealed class ContentCompatibility
    {
        [SerializeField] private List<RaceDefinition> allowedRaces = new();
        [SerializeField] private List<string> requiredTags = new();
        [SerializeField] private List<string> excludedTags = new();
        [SerializeField] private bool allowNeutral = true;

        public IReadOnlyList<RaceDefinition> AllowedRaces => allowedRaces.AsReadOnly();
        public IReadOnlyList<string> RequiredTags => requiredTags.AsReadOnly();
        public IReadOnlyList<string> ExcludedTags => excludedTags.AsReadOnly();
        public bool AllowNeutral => allowNeutral;
    }
}
