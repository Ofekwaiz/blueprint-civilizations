using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.UI.Views
{
    public sealed class BlueprintBoardView : IBlueprintBoardView
    {
        private const string HoveredClass = "blueprint-card--hovered";
        private const string SelectedClass = "blueprint-card--selected";
        private const string AdjacentClass = "blueprint-card--adjacent";
        private const string DraggingClass = "blueprint-card--dragging";
        private const string DropValidClass = "blueprint-drop-target--valid";
        private const string DropInvalidClass = "blueprint-drop-target--invalid";
        private const string InsertPreviewClass = "blueprint-insertion--preview";
        private const string SwapPreviewClass = "blueprint-slot--swap-preview";

        private readonly VisualElement root;
        private readonly Label capacityLabel;
        private readonly VisualElement activeRow;
        private readonly VisualElement benchRow;
        private readonly VisualElement dragLayer;
        private readonly Label statusLabel;
        private readonly Button undoButton;
        private readonly Button redoButton;
        private readonly BlueprintBoardInteractionState interaction;
        private readonly bool diagnosticsEnabled;
        private readonly List<VisualElement> focusTargets = new();
        private readonly Dictionary<string, VisualElement> cardsById = new(StringComparer.OrdinalIgnoreCase);
        private readonly Action undoClicked;
        private readonly Action redoClicked;

        private VisualElement dragGhost;
        private VisualElement previewTarget;
        private VisualElement previewCard;
        private DropTarget currentDropTarget;
        private BlueprintBoardDropRequest currentPreviewRequest;
        private string selectedId = "";
        private int renderedCapacity;
        private bool disposed;

        public BlueprintBoardView(VisualElement root, float dragThreshold = BlueprintBoardInteractionState.DefaultDragThreshold,
            bool diagnosticsEnabled = false)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            this.diagnosticsEnabled = diagnosticsEnabled;
            interaction = new BlueprintBoardInteractionState(dragThreshold);
            capacityLabel = Require<Label>("blueprint-capacity-label");
            activeRow = Require<VisualElement>("blueprint-active-row");
            benchRow = Require<VisualElement>("blueprint-bench-row");
            dragLayer = Require<VisualElement>("blueprint-drag-layer");
            statusLabel = Require<Label>("blueprint-status-label");
            undoButton = Require<Button>("blueprint-undo-button");
            redoButton = Require<Button>("blueprint-redo-button");

            undoClicked = () => UndoRequested?.Invoke();
            redoClicked = () => RedoRequested?.Invoke();
            undoButton.clicked += undoClicked;
            redoButton.clicked += redoClicked;
            root.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            root.RegisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            root.RegisterCallback<KeyDownEvent>(OnKeyDown);
            root.RegisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
        }

        public event Action<string> SelectionRequested;
        public event Action<string> BenchRequested;
        public event Action<string, int> ReorderRequested;
        public event Action<BlueprintBoardDropRequest> DropPreviewRequested;
        public event Action<BlueprintBoardDropRequest> DropRequested;
        public event Action UndoRequested;
        public event Action RedoRequested;

        public void Render(BlueprintBoardViewModel model)
        {
            if (model == null) throw new ArgumentNullException(nameof(model));
            CancelPointerInteraction("Board refreshed while a pointer interaction was active.", false);
            ClearHoveredCard();
            activeRow.Clear();
            benchRow.Clear();
            focusTargets.Clear();
            cardsById.Clear();
            capacityLabel.text = $"ACTIVE CAPACITY {model.ActiveCount}/{model.Capacity}";
            renderedCapacity = model.Capacity;

            for (int index = 0; index < model.Slots.Count; index++)
            {
                activeRow.Add(CreateInsertionTarget(index, false));
                activeRow.Add(CreateSlot(model.Slots[index]));
            }
            if (model.Capacity > 0) activeRow.Add(CreateInsertionTarget(model.Capacity - 1, true));

            var benchTarget = new DropTarget(BlueprintBoardDropTargetKind.Bench, -1, "");
            var benchDrop = new VisualElement
            {
                name = "blueprint-bench-drop",
                userData = benchTarget,
                tooltip = "Drop an active Blueprint here to bench it."
            };
            benchTarget.Element = benchDrop;
            benchDrop.AddToClassList("blueprint-bench-drop");
            foreach (var card in model.Bench) benchDrop.Add(CreateCard(card, BlueprintBoardDragOrigin.Bench, -1));
            if (model.Bench.Count == 0) benchDrop.Add(new Label("Bench empty") { name = "blueprint-bench-empty" });
            benchRow.Add(benchDrop);

            undoButton.SetEnabled(model.CanUndo);
            redoButton.SetEnabled(model.CanRedo);
            SetSelectionState(model.SelectedBlueprintId, model.AdjacentBlueprintIds);
        }

        public void SetSelectionState(string selectedBlueprintId, IEnumerable<string> adjacentBlueprintIds)
        {
            selectedId = selectedBlueprintId ?? "";
            foreach (var pair in cardsById)
            {
                pair.Value.EnableInClassList(SelectedClass,
                    string.Equals(pair.Key, selectedId, StringComparison.OrdinalIgnoreCase));
                pair.Value.RemoveFromClassList(AdjacentClass);
            }

            foreach (string id in adjacentBlueprintIds ?? Enumerable.Empty<string>())
            {
                if (!string.IsNullOrWhiteSpace(id) && cardsById.TryGetValue(id, out var card))
                    card.AddToClassList(AdjacentClass);
            }
        }

        public void ShowDropPreview(BlueprintBoardDropRequest request, bool isValid, string message)
        {
            if (!ReferenceEquals(request, currentPreviewRequest) || previewTarget == null) return;
            previewTarget.EnableInClassList(DropValidClass, isValid);
            previewTarget.EnableInClassList(DropInvalidClass, !isValid);
            previewCard?.EnableInClassList(DropValidClass, isValid);
            previewCard?.EnableInClassList(DropInvalidClass, !isValid);
            previewTarget.EnableInClassList(InsertPreviewClass,
                isValid && request.TargetKind == BlueprintBoardDropTargetKind.Insertion);
            previewTarget.EnableInClassList(SwapPreviewClass,
                isValid && request.TargetKind == BlueprintBoardDropTargetKind.ActiveSlot &&
                !string.IsNullOrWhiteSpace(request.OccupiedBlueprintId));
            ShowStatus(message, !isValid);
        }

        public void ShowStatus(string message, bool isError)
        {
            statusLabel.text = message ?? "";
            statusLabel.EnableInClassList("blueprint-status--error", isError);
        }

        public void LogInteractionDiagnostic(string message)
        {
            if (diagnosticsEnabled) Debug.Log($"[Blueprint Board Interaction] {message}");
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            CancelPointerInteraction("View disposed.", false);
            undoButton.clicked -= undoClicked;
            redoButton.clicked -= redoClicked;
            root.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerMoveEvent>(OnPointerMove, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerUpEvent>(OnPointerUp, TrickleDown.TrickleDown);
            root.UnregisterCallback<PointerCancelEvent>(OnPointerCancel, TrickleDown.TrickleDown);
            root.UnregisterCallback<KeyDownEvent>(OnKeyDown);
            root.UnregisterCallback<DetachFromPanelEvent>(OnDetachedFromPanel);
        }

        private VisualElement CreateSlot(BlueprintSlotViewModel slotModel)
        {
            string occupiedId = slotModel.Blueprint?.DefinitionId ?? "";
            var target = new DropTarget(BlueprintBoardDropTargetKind.ActiveSlot, slotModel.Index, occupiedId);
            var slot = new VisualElement
            {
                name = $"blueprint-slot-{slotModel.Index}",
                userData = target,
                focusable = slotModel.Blueprint == null,
                tabIndex = 0,
                tooltip = slotModel.Blueprint == null ? $"Empty active Blueprint slot {slotModel.Index + 1}." : ""
            };
            target.Element = slot;
            slot.AddToClassList("blueprint-slot");
            slot.EnableInClassList("blueprint-slot--empty", slotModel.Blueprint == null);
            if (slotModel.Blueprint == null)
            {
                slot.Add(new Label($"EMPTY\n{slotModel.Index + 1}") { name = "blueprint-empty-slot-label" });
                focusTargets.Add(slot);
            }
            else
            {
                slot.Add(CreateCard(slotModel.Blueprint, BlueprintBoardDragOrigin.Active, slotModel.Index));
            }
            return slot;
        }

        private VisualElement CreateInsertionTarget(int targetIndex, bool isAfterLast)
        {
            var data = new DropTarget(BlueprintBoardDropTargetKind.Insertion, targetIndex, "");
            var target = new VisualElement
            {
                name = isAfterLast ? "blueprint-insertion-after-last" : $"blueprint-insertion-{targetIndex}",
                userData = data,
                tooltip = $"Insert at active position {targetIndex + 1}."
            };
            data.Element = target;
            target.AddToClassList("blueprint-insertion");
            return target;
        }

        private VisualElement CreateCard(BlueprintCardViewModel model, BlueprintBoardDragOrigin origin, int sourceIndex)
        {
            var card = new VisualElement
            {
                name = $"blueprint-card-{model.DefinitionId}",
                tooltip = model.Tooltip,
                focusable = true,
                tabIndex = 0,
                viewDataKey = $"blueprint:{model.DefinitionId}",
                userData = new CardData(model.DefinitionId, origin, sourceIndex)
            };
            card.AddToClassList("blueprint-card");
            if (model.Icon != null)
                card.Add(new Image { name = "blueprint-card-icon", sprite = model.Icon, scaleMode = ScaleMode.ScaleToFit });
            card.Add(new Label(model.DisplayName) { name = "blueprint-card-name" });
            card.Add(new Label(model.DefinitionId) { name = "blueprint-card-id" });
            card.RegisterCallback<PointerDownEvent>(evt => BeginPointer(card, model.DefinitionId, origin, sourceIndex, evt));
            card.RegisterCallback<PointerMoveEvent>(OnPointerMove);
            card.RegisterCallback<PointerUpEvent>(OnPointerUp);
            card.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            card.RegisterCallback<PointerEnterEvent>(_ => SetHoveredCard(model.DefinitionId));
            card.RegisterCallback<PointerLeaveEvent>(_ =>
            {
                if (string.Equals(interaction.HoveredBlueprintId, model.DefinitionId, StringComparison.OrdinalIgnoreCase))
                    ClearHoveredCard();
            });
            card.RegisterCallback<PointerCaptureOutEvent>(evt => OnPointerCaptureOut(evt.pointerId));
            cardsById[model.DefinitionId] = card;
            focusTargets.Add(card);
            return card;
        }

        private void OnRootPointerDown(PointerDownEvent evt)
        {
            if (evt.button != 0 || FindCardData(evt.target as VisualElement) != null) return;
            var target = evt.target as VisualElement;
            if (IsDescendantOf(target, activeRow) || IsDescendantOf(target, benchRow))
                SelectionRequested?.Invoke("");
        }

        private void BeginPointer(VisualElement card, string id, BlueprintBoardDragOrigin origin, int sourceIndex,
            PointerDownEvent evt)
        {
            if (evt.button != 0 || disposed) return;
            CancelPointerInteraction("A new pointer interaction replaced the previous one.", false);
            var source = new BlueprintBoardDragSource(id, origin, sourceIndex);
            var position = new Vector2(evt.position.x, evt.position.y);
            if (!interaction.BeginPointer(source, evt.pointerId, position)) return;
            card.CapturePointer(evt.pointerId);
            LogInteractionDiagnostic($"Pointer down: pointer={evt.pointerId}, source={id}, origin={origin}, index={sourceIndex}; pointer captured.");
            evt.StopPropagation();
        }

        private void OnPointerMove(PointerMoveEvent evt)
        {
            if (!interaction.IsPointerActive || evt.pointerId != interaction.PointerId) return;
            evt.StopPropagation();
            var position = new Vector2(evt.position.x, evt.position.y);
            if (interaction.UpdatePointer(evt.pointerId, position))
            {
                ClearHoveredCard();
                if (cardsById.TryGetValue(interaction.DragSource.BlueprintId, out var sourceCard))
                    sourceCard.AddToClassList(DraggingClass);
                CreateDragGhost(interaction.DragSource.BlueprintId);
                LogInteractionDiagnostic($"Drag threshold crossed for '{interaction.DragSource.BlueprintId}'.");
            }
            if (!interaction.IsDragging) return;
            PositionDragGhost(position);
            SetDropTarget(FindDropTarget(position));
        }

        private void OnPointerUp(PointerUpEvent evt)
        {
            if (!interaction.IsPointerActive || evt.pointerId != interaction.PointerId) return;
            evt.StopPropagation();
            var position = new Vector2(evt.position.x, evt.position.y);
            bool wasDragging = interaction.IsDragging;
            DropTarget finalTarget = wasDragging ? FindDropTarget(position) : null;
            if (wasDragging) SetDropTarget(finalTarget);
            VisualElement sourceElement = cardsById.TryGetValue(interaction.DragSource.BlueprintId, out var found) ? found : null;
            interaction.TryCompletePointer(evt.pointerId, out var source, out _);
            ReleasePointer(sourceElement, evt.pointerId);
            ClearDragVisuals();

            if (!wasDragging)
            {
                LogInteractionDiagnostic($"Pointer release completed click selection for '{source.BlueprintId}'.");
                SelectionRequested?.Invoke(source.BlueprintId);
                return;
            }

            if (finalTarget == null)
            {
                ShowStatus("Drop cancelled: no Blueprint Board target was under the pointer.", true);
                LogInteractionDiagnostic("Pointer release cancelled drag because no target was hit.");
                return;
            }

            var request = CreateDropRequest(source, finalTarget);
            LogInteractionDiagnostic($"Pointer release: source={source.BlueprintId}, target={request.TargetKind}, index={request.TargetIndex}.");
            DropRequested?.Invoke(request);
        }

        private void OnPointerCancel(PointerCancelEvent evt)
        {
            if (!interaction.IsPointerActive || evt.pointerId != interaction.PointerId) return;
            evt.StopPropagation();
            CancelPointerInteraction($"Pointer {evt.pointerId} was cancelled.", true);
        }

        private void OnPointerCaptureOut(int pointerId)
        {
            if (!interaction.IsPointerActive || pointerId != interaction.PointerId) return;
            CancelPointerInteraction($"Pointer capture was lost for pointer {pointerId}.", true, false);
        }

        private void OnDetachedFromPanel(DetachFromPanelEvent _) =>
            CancelPointerInteraction("The Blueprint Board detached from its panel.", false, false);

        private void OnKeyDown(KeyDownEvent evt)
        {
            if (evt.ctrlKey && evt.keyCode == KeyCode.Z)
            {
                UndoRequested?.Invoke();
                evt.StopPropagation();
                return;
            }
            if (evt.ctrlKey && evt.keyCode == KeyCode.Y)
            {
                RedoRequested?.Invoke();
                evt.StopPropagation();
                return;
            }
            if ((evt.keyCode == KeyCode.Delete || evt.keyCode == KeyCode.Backspace) && !string.IsNullOrWhiteSpace(selectedId))
            {
                if (cardsById.TryGetValue(selectedId, out var selected) && selected.userData is CardData data &&
                    data.Origin == BlueprintBoardDragOrigin.Active)
                    BenchRequested?.Invoke(selectedId);
                evt.StopPropagation();
                return;
            }
            if (evt.shiftKey && !string.IsNullOrWhiteSpace(selectedId) && cardsById.TryGetValue(selectedId, out var selectedCard) &&
                selectedCard.userData is CardData selectedData && selectedData.Origin == BlueprintBoardDragOrigin.Active)
            {
                if (evt.keyCode == KeyCode.LeftArrow && selectedData.SourceIndex > 0)
                    ReorderRequested?.Invoke(selectedId, selectedData.SourceIndex - 1);
                else if (evt.keyCode == KeyCode.RightArrow && selectedData.SourceIndex < renderedCapacity - 1)
                    ReorderRequested?.Invoke(selectedId, selectedData.SourceIndex + 1);
                else return;
                evt.StopPropagation();
                return;
            }
            if (evt.keyCode != KeyCode.LeftArrow && evt.keyCode != KeyCode.RightArrow &&
                evt.keyCode != KeyCode.Return && evt.keyCode != KeyCode.KeypadEnter) return;

            var focused = root.panel?.focusController?.focusedElement as VisualElement;
            if (evt.keyCode is KeyCode.Return or KeyCode.KeypadEnter)
            {
                if (focused?.userData is CardData cardData) SelectionRequested?.Invoke(cardData.BlueprintId);
                evt.StopPropagation();
                return;
            }
            int currentIndex = focusTargets.IndexOf(focused);
            int direction = evt.keyCode == KeyCode.LeftArrow ? -1 : 1;
            int next = Mathf.Clamp(currentIndex < 0 ? 0 : currentIndex + direction, 0, focusTargets.Count - 1);
            if (focusTargets.Count > 0) focusTargets[next].Focus();
            evt.StopPropagation();
        }

        private void SetHoveredCard(string id)
        {
            if (interaction.IsPointerActive || !interaction.SetHoveredBlueprint(id)) return;
            foreach (var card in cardsById.Values) card.RemoveFromClassList(HoveredClass);
            if (cardsById.TryGetValue(id, out var hovered)) hovered.AddToClassList(HoveredClass);
        }

        private void ClearHoveredCard()
        {
            interaction.SetHoveredBlueprint("");
            foreach (var card in cardsById.Values) card.RemoveFromClassList(HoveredClass);
        }

        private DropTarget FindDropTarget(Vector2 panelPosition)
        {
            VisualElement element = root.panel?.Pick(panelPosition);
            while (element != null)
            {
                if (element.userData is DropTarget target) return target;
                element = element.parent;
            }
            return null;
        }

        private void SetDropTarget(DropTarget target)
        {
            if (ReferenceEquals(target, currentDropTarget)) return;
            ClearDropPreview();
            currentDropTarget = target;
            if (target == null || interaction.DragSource == null) return;
            previewTarget = target.Element;
            currentPreviewRequest = CreateDropRequest(interaction.DragSource, target);
            if (!string.IsNullOrWhiteSpace(target.OccupiedBlueprintId))
                cardsById.TryGetValue(target.OccupiedBlueprintId, out previewCard);
            LogInteractionDiagnostic($"Hovered target: kind={target.Kind}, index={target.Index}, occupied='{target.OccupiedBlueprintId}'.");
            DropPreviewRequested?.Invoke(currentPreviewRequest);
        }

        private static BlueprintBoardDropRequest CreateDropRequest(BlueprintBoardDragSource source, DropTarget target) =>
            new(source, target.Kind, target.Index, target.OccupiedBlueprintId);

        private void CreateDragGhost(string id)
        {
            dragGhost?.RemoveFromHierarchy();
            dragGhost = new VisualElement { name = "blueprint-drag-ghost", pickingMode = PickingMode.Ignore };
            dragGhost.AddToClassList("blueprint-drag-ghost");
            dragGhost.Add(new Label(id));
            dragLayer.Add(dragGhost);
        }

        private void PositionDragGhost(Vector2 panelPosition)
        {
            if (dragGhost == null) return;
            Vector2 local = dragLayer.WorldToLocal(panelPosition);
            dragGhost.style.left = local.x + 14f;
            dragGhost.style.top = local.y + 14f;
        }

        private void CancelPointerInteraction(string reason, bool showStatus, bool releaseCapture = true)
        {
            if (!interaction.IsPointerActive) return;
            int pointerId = interaction.PointerId;
            string sourceId = interaction.DragSource.BlueprintId;
            VisualElement source = cardsById.TryGetValue(sourceId, out var found) ? found : null;
            interaction.CancelPointer();
            if (releaseCapture) ReleasePointer(source, pointerId);
            ClearDragVisuals();
            if (showStatus) ShowStatus(reason, true);
            LogInteractionDiagnostic($"Interaction cancelled: {reason} source={sourceId}, pointer={pointerId}.");
        }

        private void ClearDragVisuals()
        {
            foreach (var card in cardsById.Values) card.RemoveFromClassList(DraggingClass);
            dragGhost?.RemoveFromHierarchy();
            dragGhost = null;
            ClearDropPreview();
        }

        private void ClearDropPreview()
        {
            previewTarget?.RemoveFromClassList(DropValidClass);
            previewTarget?.RemoveFromClassList(DropInvalidClass);
            previewTarget?.RemoveFromClassList(InsertPreviewClass);
            previewTarget?.RemoveFromClassList(SwapPreviewClass);
            previewCard?.RemoveFromClassList(DropValidClass);
            previewCard?.RemoveFromClassList(DropInvalidClass);
            previewTarget = null;
            previewCard = null;
            currentDropTarget = null;
            currentPreviewRequest = null;
        }

        private static void ReleasePointer(VisualElement element, int pointerId)
        {
            if (element?.panel != null && element.HasPointerCapture(pointerId)) element.ReleasePointer(pointerId);
        }

        private static CardData FindCardData(VisualElement element)
        {
            while (element != null)
            {
                if (element.userData is CardData data) return data;
                element = element.parent;
            }
            return null;
        }

        private static bool IsDescendantOf(VisualElement element, VisualElement ancestor)
        {
            while (element != null)
            {
                if (ReferenceEquals(element, ancestor)) return true;
                element = element.parent;
            }
            return false;
        }

        private T Require<T>(string name) where T : VisualElement =>
            root.Q<T>(name) ?? throw new InvalidOperationException($"Blueprint Board UXML is missing required element '{name}'.");

        private sealed class CardData
        {
            public CardData(string id, BlueprintBoardDragOrigin origin, int sourceIndex)
            {
                BlueprintId = id;
                Origin = origin;
                SourceIndex = sourceIndex;
            }

            public string BlueprintId { get; }
            public BlueprintBoardDragOrigin Origin { get; }
            public int SourceIndex { get; }
        }

        private sealed class DropTarget
        {
            public DropTarget(BlueprintBoardDropTargetKind kind, int index, string occupiedBlueprintId)
            {
                Kind = kind;
                Index = index;
                OccupiedBlueprintId = occupiedBlueprintId ?? "";
            }

            public BlueprintBoardDropTargetKind Kind { get; }
            public int Index { get; }
            public string OccupiedBlueprintId { get; }
            public VisualElement Element { get; set; }
        }
    }
}
