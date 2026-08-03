using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace BlueprintCivilizations.Blueprints
{
    [Serializable]
    public sealed class BlueprintSlotState
    {
        [SerializeField] private int boardIndex;
        [SerializeField] private string blueprintDefinitionId = "";

        public BlueprintSlotState() { }
        internal BlueprintSlotState(int boardIndex) => this.boardIndex = boardIndex;

        public int BoardIndex => boardIndex;
        public string BlueprintDefinitionId => blueprintDefinitionId;
        public bool IsEmpty => string.IsNullOrWhiteSpace(blueprintDefinitionId);

        internal void SetIndex(int value) => boardIndex = value;
        internal void SetBlueprint(string definitionId) => blueprintDefinitionId = definitionId ?? "";
    }

    [Serializable]
    public sealed class BlueprintBenchState
    {
        [SerializeField] private List<string> blueprintDefinitionIds = new();

        public IReadOnlyList<string> BlueprintDefinitionIds => blueprintDefinitionIds?.AsReadOnly();
        public int Count => blueprintDefinitionIds?.Count ?? 0;

        internal List<string> MutableIds => blueprintDefinitionIds;
        internal void EnsureInitialized() => blueprintDefinitionIds ??= new List<string>();
    }

    /// <summary>Serializable aggregate root for one player's ordered active line and bench.</summary>
    [Serializable]
    public sealed class BlueprintBoardState
    {
        public const int CurrentSaveVersion = 1;

        [SerializeField] private int saveVersion = CurrentSaveVersion;
        [SerializeField] private string ownerId = "";
        [Min(0)] [SerializeField] private int capacity;
        [SerializeField] private List<BlueprintSlotState> slots = new();
        [SerializeField] private BlueprintBenchState bench = new();
        [SerializeField] private List<BlueprintState> blueprints = new();
        [SerializeField] private int revision;

        public BlueprintBoardState() { }

        public BlueprintBoardState(string ownerId, int capacity, IEnumerable<BlueprintState> ownedBlueprints = null)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID is required.", nameof(ownerId));
            if (capacity < 0) throw new ArgumentOutOfRangeException(nameof(capacity), "Blueprint Capacity cannot be negative.");

            this.ownerId = ownerId.Trim();
            this.capacity = capacity;
            for (int index = 0; index < capacity; index++) slots.Add(new BlueprintSlotState(index));

            foreach (var blueprint in ownedBlueprints ?? Enumerable.Empty<BlueprintState>())
            {
                if (blueprint == null) throw new ArgumentException("Owned blueprints cannot contain null.", nameof(ownedBlueprints));
                if (!string.Equals(blueprint.OwnerId, this.ownerId, StringComparison.Ordinal))
                    throw new ArgumentException($"Blueprint '{blueprint.DefinitionId}' belongs to another player.", nameof(ownedBlueprints));
                if (blueprints.Any(value => string.Equals(value.DefinitionId, blueprint.DefinitionId, StringComparison.OrdinalIgnoreCase)))
                    throw new ArgumentException($"Blueprint '{blueprint.DefinitionId}' is registered more than once.", nameof(ownedBlueprints));
                blueprint.SetPlacement(BlueprintLocationState.Benched, -1);
                blueprints.Add(blueprint);
                bench.MutableIds.Add(blueprint.DefinitionId);
            }
        }

        public int SaveVersion => saveVersion;
        public string OwnerId => ownerId;
        public int Capacity => capacity;
        public IReadOnlyList<BlueprintSlotState> Slots => slots?.AsReadOnly();
        public BlueprintBenchState Bench => bench;
        public IReadOnlyList<BlueprintState> Blueprints => blueprints?.AsReadOnly();
        public int Revision => revision;
        public int ActiveCount => slots?.Count(slot => slot != null && !slot.IsEmpty) ?? 0;

        public BlueprintState FindBlueprint(string definitionId) => blueprints?.FirstOrDefault(value =>
            value != null && string.Equals(value.DefinitionId, definitionId, StringComparison.OrdinalIgnoreCase));

        public int FindActiveIndex(string definitionId)
        {
            if (slots == null) return -1;
            for (int index = 0; index < slots.Count; index++)
            {
                if (slots[index] != null && string.Equals(slots[index].BlueprintDefinitionId, definitionId, StringComparison.OrdinalIgnoreCase)) return index;
            }
            return -1;
        }

        internal List<BlueprintSlotState> MutableSlots => slots;
        internal List<BlueprintState> MutableBlueprints => blueprints;
        internal void SetCapacity(int value) => capacity = value;
        internal void IncrementRevision() => revision++;

        internal void RestoreFrom(BlueprintBoardState source, int restoredRevision)
        {
            saveVersion = source.saveVersion;
            ownerId = source.ownerId;
            capacity = source.capacity;
            slots = source.slots;
            bench = source.bench;
            blueprints = source.blueprints;
            revision = restoredRevision;
            NormalizeAfterDeserialization();
        }

        internal void NormalizeAfterDeserialization()
        {
            slots ??= new List<BlueprintSlotState>();
            blueprints ??= new List<BlueprintState>();
            bench ??= new BlueprintBenchState();
            bench.EnsureInitialized();
        }
    }
}
