using System;
using System.Collections.Generic;
using System.Linq;

namespace BlueprintCivilizations.Blueprints
{
    public enum BlueprintValidationSeverity { Warning, Error }

    public enum BlueprintValidationCode
    {
        NullBoard,
        InvalidCapacity,
        CapacitySlotMismatch,
        NullSlot,
        InvalidBoardIndex,
        CapacityOverflow,
        DuplicateActiveBlueprint,
        DuplicateBenchBlueprint,
        ActiveAndBenchDuplicate,
        NullBlueprint,
        DuplicateOwnedBlueprint,
        BrokenReference,
        OwnerMismatch,
        PlacementMismatch,
        UnsupportedSaveVersion
    }

    public sealed class BlueprintValidationIssue
    {
        public BlueprintValidationIssue(BlueprintValidationCode code, BlueprintValidationSeverity severity,
            string blueprintId, int boardIndex, string message)
        {
            Code = code;
            Severity = severity;
            BlueprintId = blueprintId ?? "";
            BoardIndex = boardIndex;
            Message = message ?? "";
        }

        public BlueprintValidationCode Code { get; }
        public BlueprintValidationSeverity Severity { get; }
        public string BlueprintId { get; }
        public int BoardIndex { get; }
        public string Message { get; }
    }

    public sealed class BlueprintValidationService
    {
        private static readonly StringComparer IdComparer = StringComparer.OrdinalIgnoreCase;
        private readonly IBlueprintDefinitionResolver resolver;

        public BlueprintValidationService(IBlueprintDefinitionResolver resolver = null) => this.resolver = resolver;

        public IReadOnlyList<BlueprintValidationIssue> Validate(BlueprintBoardState board)
        {
            var issues = new List<BlueprintValidationIssue>();
            if (board == null)
            {
                issues.Add(Issue(BlueprintValidationCode.NullBoard, "Blueprint Board state is null."));
                return issues;
            }

            if (board.SaveVersion != BlueprintBoardState.CurrentSaveVersion)
                issues.Add(Issue(BlueprintValidationCode.UnsupportedSaveVersion, $"Blueprint Board save version {board.SaveVersion} is unsupported."));
            if (board.Capacity < 0)
                issues.Add(Issue(BlueprintValidationCode.InvalidCapacity, "Blueprint Capacity cannot be negative."));
            if (board.Slots == null || board.Slots.Count != board.Capacity)
                issues.Add(Issue(BlueprintValidationCode.CapacitySlotMismatch, "Slot count must exactly equal Blueprint Capacity."));

            var activeIds = new HashSet<string>(IdComparer);
            int occupiedSlotCount = 0;
            if (board.Slots != null)
            {
                for (int index = 0; index < board.Slots.Count; index++)
                {
                    var slot = board.Slots[index];
                    if (slot == null)
                    {
                        issues.Add(Issue(BlueprintValidationCode.NullSlot, $"Active slot {index} is null.", boardIndex: index));
                        continue;
                    }
                    if (slot.BoardIndex != index)
                        issues.Add(Issue(BlueprintValidationCode.InvalidBoardIndex, $"Slot {index} stores index {slot.BoardIndex}.", slot.BlueprintDefinitionId, index));
                    if (slot.IsEmpty) continue;
                    occupiedSlotCount++;
                    if (!activeIds.Add(slot.BlueprintDefinitionId))
                        issues.Add(Issue(BlueprintValidationCode.DuplicateActiveBlueprint, $"Blueprint '{slot.BlueprintDefinitionId}' occupies more than one active slot.", slot.BlueprintDefinitionId, index));
                }
            }
            if (occupiedSlotCount > board.Capacity)
                issues.Add(Issue(BlueprintValidationCode.CapacityOverflow,
                    $"Occupied active slot count {occupiedSlotCount} exceeds Blueprint Capacity {board.Capacity}."));

            var benchIds = new HashSet<string>(IdComparer);
            var duplicateBenchIds = new HashSet<string>(IdComparer);
            var activeAndBenchIds = new HashSet<string>(IdComparer);
            if (board.Bench == null)
            {
                issues.Add(Issue(BlueprintValidationCode.BrokenReference, "Blueprint Bench state is null."));
            }
            else
            {
                if (board.Bench.BlueprintDefinitionIds == null)
                {
                    issues.Add(Issue(BlueprintValidationCode.BrokenReference, "Blueprint Bench collection is null."));
                }
                else foreach (string id in board.Bench.BlueprintDefinitionIds)
                {
                    if (string.IsNullOrWhiteSpace(id))
                    {
                        issues.Add(Issue(BlueprintValidationCode.BrokenReference, "Blueprint Bench contains an empty definition ID."));
                        continue;
                    }
                    if (!benchIds.Add(id) && duplicateBenchIds.Add(id))
                        issues.Add(Issue(BlueprintValidationCode.DuplicateBenchBlueprint, $"Blueprint '{id}' appears more than once on the bench.", id));
                    if (activeIds.Contains(id) && activeAndBenchIds.Add(id))
                        issues.Add(Issue(BlueprintValidationCode.ActiveAndBenchDuplicate, $"Blueprint '{id}' is both active and benched.", id));
                }
            }

            var registeredIds = new HashSet<string>(IdComparer);
            if (board.Blueprints == null)
            {
                issues.Add(Issue(BlueprintValidationCode.BrokenReference, "Owned Blueprint collection is null."));
                return issues;
            }

            foreach (var blueprint in board.Blueprints)
            {
                if (blueprint == null)
                {
                    issues.Add(Issue(BlueprintValidationCode.NullBlueprint, "Owned Blueprint collection contains null."));
                    continue;
                }
                string id = blueprint.DefinitionId;
                if (string.IsNullOrWhiteSpace(id)) issues.Add(Issue(BlueprintValidationCode.BrokenReference, "Owned Blueprint has no definition ID."));
                else if (!registeredIds.Add(id)) issues.Add(Issue(BlueprintValidationCode.DuplicateOwnedBlueprint, $"Blueprint '{id}' is registered more than once.", id));
                if (!string.Equals(blueprint.OwnerId, board.OwnerId, StringComparison.Ordinal))
                    issues.Add(Issue(BlueprintValidationCode.OwnerMismatch, $"Blueprint '{id}' does not belong to board owner '{board.OwnerId}'.", id));
                if (resolver != null && !resolver.TryResolve(id, out _))
                    issues.Add(Issue(BlueprintValidationCode.BrokenReference, $"Blueprint definition '{id}' cannot be resolved.", id));

                int actualIndex = board.FindActiveIndex(id);
                bool onBench = benchIds.Contains(id);
                if (blueprint.Location == BlueprintLocationState.Active &&
                    (actualIndex < 0 || actualIndex != blueprint.BlueprintBoardIndex))
                    issues.Add(Issue(BlueprintValidationCode.PlacementMismatch, $"Blueprint '{id}' active placement does not match its slot.", id, blueprint.BlueprintBoardIndex));
                if (blueprint.Location == BlueprintLocationState.Benched && (!onBench || blueprint.BlueprintBoardIndex != -1))
                    issues.Add(Issue(BlueprintValidationCode.PlacementMismatch, $"Blueprint '{id}' benched placement does not match the bench.", id));
            }

            foreach (string id in activeIds.Concat(benchIds).Distinct(IdComparer))
            {
                if (!registeredIds.Contains(id)) issues.Add(Issue(BlueprintValidationCode.BrokenReference, $"Placement references unregistered Blueprint '{id}'.", id));
            }

            return issues;
        }

        public bool IsValid(BlueprintBoardState board) => Validate(board).All(issue => issue.Severity != BlueprintValidationSeverity.Error);

        private static BlueprintValidationIssue Issue(BlueprintValidationCode code, string message, string id = "", int boardIndex = -1) =>
            new(code, BlueprintValidationSeverity.Error, id, boardIndex, message);
    }
}
