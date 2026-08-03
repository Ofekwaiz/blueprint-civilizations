using System.Collections.Generic;

namespace BlueprintCivilizations.Blueprints
{
    public enum BlueprintCommandFailure
    {
        None,
        InvalidCommand,
        InvalidBoardState,
        MissingBlueprint,
        OwnerMismatch,
        AlreadyActive,
        NotActive,
        CapacityExceeded,
        InvalidIndex,
        OccupiedSlot,
        EmptySlot,
        InvalidSwap,
        StaleRevision,
        NothingToUndo,
        NothingToRedo
    }

    public sealed class BlueprintCommandResult
    {
        private BlueprintCommandResult(bool success, BlueprintCommandFailure failure, string message,
            IReadOnlyList<BlueprintEvent> events, IReadOnlyList<BlueprintValidationIssue> validationIssues)
        {
            Success = success;
            Failure = failure;
            Message = message ?? "";
            Events = events ?? System.Array.Empty<BlueprintEvent>();
            ValidationIssues = validationIssues ?? System.Array.Empty<BlueprintValidationIssue>();
        }

        public bool Success { get; }
        public BlueprintCommandFailure Failure { get; }
        public string Message { get; }
        public IReadOnlyList<BlueprintEvent> Events { get; }
        public IReadOnlyList<BlueprintValidationIssue> ValidationIssues { get; }

        internal static BlueprintCommandResult Succeeded(BlueprintEvent blueprintEvent) =>
            new(true, BlueprintCommandFailure.None, "", new[] { blueprintEvent }, System.Array.Empty<BlueprintValidationIssue>());

        internal static BlueprintCommandResult Failed(BlueprintCommandFailure failure, string message,
            IReadOnlyList<BlueprintValidationIssue> issues = null) =>
            new(false, failure, message, System.Array.Empty<BlueprintEvent>(), issues);
    }
}
