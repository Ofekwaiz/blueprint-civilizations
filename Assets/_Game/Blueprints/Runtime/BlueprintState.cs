using System;
using System.Collections.Generic;
using BlueprintCivilizations.Core;
using UnityEngine;

namespace BlueprintCivilizations.Blueprints
{
    public enum BlueprintLocationState { Active, Benched }
    public enum BlueprintLane { Unassigned, Left, Right, Split }
    public enum BlueprintStance { Unassigned, Assault, Defense }

    /// <summary>
    /// Player-owned mutable state for one blueprint. It contains stable IDs and player choices only;
    /// authored definitions remain immutable ScriptableObjects outside this state graph.
    /// </summary>
    [Serializable]
    public class BlueprintState
    {
        [SerializeField] private string definitionId = "";
        [SerializeField] private string ownerId = "";
        [Min(1)] [SerializeField] private int copiesPurchased = 1;
        [SerializeField] private AscensionLevel ascensionLevel;
        [SerializeField] private List<string> chosenEvolutionIds = new();
        [SerializeField] private List<string> selectedPerCopyStatUpgradeIds = new();
        [SerializeField] private List<string> attachedResearchIds = new();
        [SerializeField] private BlueprintLocationState location = BlueprintLocationState.Benched;
        [SerializeField] private int blueprintBoardIndex = -1;
        [SerializeField] private BlueprintLane assignedLane = BlueprintLane.Unassigned;
        [SerializeField] private BlueprintStance assignedStance = BlueprintStance.Unassigned;

        public BlueprintState() { }

        public BlueprintState(string definitionId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("Definition ID is required.", nameof(definitionId));
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID is required.", nameof(ownerId));
            this.definitionId = definitionId.Trim();
            this.ownerId = ownerId.Trim();
        }

        public string DefinitionId => definitionId;
        public string OwnerId => ownerId;
        public int CopiesPurchased { get => copiesPurchased; set => copiesPurchased = value; }
        public AscensionLevel AscensionLevel { get => ascensionLevel; set => ascensionLevel = value; }
        public IList<string> ChosenEvolutionIds => chosenEvolutionIds;
        public IList<string> SelectedPerCopyStatUpgradeIds => selectedPerCopyStatUpgradeIds;
        public IList<string> AttachedResearchIds => attachedResearchIds;
        public BlueprintLocationState Location => location;
        public int BlueprintBoardIndex => blueprintBoardIndex;
        public BlueprintLane AssignedLane { get => assignedLane; set => assignedLane = value; }
        public BlueprintStance AssignedStance { get => assignedStance; set => assignedStance = value; }

        internal void SetPlacement(BlueprintLocationState newLocation, int boardIndex)
        {
            location = newLocation;
            blueprintBoardIndex = newLocation == BlueprintLocationState.Active ? boardIndex : -1;
        }
    }

    /// <summary>Semantic state type retained for unit blueprints; board placement is shared with structures.</summary>
    [Serializable]
    public sealed class UnitBlueprintState : BlueprintState
    {
        public UnitBlueprintState() { }
        public UnitBlueprintState(string definitionId, string ownerId) : base(definitionId, ownerId) { }
    }
}
