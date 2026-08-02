using System;
using System.Collections.Generic;

namespace BlueprintCivilizations.Core
{
    /// <summary>
    /// Player-owned mutable state for one unit blueprint. It stores stable IDs and selections only;
    /// authored base statistics remain exclusively on the corresponding content definition.
    /// </summary>
    [Serializable]
    public sealed class UnitBlueprintState
    {
        private readonly List<string> chosenEvolutionIds = new();
        private readonly List<string> selectedPerCopyStatUpgradeIds = new();
        private readonly List<string> attachedResearchIds = new();

        public UnitBlueprintState(string definitionId, string ownerId)
        {
            if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("Definition ID is required.", nameof(definitionId));
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID is required.", nameof(ownerId));

            DefinitionId = definitionId;
            OwnerId = ownerId;
        }

        /// <summary>Immutable ID of the authored unit definition.</summary>
        public string DefinitionId { get; }

        /// <summary>Stable ID of the owning player.</summary>
        public string OwnerId { get; }

        /// <summary>Number of purchased copies represented by this blueprint.</summary>
        public int CopiesPurchased { get; set; } = 1;

        /// <summary>Current authored progression milestone.</summary>
        public AscensionLevel AscensionLevel { get; set; }

        /// <summary>Stable IDs for the selected evolution path and final form, when present.</summary>
        public IList<string> ChosenEvolutionIds => chosenEvolutionIds;

        /// <summary>Stable IDs for per-copy refinement choices.</summary>
        public IList<string> SelectedPerCopyStatUpgradeIds => selectedPerCopyStatUpgradeIds;

        /// <summary>Stable IDs for research currently attached to this blueprint.</summary>
        public IList<string> AttachedResearchIds => attachedResearchIds;

        /// <summary>Whether the blueprint is active or on the bench.</summary>
        public BlueprintLocationState Location { get; set; } = BlueprintLocationState.Benched;

        /// <summary>Zero-based active-board position, or -1 while benched.</summary>
        public int BlueprintBoardIndex { get; set; } = -1;

        /// <summary>Lane selected for produced entities.</summary>
        public BlueprintLane AssignedLane { get; set; } = BlueprintLane.Unassigned;

        /// <summary>AI stance selected during planning.</summary>
        public BlueprintStance AssignedStance { get; set; } = BlueprintStance.Unassigned;
    }
}
