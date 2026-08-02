using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    /// <summary>Serializable, inspectable description of a deterministic statistic modifier.</summary>
    [Serializable]
    public sealed class ModifierSpec
    {
        [SerializeField] private string targetSelector = "self";
        [SerializeField] private string stat = "";
        [SerializeField] private ModifierOperation operation;
        [SerializeField] private float value;
        [SerializeField] private List<string> conditionTags = new();
        [SerializeField] private int sourcePriority;
        [SerializeField] private ModifierDurationScope durationScope = ModifierDurationScope.PlanningSnapshot;
        [Min(0)] [SerializeField] private float durationSeconds;

        public string TargetSelector => targetSelector;
        public string Stat => stat;
        public ModifierOperation Operation => operation;
        public float Value => value;
        public IReadOnlyList<string> ConditionTags => conditionTags.AsReadOnly();
        public int SourcePriority => sourcePriority;
        public ModifierDurationScope DurationScope => durationScope;
        public float DurationSeconds => durationSeconds;
    }
}
