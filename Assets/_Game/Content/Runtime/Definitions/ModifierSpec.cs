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
        [SerializeField] private ModifierOperation operation = ModifierOperation.FlatAdd;
        [SerializeField] private float value = 0;
        [SerializeField] private List<string> conditionTags = new();
        [SerializeField] private int sourcePriority = 0;
        [SerializeField] private ModifierDurationScope durationScope = ModifierDurationScope.PlanningSnapshot;
        [Min(0)] [SerializeField] private float durationSeconds = 0;

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
