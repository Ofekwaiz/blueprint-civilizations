using System;
using UnityEngine;

namespace BlueprintCivilizations.UI.ViewModels
{
    public enum BlueprintBoardDragOrigin
    {
        Active,
        Bench
    }

    public enum BlueprintBoardDropTargetKind
    {
        ActiveSlot,
        Insertion,
        Bench
    }

    /// <summary>Stable-ID payload for one pending or active runtime Blueprint drag.</summary>
    public sealed class BlueprintBoardDragSource
    {
        public BlueprintBoardDragSource(string blueprintId, BlueprintBoardDragOrigin origin, int sourceIndex)
        {
            BlueprintId = blueprintId ?? "";
            Origin = origin;
            SourceIndex = sourceIndex;
        }

        public string BlueprintId { get; }
        public BlueprintBoardDragOrigin Origin { get; }
        public int SourceIndex { get; }
    }

    /// <summary>Presentation intent describing a drag source and geometric drop target.</summary>
    public sealed class BlueprintBoardDropRequest
    {
        public BlueprintBoardDropRequest(BlueprintBoardDragSource source, BlueprintBoardDropTargetKind targetKind,
            int targetIndex, string occupiedBlueprintId)
        {
            Source = source ?? throw new ArgumentNullException(nameof(source));
            TargetKind = targetKind;
            TargetIndex = targetIndex;
            OccupiedBlueprintId = occupiedBlueprintId ?? "";
        }

        public BlueprintBoardDragSource Source { get; }
        public BlueprintBoardDropTargetKind TargetKind { get; }
        public int TargetIndex { get; }
        public string OccupiedBlueprintId { get; }
    }

    /// <summary>
    /// Deterministic pointer interaction state. It owns no board rules and never mutates gameplay state.
    /// </summary>
    public sealed class BlueprintBoardInteractionState
    {
        public const float DefaultDragThreshold = 6f;

        private readonly float dragThreshold;
        private Vector2 pointerStart;

        public BlueprintBoardInteractionState(float dragThreshold = DefaultDragThreshold)
        {
            if (dragThreshold < 0f) throw new ArgumentOutOfRangeException(nameof(dragThreshold));
            this.dragThreshold = dragThreshold;
        }

        public string HoveredBlueprintId { get; private set; } = "";
        public BlueprintBoardDragSource DragSource { get; private set; }
        public int PointerId { get; private set; } = -1;
        public bool IsPointerActive => DragSource != null;
        public bool IsDragging { get; private set; }

        public bool SetHoveredBlueprint(string blueprintId)
        {
            string next = blueprintId ?? "";
            if (string.Equals(HoveredBlueprintId, next, StringComparison.OrdinalIgnoreCase)) return false;
            HoveredBlueprintId = next;
            return true;
        }

        public bool BeginPointer(BlueprintBoardDragSource source, int pointerId, Vector2 position)
        {
            if (source == null || string.IsNullOrWhiteSpace(source.BlueprintId) || pointerId < 0) return false;
            CancelPointer();
            DragSource = source;
            PointerId = pointerId;
            pointerStart = position;
            IsDragging = false;
            return true;
        }

        /// <summary>Returns true only on the update that first crosses the configured threshold.</summary>
        public bool UpdatePointer(int pointerId, Vector2 position)
        {
            if (!IsPointerActive || pointerId != PointerId || IsDragging) return false;
            if (Vector2.Distance(pointerStart, position) < dragThreshold) return false;
            IsDragging = true;
            return true;
        }

        public bool TryCompletePointer(int pointerId, out BlueprintBoardDragSource source, out bool completedDrag)
        {
            source = null;
            completedDrag = false;
            if (!IsPointerActive || pointerId != PointerId) return false;
            source = DragSource;
            completedDrag = IsDragging;
            CancelPointer();
            return true;
        }

        public bool CancelPointer(int? pointerId = null)
        {
            if (!IsPointerActive || (pointerId.HasValue && pointerId.Value != PointerId)) return false;
            DragSource = null;
            PointerId = -1;
            IsDragging = false;
            return true;
        }
    }
}
