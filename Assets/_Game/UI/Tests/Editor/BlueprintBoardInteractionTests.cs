using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using BlueprintCivilizations.Blueprints;
using BlueprintCivilizations.UI.Presenters;
using BlueprintCivilizations.UI.ViewModels;
using BlueprintCivilizations.UI.Views;
using NUnit.Framework;
using UnityEngine;

namespace BlueprintCivilizations.UI.Tests
{
    public sealed class BlueprintBoardInteractionTests
    {
        [Test]
        public void InteractionState_HoverTracksOnlyMostRecentBlueprint()
        {
            var state = new BlueprintBoardInteractionState();

            Assert.That(state.SetHoveredBlueprint("A"), Is.True);
            Assert.That(state.SetHoveredBlueprint("B"), Is.True);

            Assert.That(state.HoveredBlueprintId, Is.EqualTo("B"));
            Assert.That(state.SetHoveredBlueprint("B"), Is.False);
        }

        [Test]
        public void InteractionState_DragStartsOnlyAfterThresholdAndTracksStableId()
        {
            var state = new BlueprintBoardInteractionState(6f);
            var source = new BlueprintBoardDragSource("A", BlueprintBoardDragOrigin.Active, 0);
            Assert.That(state.BeginPointer(source, 7, Vector2.zero), Is.True);

            Assert.That(state.UpdatePointer(7, new Vector2(3f, 4f)), Is.False);
            Assert.That(state.IsDragging, Is.False);
            Assert.That(state.UpdatePointer(7, new Vector2(6f, 0f)), Is.True);
            Assert.That(state.IsDragging, Is.True);
            Assert.That(state.DragSource.BlueprintId, Is.EqualTo("A"));
        }

        [Test]
        public void InteractionState_CancelClearsPointerAndDragSource()
        {
            var state = new BlueprintBoardInteractionState(1f);
            state.BeginPointer(new BlueprintBoardDragSource("A", BlueprintBoardDragOrigin.Bench, -1), 4, Vector2.zero);
            state.UpdatePointer(4, Vector2.one);

            Assert.That(state.CancelPointer(4), Is.True);
            Assert.That(state.IsPointerActive, Is.False);
            Assert.That(state.IsDragging, Is.False);
            Assert.That(state.DragSource, Is.Null);
            Assert.That(state.PointerId, Is.EqualTo(-1));
        }

        [Test]
        public void Presenter_SelectionTransfersAndOnlyOneSelectionIsRendered()
        {
            using var fixture = CreateFixture();

            fixture.View.RequestSelection("A");
            fixture.View.RequestSelection("B");

            Assert.That(fixture.Presenter.SelectedBlueprintId, Is.EqualTo("B"));
            Assert.That(fixture.View.SelectedId, Is.EqualTo("B"));
            Assert.That(fixture.View.SelectionUpdates, Is.EqualTo(2));
        }

        [Test]
        public void Presenter_AdjacencyFollowsSelectionAndClearsWithSelection()
        {
            using var fixture = CreateFixture();

            fixture.View.RequestSelection("A");
            CollectionAssert.AreEqual(new[] { "B" }, fixture.View.AdjacentIds);

            fixture.View.RequestSelection("B");
            CollectionAssert.AreEqual(new[] { "A" }, fixture.View.AdjacentIds);

            fixture.View.RequestSelection("");
            Assert.That(fixture.View.AdjacentIds, Is.Empty);
        }

        [Test]
        public void Presenter_DropPreviewUsesPlacementValidationWithoutMutation()
        {
            using var fixture = CreateFixture();
            string before = BlueprintBoardSerializer.Serialize(fixture.Board);
            var request = Drop("C", BlueprintBoardDragOrigin.Bench, -1,
                BlueprintBoardDropTargetKind.ActiveSlot, 2, "");

            fixture.View.RequestDropPreview(request);

            Assert.That(fixture.View.LastPreviewValid, Is.True);
            Assert.That(BlueprintBoardSerializer.Serialize(fixture.Board), Is.EqualTo(before));
            Assert.That(fixture.Service.CanUndo, Is.True, "Fixture setup history is unchanged by preview.");
        }

        [Test]
        public void Presenter_BenchToEmptySlotDispatchesActivateAndPreservesSelection()
        {
            using var fixture = CreateFixture();
            fixture.View.RequestSelection("C");
            int renderCount = fixture.View.RenderCount;

            fixture.View.RequestDrop(Drop("C", BlueprintBoardDragOrigin.Bench, -1,
                BlueprintBoardDropTargetKind.ActiveSlot, 2, ""));

            Assert.That(fixture.Board.FindActiveIndex("C"), Is.EqualTo(2));
            Assert.That(fixture.Presenter.SelectedBlueprintId, Is.EqualTo("C"));
            Assert.That(fixture.View.RenderCount, Is.EqualTo(renderCount + 1));
        }

        [Test]
        public void Presenter_ActiveToBenchDispatchesBenchBlueprint()
        {
            using var fixture = CreateFixture();

            fixture.View.RequestDrop(Drop("A", BlueprintBoardDragOrigin.Active, 0,
                BlueprintBoardDropTargetKind.Bench, -1, ""));

            Assert.That(fixture.Board.FindActiveIndex("A"), Is.EqualTo(-1));
            Assert.That(fixture.Board.Bench.BlueprintDefinitionIds, Does.Contain("A"));
        }

        [Test]
        public void Presenter_ActiveToEmptySlotDispatchesMoveBlueprint()
        {
            using var fixture = CreateFixture();

            fixture.View.RequestDrop(Drop("A", BlueprintBoardDragOrigin.Active, 0,
                BlueprintBoardDropTargetKind.ActiveSlot, 3, ""));

            Assert.That(fixture.Board.FindActiveIndex("A"), Is.EqualTo(3));
            Assert.That(fixture.Board.FindActiveIndex("B"), Is.EqualTo(1));
        }

        [Test]
        public void Presenter_ActiveToOccupiedSlotDispatchesSwapBlueprints()
        {
            using var fixture = CreateFixture();

            fixture.View.RequestDrop(Drop("A", BlueprintBoardDragOrigin.Active, 0,
                BlueprintBoardDropTargetKind.ActiveSlot, 1, "B"));

            CollectionAssert.AreEqual(new[] { "B", "A", "", "" }, SlotIds(fixture.Board));
        }

        [Test]
        public void Presenter_ActiveInsertionDispatchesReorderBlueprints()
        {
            using var fixture = CreateFixture();
            fixture.Service.Execute(BlueprintCommands.ActivateBlueprint("C", 2));

            fixture.View.RequestDrop(Drop("A", BlueprintBoardDragOrigin.Active, 0,
                BlueprintBoardDropTargetKind.Insertion, 2, ""));

            CollectionAssert.AreEqual(new[] { "B", "C", "A", "" }, SlotIds(fixture.Board));
        }

        [Test]
        public void Presenter_InvalidSameSlotDropLeavesStateUnchangedAndShowsReason()
        {
            using var fixture = CreateFixture();
            string before = BlueprintBoardSerializer.Serialize(fixture.Board);

            fixture.View.RequestDrop(Drop("A", BlueprintBoardDragOrigin.Active, 0,
                BlueprintBoardDropTargetKind.ActiveSlot, 0, "A"));

            Assert.That(BlueprintBoardSerializer.Serialize(fixture.Board), Is.EqualTo(before));
            Assert.That(fixture.View.StatusIsError, Is.True);
            Assert.That(fixture.View.Status, Does.Contain("own slot"));
        }

        [Test]
        public void Presenter_SelectionClearsWhenSelectedBlueprintIsRemovedThenRefreshed()
        {
            using var fixture = CreateFixture();
            fixture.View.RequestSelection("A");
            OwnedBlueprints(fixture.Board).Remove(fixture.Board.FindBlueprint("A"));

            fixture.Presenter.Refresh();

            Assert.That(fixture.Presenter.SelectedBlueprintId, Is.Empty);
            Assert.That(fixture.View.LastModel.SelectedBlueprintId, Is.Empty);
            Assert.That(fixture.View.LastModel.AdjacentBlueprintIds, Is.Empty);
        }

        [Test]
        public void PlacementPreview_ValidCommandDoesNotChangeRevisionHistoryOrSerializedState()
        {
            using var fixture = CreateFixture();
            string before = BlueprintBoardSerializer.Serialize(fixture.Board);
            int revision = fixture.Board.Revision;
            bool canUndo = fixture.Service.CanUndo;

            BlueprintCommandResult result = fixture.Service.Preview(BlueprintCommands.ActivateBlueprint("C", 2));

            Assert.That(result.Success, Is.True);
            Assert.That(BlueprintBoardSerializer.Serialize(fixture.Board), Is.EqualTo(before));
            Assert.That(fixture.Board.Revision, Is.EqualTo(revision));
            Assert.That(fixture.Service.CanUndo, Is.EqualTo(canUndo));
        }

        private static Fixture CreateFixture()
        {
            const string owner = "PLAYER";
            var board = new BlueprintBoardState(owner, 4, new BlueprintState[]
            {
                new UnitBlueprintState("A", owner),
                new UnitBlueprintState("B", owner),
                new UnitBlueprintState("C", owner),
                new UnitBlueprintState("D", owner)
            });
            var service = new BlueprintPlacementService(board);
            Assert.That(service.Execute(BlueprintCommands.ActivateBlueprint("A", 0)).Success, Is.True);
            Assert.That(service.Execute(BlueprintCommands.ActivateBlueprint("B", 1)).Success, Is.True);
            var view = new FakeView();
            var presenter = new BlueprintBoardPresenter(view, service, new BlueprintAdjacencyService(), new FakePresentation());
            return new Fixture(board, service, view, presenter);
        }

        private static BlueprintBoardDropRequest Drop(string id, BlueprintBoardDragOrigin origin, int sourceIndex,
            BlueprintBoardDropTargetKind kind, int targetIndex, string occupiedId) =>
            new(new BlueprintBoardDragSource(id, origin, sourceIndex), kind, targetIndex, occupiedId);

        private static string[] SlotIds(BlueprintBoardState board) =>
            board.Slots.Select(slot => slot.BlueprintDefinitionId).ToArray();

        private static List<BlueprintState> OwnedBlueprints(BlueprintBoardState board) =>
            (List<BlueprintState>)typeof(BlueprintBoardState).GetField("blueprints", BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(board);

        private sealed class Fixture : IDisposable
        {
            public Fixture(BlueprintBoardState board, BlueprintPlacementService service, FakeView view,
                BlueprintBoardPresenter presenter)
            {
                Board = board;
                Service = service;
                View = view;
                Presenter = presenter;
            }

            public BlueprintBoardState Board { get; }
            public BlueprintPlacementService Service { get; }
            public FakeView View { get; }
            public BlueprintBoardPresenter Presenter { get; }
            public void Dispose() => Presenter.Dispose();
        }

        private sealed class FakePresentation : IBlueprintBoardPresentationResolver
        {
            public bool TryResolve(string definitionId, out BlueprintCardViewModel card)
            {
                card = new BlueprintCardViewModel(definitionId, definitionId, definitionId, null);
                return true;
            }
        }

        private sealed class FakeView : IBlueprintBoardView
        {
            public event Action<string> SelectionRequested;
            public event Action<string> BenchRequested;
            public event Action<string, int> ReorderRequested;
            public event Action<BlueprintBoardDropRequest> DropPreviewRequested;
            public event Action<BlueprintBoardDropRequest> DropRequested;
            public event Action UndoRequested;
            public event Action RedoRequested;

            public BlueprintBoardViewModel LastModel { get; private set; }
            public int RenderCount { get; private set; }
            public int SelectionUpdates { get; private set; }
            public string SelectedId { get; private set; } = "";
            public string[] AdjacentIds { get; private set; } = Array.Empty<string>();
            public bool LastPreviewValid { get; private set; }
            public string Status { get; private set; } = "";
            public bool StatusIsError { get; private set; }

            public void Render(BlueprintBoardViewModel model)
            {
                LastModel = model;
                RenderCount++;
                SelectedId = model.SelectedBlueprintId;
                AdjacentIds = model.AdjacentBlueprintIds.ToArray();
            }

            public void SetSelectionState(string selectedBlueprintId, IEnumerable<string> adjacentBlueprintIds)
            {
                SelectionUpdates++;
                SelectedId = selectedBlueprintId ?? "";
                AdjacentIds = (adjacentBlueprintIds ?? Enumerable.Empty<string>()).ToArray();
            }

            public void ShowDropPreview(BlueprintBoardDropRequest request, bool isValid, string message)
            {
                LastPreviewValid = isValid;
                ShowStatus(message, !isValid);
            }

            public void ShowStatus(string message, bool isError)
            {
                Status = message ?? "";
                StatusIsError = isError;
            }

            public void LogInteractionDiagnostic(string message) { }
            public void Dispose() { }
            public void RequestSelection(string id) => SelectionRequested?.Invoke(id);
            public void RequestDropPreview(BlueprintBoardDropRequest request) => DropPreviewRequested?.Invoke(request);
            public void RequestDrop(BlueprintBoardDropRequest request) => DropRequested?.Invoke(request);
            public void RequestBench(string id) => BenchRequested?.Invoke(id);
            public void RequestReorder(string id, int index) => ReorderRequested?.Invoke(id, index);
            public void RequestUndo() => UndoRequested?.Invoke();
            public void RequestRedo() => RedoRequested?.Invoke();
        }
    }
}
