namespace BlueprintCivilizations.Blueprints
{
    public abstract class BlueprintCommand
    {
        protected BlueprintCommand(string playerId = "", long sequenceNumber = 0, int? expectedRevision = null)
        {
            PlayerId = playerId ?? "";
            SequenceNumber = sequenceNumber;
            ExpectedRevision = expectedRevision;
        }

        public string PlayerId { get; }
        public long SequenceNumber { get; }
        public int? ExpectedRevision { get; }
    }

    public sealed class ActivateBlueprintCommand : BlueprintCommand
    {
        public ActivateBlueprintCommand(string definitionId, int targetIndex = -1, string playerId = "", long sequenceNumber = 0, int? expectedRevision = null)
            : base(playerId, sequenceNumber, expectedRevision) { DefinitionId = definitionId ?? ""; TargetIndex = targetIndex; }
        public string DefinitionId { get; }
        public int TargetIndex { get; }
    }

    public sealed class BenchBlueprintCommand : BlueprintCommand
    {
        public BenchBlueprintCommand(string definitionId, string playerId = "", long sequenceNumber = 0, int? expectedRevision = null)
            : base(playerId, sequenceNumber, expectedRevision) { DefinitionId = definitionId ?? ""; }
        public string DefinitionId { get; }
    }

    public sealed class MoveBlueprintCommand : BlueprintCommand
    {
        public MoveBlueprintCommand(string definitionId, int targetIndex, string playerId = "", long sequenceNumber = 0, int? expectedRevision = null)
            : base(playerId, sequenceNumber, expectedRevision) { DefinitionId = definitionId ?? ""; TargetIndex = targetIndex; }
        public string DefinitionId { get; }
        public int TargetIndex { get; }
    }

    public sealed class SwapBlueprintsCommand : BlueprintCommand
    {
        public SwapBlueprintsCommand(int firstIndex, int secondIndex, string playerId = "", long sequenceNumber = 0, int? expectedRevision = null)
            : base(playerId, sequenceNumber, expectedRevision) { FirstIndex = firstIndex; SecondIndex = secondIndex; }
        public int FirstIndex { get; }
        public int SecondIndex { get; }
    }

    public sealed class ReorderBlueprintsCommand : BlueprintCommand
    {
        public ReorderBlueprintsCommand(string definitionId, int targetIndex, string playerId = "", long sequenceNumber = 0, int? expectedRevision = null)
            : base(playerId, sequenceNumber, expectedRevision) { DefinitionId = definitionId ?? ""; TargetIndex = targetIndex; }
        public string DefinitionId { get; }
        public int TargetIndex { get; }
    }

    public sealed class SetBlueprintCapacityCommand : BlueprintCommand
    {
        public SetBlueprintCapacityCommand(int capacity, string playerId = "", long sequenceNumber = 0, int? expectedRevision = null)
            : base(playerId, sequenceNumber, expectedRevision) { Capacity = capacity; }
        public int Capacity { get; }
    }

    /// <summary>Concise factories for callers that prefer command names without constructors.</summary>
    public static class BlueprintCommands
    {
        public static ActivateBlueprintCommand ActivateBlueprint(string id, int targetIndex = -1) => new(id, targetIndex);
        public static BenchBlueprintCommand BenchBlueprint(string id) => new(id);
        public static MoveBlueprintCommand MoveBlueprint(string id, int targetIndex) => new(id, targetIndex);
        public static SwapBlueprintsCommand SwapBlueprints(int firstIndex, int secondIndex) => new(firstIndex, secondIndex);
        public static ReorderBlueprintsCommand ReorderBlueprints(string id, int targetIndex) => new(id, targetIndex);
    }
}
