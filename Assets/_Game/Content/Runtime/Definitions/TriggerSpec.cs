using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.Content.Definitions
{
    /// <summary>Serializable trigger definition evaluated by future gameplay services.</summary>
    [Serializable]
    public sealed class TriggerSpec
    {
        [SerializeField] private TriggerEventType eventType;
        [SerializeField] private List<string> requiredTags = new();
        [Range(0, 1)] [SerializeField] private float probability = 1f;
        [Min(1)] [SerializeField] private int everyNthEvent = 1;
        [Min(0)] [SerializeField] private float cooldownSeconds;
        [Min(0)] [SerializeField] private int maximumTriggers;
        [SerializeField] private List<ModifierSpec> modifiers = new();

        public TriggerEventType EventType => eventType;
        public IReadOnlyList<string> RequiredTags => requiredTags.AsReadOnly();
        public float Probability => probability;
        public int EveryNthEvent => everyNthEvent;
        public float CooldownSeconds => cooldownSeconds;
        public int MaximumTriggers => maximumTriggers;
        public IReadOnlyList<ModifierSpec> Modifiers => modifiers.AsReadOnly();
    }
}
