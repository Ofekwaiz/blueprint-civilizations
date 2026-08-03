using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueprintCivilizations.Blueprints
{
    /// <summary>
    /// Transactional authority for Blueprint Board mutations. Expected command failures return results;
    /// successful mutations are revisioned, evented, and available to runtime undo/redo.
    /// </summary>
    public sealed class BlueprintPlacementService
    {
        private readonly BlueprintBoardState board;
        private readonly BlueprintValidationService validation;
        private readonly Stack<string> undo = new();
        private readonly Stack<string> redo = new();

        public BlueprintPlacementService(BlueprintBoardState board, BlueprintValidationService validation = null)
        {
            this.board = board ?? throw new ArgumentNullException(nameof(board));
            this.validation = validation ?? new BlueprintValidationService();
        }

        public event Action<BlueprintEvent> EventRaised;
        public BlueprintBoardState State => board;
        public bool CanUndo => undo.Count > 0;
        public bool CanRedo => redo.Count > 0;

        /// <summary>
        /// Validates a command against an isolated clone of the current board. The authoritative board,
        /// history, revision, events, and persistence bindings are not changed.
        /// </summary>
        public BlueprintCommandResult Preview(BlueprintCommand command)
        {
            var clone = BlueprintBoardSerializer.TryDeserialize(BlueprintBoardSerializer.Serialize(board));
            if (!clone.Success)
                return BlueprintCommandResult.Failed(BlueprintCommandFailure.InvalidBoardState, clone.Error);
            return new BlueprintPlacementService(clone.Board, validation).Execute(command);
        }

        public BlueprintCommandResult Execute(BlueprintCommand command)
        {
            if (command == null) return BlueprintCommandResult.Failed(BlueprintCommandFailure.InvalidCommand, "Blueprint command is null.");
            if (!string.IsNullOrWhiteSpace(command.PlayerId) && !string.Equals(command.PlayerId, board.OwnerId, StringComparison.Ordinal))
                return BlueprintCommandResult.Failed(BlueprintCommandFailure.OwnerMismatch, $"Command player '{command.PlayerId}' does not own this board.");
            if (command.ExpectedRevision.HasValue && command.ExpectedRevision.Value != board.Revision)
                return BlueprintCommandResult.Failed(BlueprintCommandFailure.StaleRevision,
                    $"Command expected board revision {command.ExpectedRevision.Value}, but current revision is {board.Revision}.");

            var currentIssues = validation.Validate(board);
            if (currentIssues.Any(issue => issue.Severity == BlueprintValidationSeverity.Error))
                return BlueprintCommandResult.Failed(BlueprintCommandFailure.InvalidBoardState,
                    "Blueprint Board contains validation errors and was not changed.", currentIssues);

            string before = BlueprintBoardSerializer.Serialize(board);
            Operation operation = command switch
            {
                ActivateBlueprintCommand activate => Activate(activate),
                BenchBlueprintCommand bench => Bench(bench),
                MoveBlueprintCommand move => Move(move),
                SwapBlueprintsCommand swap => Swap(swap),
                ReorderBlueprintsCommand reorder => Reorder(reorder),
                SetBlueprintCapacityCommand capacity => SetCapacity(capacity),
                _ => Operation.Fail(BlueprintCommandFailure.InvalidCommand, $"Unsupported Blueprint command type '{command.GetType().Name}'.")
            };

            if (!operation.Success) return BlueprintCommandResult.Failed(operation.Failure, operation.Message);

            SynchronizePlacements();
            board.IncrementRevision();
            var postIssues = validation.Validate(board);
            if (postIssues.Any(issue => issue.Severity == BlueprintValidationSeverity.Error))
            {
                RestoreSnapshot(before, board.Revision - 1);
                return BlueprintCommandResult.Failed(BlueprintCommandFailure.InvalidBoardState,
                    "The command would create an invalid Blueprint Board and was rolled back.", postIssues);
            }

            undo.Push(before);
            redo.Clear();
            var blueprintEvent = BlueprintEvents.Create(operation.EventType, operation.BlueprintId,
                operation.FromIndex, operation.ToIndex, board.Revision, operation.SecondaryBlueprintId);
            EventRaised?.Invoke(blueprintEvent);
            return BlueprintCommandResult.Succeeded(blueprintEvent);
        }

        public BlueprintCommandResult Undo()
        {
            if (undo.Count == 0) return BlueprintCommandResult.Failed(BlueprintCommandFailure.NothingToUndo, "There is no Blueprint Board command to undo.");
            int nextRevision = board.Revision + 1;
            redo.Push(BlueprintBoardSerializer.Serialize(board));
            RestoreSnapshot(undo.Pop(), nextRevision);
            var blueprintEvent = BlueprintEvents.Create(BlueprintEventType.UndoCompleted, "", -1, -1, board.Revision);
            EventRaised?.Invoke(blueprintEvent);
            return BlueprintCommandResult.Succeeded(blueprintEvent);
        }

        public BlueprintCommandResult Redo()
        {
            if (redo.Count == 0) return BlueprintCommandResult.Failed(BlueprintCommandFailure.NothingToRedo, "There is no Blueprint Board command to redo.");
            int nextRevision = board.Revision + 1;
            undo.Push(BlueprintBoardSerializer.Serialize(board));
            RestoreSnapshot(redo.Pop(), nextRevision);
            var blueprintEvent = BlueprintEvents.Create(BlueprintEventType.RedoCompleted, "", -1, -1, board.Revision);
            EventRaised?.Invoke(blueprintEvent);
            return BlueprintCommandResult.Succeeded(blueprintEvent);
        }

        private Operation Activate(ActivateBlueprintCommand command)
        {
            var blueprint = board.FindBlueprint(command.DefinitionId);
            if (blueprint == null) return Missing(command.DefinitionId);
            if (blueprint.Location == BlueprintLocationState.Active)
                return Operation.Fail(BlueprintCommandFailure.AlreadyActive, $"Blueprint '{command.DefinitionId}' is already active.");
            if (board.ActiveCount >= board.Capacity)
                return Operation.Fail(BlueprintCommandFailure.CapacityExceeded, "Blueprint Capacity is full; bench an active Blueprint first.");

            int target = command.TargetIndex < 0 ? FindEmptySlot() : command.TargetIndex;
            if (!IsValidIndex(target)) return InvalidIndex(target);
            if (!board.Bench.MutableIds.Remove(blueprint.DefinitionId))
                return Operation.Fail(BlueprintCommandFailure.InvalidBoardState, $"Blueprint '{blueprint.DefinitionId}' is not present on the bench.");

            InsertAt(target, blueprint.DefinitionId);
            return Operation.Ok(BlueprintEventType.Activated, blueprint.DefinitionId, -1, target);
        }

        private Operation Bench(BenchBlueprintCommand command)
        {
            var blueprint = board.FindBlueprint(command.DefinitionId);
            if (blueprint == null) return Missing(command.DefinitionId);
            int source = board.FindActiveIndex(command.DefinitionId);
            if (source < 0) return Operation.Fail(BlueprintCommandFailure.NotActive, $"Blueprint '{command.DefinitionId}' is not active.");
            board.MutableSlots[source].SetBlueprint("");
            board.Bench.MutableIds.Add(blueprint.DefinitionId);
            return Operation.Ok(BlueprintEventType.Benched, blueprint.DefinitionId, source, -1);
        }

        private Operation Move(MoveBlueprintCommand command)
        {
            var blueprint = board.FindBlueprint(command.DefinitionId);
            if (blueprint == null) return Missing(command.DefinitionId);
            int source = board.FindActiveIndex(command.DefinitionId);
            if (source < 0) return Operation.Fail(BlueprintCommandFailure.NotActive, $"Blueprint '{command.DefinitionId}' is not active.");
            if (!IsValidIndex(command.TargetIndex)) return InvalidIndex(command.TargetIndex);
            if (source == command.TargetIndex) return Operation.Fail(BlueprintCommandFailure.InvalidCommand, "Source and target slots are the same.");
            if (!board.MutableSlots[command.TargetIndex].IsEmpty)
                return Operation.Fail(BlueprintCommandFailure.OccupiedSlot, $"Slot {command.TargetIndex} is occupied; use SwapBlueprints or ReorderBlueprints.");
            board.MutableSlots[source].SetBlueprint("");
            board.MutableSlots[command.TargetIndex].SetBlueprint(blueprint.DefinitionId);
            return Operation.Ok(BlueprintEventType.Moved, blueprint.DefinitionId, source, command.TargetIndex);
        }

        private Operation Swap(SwapBlueprintsCommand command)
        {
            if (!IsValidIndex(command.FirstIndex)) return InvalidIndex(command.FirstIndex);
            if (!IsValidIndex(command.SecondIndex)) return InvalidIndex(command.SecondIndex);
            if (command.FirstIndex == command.SecondIndex)
                return Operation.Fail(BlueprintCommandFailure.InvalidSwap, "A Blueprint cannot be swapped with its own slot.");
            var first = board.MutableSlots[command.FirstIndex];
            var second = board.MutableSlots[command.SecondIndex];
            if (first.IsEmpty || second.IsEmpty)
                return Operation.Fail(BlueprintCommandFailure.InvalidSwap, "SwapBlueprints requires two occupied active slots.");
            string firstId = first.BlueprintDefinitionId;
            string secondId = second.BlueprintDefinitionId;
            first.SetBlueprint(secondId);
            second.SetBlueprint(firstId);
            return Operation.Ok(BlueprintEventType.Swapped, firstId, command.FirstIndex, command.SecondIndex, secondId);
        }

        private Operation Reorder(ReorderBlueprintsCommand command)
        {
            var blueprint = board.FindBlueprint(command.DefinitionId);
            if (blueprint == null) return Missing(command.DefinitionId);
            int source = board.FindActiveIndex(command.DefinitionId);
            if (source < 0) return Operation.Fail(BlueprintCommandFailure.NotActive, $"Blueprint '{command.DefinitionId}' is not active.");
            if (!IsValidIndex(command.TargetIndex)) return InvalidIndex(command.TargetIndex);
            if (source == command.TargetIndex) return Operation.Fail(BlueprintCommandFailure.InvalidCommand, "Source and target positions are the same.");

            if (source < command.TargetIndex)
            {
                for (int index = source; index < command.TargetIndex; index++)
                    board.MutableSlots[index].SetBlueprint(board.MutableSlots[index + 1].BlueprintDefinitionId);
            }
            else
            {
                for (int index = source; index > command.TargetIndex; index--)
                    board.MutableSlots[index].SetBlueprint(board.MutableSlots[index - 1].BlueprintDefinitionId);
            }
            board.MutableSlots[command.TargetIndex].SetBlueprint(blueprint.DefinitionId);
            return Operation.Ok(BlueprintEventType.Reordered, blueprint.DefinitionId, source, command.TargetIndex);
        }

        private Operation SetCapacity(SetBlueprintCapacityCommand command)
        {
            if (command.Capacity < 0) return Operation.Fail(BlueprintCommandFailure.InvalidCommand, "Blueprint Capacity cannot be negative.");
            if (command.Capacity == board.Capacity) return Operation.Fail(BlueprintCommandFailure.InvalidCommand, "Blueprint Capacity is already set to that value.");
            if (command.Capacity < board.Capacity)
            {
                if (board.ActiveCount > command.Capacity)
                    return Operation.Fail(BlueprintCommandFailure.CapacityExceeded, "Bench active Blueprints before reducing capacity.");
                for (int index = command.Capacity; index < board.Capacity; index++)
                {
                    if (!board.MutableSlots[index].IsEmpty)
                        return Operation.Fail(BlueprintCommandFailure.CapacityExceeded, $"Slot {index} must be empty before reducing capacity.");
                }
                board.MutableSlots.RemoveRange(command.Capacity, board.Capacity - command.Capacity);
            }
            else
            {
                for (int index = board.Capacity; index < command.Capacity; index++) board.MutableSlots.Add(new BlueprintSlotState(index));
            }
            int oldCapacity = board.Capacity;
            board.SetCapacity(command.Capacity);
            return Operation.Ok(BlueprintEventType.CapacityChanged, "", oldCapacity, command.Capacity);
        }

        private void InsertAt(int target, string definitionId)
        {
            if (board.MutableSlots[target].IsEmpty)
            {
                board.MutableSlots[target].SetBlueprint(definitionId);
                return;
            }

            int emptyRight = -1;
            for (int index = target + 1; index < board.Capacity; index++)
            {
                if (board.MutableSlots[index].IsEmpty) { emptyRight = index; break; }
            }
            int emptyLeft = -1;
            for (int index = target - 1; index >= 0; index--)
            {
                if (board.MutableSlots[index].IsEmpty) { emptyLeft = index; break; }
            }
            bool useRight = emptyRight >= 0 && (emptyLeft < 0 || emptyRight - target <= target - emptyLeft);
            if (useRight)
            {
                for (int index = emptyRight; index > target; index--)
                    board.MutableSlots[index].SetBlueprint(board.MutableSlots[index - 1].BlueprintDefinitionId);
                board.MutableSlots[target].SetBlueprint(definitionId);
                return;
            }

            for (int index = emptyLeft; index < target; index++)
                board.MutableSlots[index].SetBlueprint(board.MutableSlots[index + 1].BlueprintDefinitionId);
            board.MutableSlots[target].SetBlueprint(definitionId);
        }

        private void SynchronizePlacements()
        {
            foreach (var blueprint in board.MutableBlueprints)
            {
                if (blueprint == null) continue;
                int activeIndex = board.FindActiveIndex(blueprint.DefinitionId);
                blueprint.SetPlacement(activeIndex >= 0 ? BlueprintLocationState.Active : BlueprintLocationState.Benched, activeIndex);
            }
            for (int index = 0; index < board.MutableSlots.Count; index++) board.MutableSlots[index].SetIndex(index);
        }

        private void RestoreSnapshot(string json, int revision)
        {
            var restored = BlueprintBoardSerializer.TryDeserialize(json);
            if (!restored.Success) throw new InvalidOperationException(restored.Error);
            board.RestoreFrom(restored.Board, revision);
        }

        private int FindEmptySlot()
        {
            for (int index = 0; index < board.Capacity; index++) if (board.MutableSlots[index].IsEmpty) return index;
            return -1;
        }

        private bool IsValidIndex(int index) => index >= 0 && index < board.Capacity;
        private static Operation Missing(string id) => Operation.Fail(BlueprintCommandFailure.MissingBlueprint, $"Blueprint '{id}' is not owned by this player.");
        private static Operation InvalidIndex(int index) => Operation.Fail(BlueprintCommandFailure.InvalidIndex, $"Blueprint Board index {index} is outside capacity.");

        private readonly struct Operation
        {
            private Operation(bool success, BlueprintCommandFailure failure, string message, BlueprintEventType eventType,
                string blueprintId, int fromIndex, int toIndex, string secondaryBlueprintId)
            {
                Success = success; Failure = failure; Message = message; EventType = eventType;
                BlueprintId = blueprintId; FromIndex = fromIndex; ToIndex = toIndex; SecondaryBlueprintId = secondaryBlueprintId;
            }

            public bool Success { get; }
            public BlueprintCommandFailure Failure { get; }
            public string Message { get; }
            public BlueprintEventType EventType { get; }
            public string BlueprintId { get; }
            public int FromIndex { get; }
            public int ToIndex { get; }
            public string SecondaryBlueprintId { get; }

            public static Operation Ok(BlueprintEventType type, string id, int from, int to, string secondaryId = "") =>
                new(true, BlueprintCommandFailure.None, "", type, id, from, to, secondaryId);
            public static Operation Fail(BlueprintCommandFailure failure, string message) =>
                new(false, failure, message, default, "", -1, -1, "");
        }
    }
}
