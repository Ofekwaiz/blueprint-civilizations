using System;
using System.Collections.Generic;

namespace BlueprintCivilizations.Core
{
    [Serializable]
    public sealed class UnitBlueprintState
    {
        public string OwnerId { get; private set; }
        public string DefinitionId { get; private set; }
        public int CopiesPurchased { get; private set; }
        public AscensionLevel Ascension { get; private set; }
        public IReadOnlyList<string> ChosenEvolutionIds => _chosenEvolutionIds;
        public IReadOnlyList<StatUpgradeType> SelectedStatUpgrades => _selectedStatUpgrades;
        public IReadOnlyList<string> AttachedResearchIds => _attachedResearchIds;
        public BlueprintLocation Location { get; private set; }
        public int BoardIndex { get; private set; }
        public UnitLane AssignedLane { get; private set; }
        public UnitStance AssignedStance { get; private set; }

        private readonly List<string> _chosenEvolutionIds = new();
        private readonly List<StatUpgradeType> _selectedStatUpgrades = new();
        private readonly List<string> _attachedResearchIds = new();

        public UnitBlueprintState(string ownerId, string definitionId)
        {
            if (string.IsNullOrWhiteSpace(ownerId)) throw new ArgumentException("Owner ID is required.", nameof(ownerId));
            if (string.IsNullOrWhiteSpace(definitionId)) throw new ArgumentException("Definition ID is required.", nameof(definitionId));
            OwnerId = ownerId;
            DefinitionId = definitionId;
            CopiesPurchased = 1;
            Location = BlueprintLocation.Bench;
            BoardIndex = -1;
            AssignedLane = UnitLane.Left;
            AssignedStance = UnitStance.Assault;
        }

        public void PurchaseCopy(StatUpgradeType selectedUpgrade)
        {
            CopiesPurchased++;
            _selectedStatUpgrades.Add(selectedUpgrade);
            Ascension = CopiesPurchased >= 10 ? AscensionLevel.AscensionTwo :
                        CopiesPurchased >= 5 ? AscensionLevel.AscensionOne : AscensionLevel.Base;
        }

        public void ChooseEvolution(string evolutionId)
        {
            if (string.IsNullOrWhiteSpace(evolutionId)) throw new ArgumentException("Evolution ID is required.", nameof(evolutionId));
            if (!_chosenEvolutionIds.Contains(evolutionId)) _chosenEvolutionIds.Add(evolutionId);
        }

        public void AttachResearch(string researchId)
        {
            if (string.IsNullOrWhiteSpace(researchId)) throw new ArgumentException("Research ID is required.", nameof(researchId));
            if (!_attachedResearchIds.Contains(researchId)) _attachedResearchIds.Add(researchId);
        }

        public void DetachResearch(string researchId) => _attachedResearchIds.Remove(researchId);

        public void SetPlacement(BlueprintLocation location, int boardIndex)
        {
            Location = location;
            BoardIndex = location == BlueprintLocation.Active ? boardIndex : -1;
        }

        public void SetOrders(UnitLane lane, UnitStance stance)
        {
            AssignedLane = lane;
            AssignedStance = stance;
        }
    }
}
