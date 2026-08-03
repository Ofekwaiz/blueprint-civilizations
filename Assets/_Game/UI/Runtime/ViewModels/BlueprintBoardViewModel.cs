using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.UI.ViewModels
{
    public sealed class BlueprintCardViewModel
    {
        public BlueprintCardViewModel(string definitionId, string displayName, string tooltip, Sprite icon)
        {
            DefinitionId = definitionId ?? "";
            DisplayName = string.IsNullOrWhiteSpace(displayName) ? DefinitionId : displayName;
            Tooltip = tooltip ?? "";
            Icon = icon;
        }

        public string DefinitionId { get; }
        public string DisplayName { get; }
        public string Tooltip { get; }
        public Sprite Icon { get; }
    }

    public sealed class BlueprintSlotViewModel
    {
        public BlueprintSlotViewModel(int index, BlueprintCardViewModel blueprint) { Index = index; Blueprint = blueprint; }
        public int Index { get; }
        public BlueprintCardViewModel Blueprint { get; }
    }

    public sealed class BlueprintBoardViewModel
    {
        public BlueprintBoardViewModel(int capacity, int activeCount, IReadOnlyList<BlueprintSlotViewModel> slots,
            IReadOnlyList<BlueprintCardViewModel> bench, bool canUndo, bool canRedo,
            string selectedBlueprintId = "", IReadOnlyList<string> adjacentBlueprintIds = null)
        {
            Capacity = capacity;
            ActiveCount = activeCount;
            Slots = slots;
            Bench = bench;
            CanUndo = canUndo;
            CanRedo = canRedo;
            SelectedBlueprintId = selectedBlueprintId ?? "";
            AdjacentBlueprintIds = adjacentBlueprintIds ?? System.Array.Empty<string>();
        }

        public int Capacity { get; }
        public int ActiveCount { get; }
        public IReadOnlyList<BlueprintSlotViewModel> Slots { get; }
        public IReadOnlyList<BlueprintCardViewModel> Bench { get; }
        public bool CanUndo { get; }
        public bool CanRedo { get; }
        public string SelectedBlueprintId { get; }
        public IReadOnlyList<string> AdjacentBlueprintIds { get; }
    }
}
