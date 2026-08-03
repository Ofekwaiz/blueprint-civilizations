using System;

namespace BlueprintCivilizations.Blueprints
{
    public enum BlueprintEventType { Activated, Benched, Moved, Swapped, Reordered, CapacityChanged, UndoCompleted, RedoCompleted }

    [Serializable]
    public sealed class BlueprintEvent
    {
        internal BlueprintEvent(BlueprintEventType type, string blueprintId, string secondaryBlueprintId, int fromIndex, int toIndex, int revision)
        {
            Type = type;
            BlueprintId = blueprintId ?? "";
            SecondaryBlueprintId = secondaryBlueprintId ?? "";
            FromIndex = fromIndex;
            ToIndex = toIndex;
            Revision = revision;
        }

        public BlueprintEventType Type { get; }
        public string BlueprintId { get; }
        public string SecondaryBlueprintId { get; }
        public int FromIndex { get; }
        public int ToIndex { get; }
        public int Revision { get; }
    }

    public static class BlueprintEvents
    {
        internal static BlueprintEvent Create(BlueprintEventType type, string id, int from, int to, int revision, string secondaryId = "") =>
            new(type, id, secondaryId, from, to, revision);
    }
}
