using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Blueprints;
using BlueprintCivilizations.UI.ViewModels;
using BlueprintCivilizations.UI.Views;

namespace BlueprintCivilizations.UI.Presenters
{
    public sealed class BlueprintBoardPresenter : IDisposable
    {
        private readonly IBlueprintBoardView view;
        private readonly BlueprintPlacementService placement;
        private readonly BlueprintAdjacencyService adjacency;
        private readonly IBlueprintBoardPresentationResolver presentation;
        private string selectedBlueprintId = "";
        private bool disposed;

        public BlueprintBoardPresenter(IBlueprintBoardView view, BlueprintPlacementService placement,
            BlueprintAdjacencyService adjacency, IBlueprintBoardPresentationResolver presentation)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.placement = placement ?? throw new ArgumentNullException(nameof(placement));
            this.adjacency = adjacency ?? throw new ArgumentNullException(nameof(adjacency));
            this.presentation = presentation ?? throw new ArgumentNullException(nameof(presentation));

            view.SelectionRequested += Select;
            view.BenchRequested += Bench;
            view.ReorderRequested += Reorder;
            view.DropPreviewRequested += PreviewDrop;
            view.DropRequested += Drop;
            view.UndoRequested += Undo;
            view.RedoRequested += Redo;
            placement.EventRaised += OnPlacementEvent;
            Refresh();
        }

        public BlueprintPlacementService Placement => placement;
        public string SelectedBlueprintId => selectedBlueprintId;
        /// <summary>Raised only when the presenter-owned stable selection changes.</summary>
        public event Action<string> SelectionChanged;

        public void Refresh()
        {
            bool selectionCleared = false;
            if (!string.IsNullOrWhiteSpace(selectedBlueprintId) && placement.State.FindBlueprint(selectedBlueprintId) == null)
            {
                selectedBlueprintId = "";
                selectionCleared = true;
            }

            var state = placement.State;
            var slots = state.Slots.Select(slot => new BlueprintSlotViewModel(slot.BoardIndex,
                slot.IsEmpty ? null : ResolveCard(slot.BlueprintDefinitionId))).ToArray();
            var bench = state.Bench.BlueprintDefinitionIds.Select(ResolveCard).ToArray();
            var adjacentIds = GetSelectedAdjacentIds();
            view.Render(new BlueprintBoardViewModel(state.Capacity, state.ActiveCount, slots, bench,
                placement.CanUndo, placement.CanRedo, selectedBlueprintId, adjacentIds));
            if (selectionCleared) SelectionChanged?.Invoke(selectedBlueprintId);
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            placement.EventRaised -= OnPlacementEvent;
            view.SelectionRequested -= Select;
            view.BenchRequested -= Bench;
            view.ReorderRequested -= Reorder;
            view.DropPreviewRequested -= PreviewDrop;
            view.DropRequested -= Drop;
            view.UndoRequested -= Undo;
            view.RedoRequested -= Redo;
            view.Dispose();
        }

        private BlueprintCardViewModel ResolveCard(string id)
        {
            presentation.TryResolve(id, out var card);
            return card ?? new BlueprintCardViewModel(id, id, $"Broken content reference: {id}", null);
        }

        private void Select(string id)
        {
            string requested = id ?? "";
            string next = string.IsNullOrWhiteSpace(requested) || placement.State.FindBlueprint(requested) == null
                ? ""
                : requested;
            bool changed = !string.Equals(selectedBlueprintId, next, StringComparison.OrdinalIgnoreCase);
            selectedBlueprintId = next;
            view.SetSelectionState(selectedBlueprintId, GetSelectedAdjacentIds());
            if (changed) SelectionChanged?.Invoke(selectedBlueprintId);
        }

        private void Bench(string id) => Execute(BlueprintCommands.BenchBlueprint(id));
        private void Reorder(string id, int index) => Execute(BlueprintCommands.ReorderBlueprints(id, index));
        private void Undo() => ShowResult(placement.Undo(), "Undo");
        private void Redo() => ShowResult(placement.Redo(), "Redo");

        private void PreviewDrop(BlueprintBoardDropRequest request)
        {
            if (!TryCreateDropCommand(request, out var command, out string failure))
            {
                view.ShowDropPreview(request, false, failure);
                return;
            }

            BlueprintCommandResult result = placement.Preview(command);
            view.ShowDropPreview(request, result.Success, result.Success ? DropDescription(command) : result.Message);
        }

        private void Drop(BlueprintBoardDropRequest request)
        {
            if (!TryCreateDropCommand(request, out var command, out string failure))
            {
                view.ShowStatus(failure, true);
                view.LogInteractionDiagnostic($"Drop rejected before dispatch: {failure}");
                return;
            }
            Execute(command);
        }

        private void Execute(BlueprintCommand command)
        {
            view.LogInteractionDiagnostic($"Dispatching {command.GetType().Name}.");
            ShowResult(placement.Execute(command), command.GetType().Name);
        }

        private void ShowResult(BlueprintCommandResult result, string operation)
        {
            view.LogInteractionDiagnostic($"{operation} result: success={result.Success}, failure={result.Failure}, message='{result.Message}'.");
            view.ShowStatus(result.Success ? "Blueprint Board updated." : result.Message, !result.Success);
        }

        private void OnPlacementEvent(BlueprintEvent _) => Refresh();

        private string[] GetSelectedAdjacentIds()
        {
            if (string.IsNullOrWhiteSpace(selectedBlueprintId)) return Array.Empty<string>();
            var pair = adjacency.GetAdjacentPair(placement.State, selectedBlueprintId);
            var ids = new List<string>(2);
            if (pair.Left != null) ids.Add(pair.Left.DefinitionId);
            if (pair.Right != null) ids.Add(pair.Right.DefinitionId);
            return ids.ToArray();
        }

        private bool TryCreateDropCommand(BlueprintBoardDropRequest request, out BlueprintCommand command, out string failure)
        {
            command = null;
            failure = "The drop target is not supported.";
            if (request?.Source == null || string.IsNullOrWhiteSpace(request.Source.BlueprintId))
            {
                failure = "The drag source is missing.";
                return false;
            }

            string id = request.Source.BlueprintId;
            if (request.Source.Origin == BlueprintBoardDragOrigin.Bench)
            {
                if (request.TargetKind is BlueprintBoardDropTargetKind.ActiveSlot or BlueprintBoardDropTargetKind.Insertion)
                {
                    command = BlueprintCommands.ActivateBlueprint(id, request.TargetIndex);
                    return true;
                }
                failure = "A benched Blueprint must be dropped on the Active line.";
                return false;
            }

            if (request.TargetKind == BlueprintBoardDropTargetKind.Bench)
            {
                command = BlueprintCommands.BenchBlueprint(id);
                return true;
            }
            if (request.TargetKind == BlueprintBoardDropTargetKind.Insertion)
            {
                command = BlueprintCommands.ReorderBlueprints(id, request.TargetIndex);
                return true;
            }
            if (request.TargetKind != BlueprintBoardDropTargetKind.ActiveSlot ||
                request.TargetIndex < 0 || request.TargetIndex >= placement.State.Capacity)
            {
                failure = $"Blueprint Board index {request.TargetIndex} is outside capacity.";
                return false;
            }

            int sourceIndex = placement.State.FindActiveIndex(id);
            if (sourceIndex < 0)
            {
                failure = $"Blueprint '{id}' is no longer active.";
                return false;
            }

            command = placement.State.Slots[request.TargetIndex].IsEmpty
                ? BlueprintCommands.MoveBlueprint(id, request.TargetIndex)
                : BlueprintCommands.SwapBlueprints(sourceIndex, request.TargetIndex);
            return true;
        }

        private static string DropDescription(BlueprintCommand command) => command switch
        {
            ActivateBlueprintCommand => "Release to activate this Blueprint.",
            BenchBlueprintCommand => "Release to move this Blueprint to the Bench.",
            MoveBlueprintCommand => "Release to move this Blueprint.",
            SwapBlueprintsCommand => "Release to swap these Blueprints.",
            ReorderBlueprintsCommand => "Release to insert this Blueprint at the previewed position.",
            _ => "Release to complete this board action."
        };
    }
}
