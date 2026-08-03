using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Core;

namespace BlueprintCivilizations.Blueprints
{
    public sealed class BlueprintAdjacentPair
    {
        internal BlueprintAdjacentPair(BlueprintState left, BlueprintState right) { Left = left; Right = right; }
        public BlueprintState Left { get; }
        public BlueprintState Right { get; }
    }

    /// <summary>Read-only relationship queries over active slots. Empty slots break immediate adjacency.</summary>
    public sealed class BlueprintAdjacencyService
    {
        private readonly IBlueprintDefinitionResolver resolver;
        public BlueprintAdjacencyService(IBlueprintDefinitionResolver resolver = null) => this.resolver = resolver;

        public BlueprintState GetLeftNeighbor(BlueprintBoardState board, string definitionId) => GetAtOffset(board, definitionId, -1);
        public BlueprintState GetRightNeighbor(BlueprintBoardState board, string definitionId) => GetAtOffset(board, definitionId, 1);
        public BlueprintAdjacentPair GetAdjacentPair(BlueprintBoardState board, string definitionId) =>
            new(GetLeftNeighbor(board, definitionId), GetRightNeighbor(board, definitionId));

        public IReadOnlyList<BlueprintState> GetBlueprintsLeftOf(BlueprintBoardState board, string definitionId) => GetSide(board, definitionId, left: true);
        public IReadOnlyList<BlueprintState> GetBlueprintsRightOf(BlueprintBoardState board, string definitionId) => GetSide(board, definitionId, left: false);

        public IReadOnlyList<BlueprintState> GetMatchingTags(BlueprintBoardState board, IEnumerable<string> tags, bool requireAll = false)
        {
            RequireResolver();
            string[] requested = (tags ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value))
                .Distinct(StringComparer.OrdinalIgnoreCase).ToArray();
            if (requested.Length == 0) return Array.Empty<BlueprintState>();
            return GetActive(board).Where(state => resolver.TryResolve(state.DefinitionId, out var metadata) &&
                (requireAll
                    ? requested.All(tag => metadata.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                    : requested.Any(tag => metadata.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)))).ToArray();
        }

        public IReadOnlyList<BlueprintState> GetMatchingRace(BlueprintBoardState board, string raceId)
        {
            RequireResolver();
            if (string.IsNullOrWhiteSpace(raceId)) return Array.Empty<BlueprintState>();
            return GetActive(board).Where(state => resolver.TryResolve(state.DefinitionId, out var metadata) &&
                string.Equals(metadata.RaceId, raceId, StringComparison.OrdinalIgnoreCase)).ToArray();
        }

        public IReadOnlyList<BlueprintState> GetMatchingTier(BlueprintBoardState board, ContentTier tier)
        {
            RequireResolver();
            return GetActive(board).Where(state => resolver.TryResolve(state.DefinitionId, out var metadata) && metadata.Tier == tier).ToArray();
        }

        public IReadOnlyList<BlueprintState> GetMatchingTagsOf(BlueprintBoardState board, string definitionId, bool requireAll = false)
        {
            RequireResolver();
            return resolver.TryResolve(definitionId, out var source)
                ? GetMatchingTags(board, source.Tags, requireAll)
                : Array.Empty<BlueprintState>();
        }

        public IReadOnlyList<BlueprintState> GetMatchingRaceOf(BlueprintBoardState board, string definitionId)
        {
            RequireResolver();
            return resolver.TryResolve(definitionId, out var source)
                ? GetMatchingRace(board, source.RaceId)
                : Array.Empty<BlueprintState>();
        }

        public IReadOnlyList<BlueprintState> GetMatchingTierOf(BlueprintBoardState board, string definitionId)
        {
            RequireResolver();
            return resolver.TryResolve(definitionId, out var source)
                ? GetMatchingTier(board, source.Tier)
                : Array.Empty<BlueprintState>();
        }

        private static BlueprintState GetAtOffset(BlueprintBoardState board, string id, int offset)
        {
            if (board == null) return null;
            int index = board.FindActiveIndex(id);
            int neighborIndex = index + offset;
            if (index < 0 || neighborIndex < 0 || neighborIndex >= board.Slots.Count || board.Slots[neighborIndex].IsEmpty) return null;
            return board.FindBlueprint(board.Slots[neighborIndex].BlueprintDefinitionId);
        }

        private static IReadOnlyList<BlueprintState> GetSide(BlueprintBoardState board, string id, bool left)
        {
            if (board == null) return Array.Empty<BlueprintState>();
            int sourceIndex = board.FindActiveIndex(id);
            if (sourceIndex < 0) return Array.Empty<BlueprintState>();
            var values = new List<BlueprintState>();
            int start = left ? 0 : sourceIndex + 1;
            int end = left ? sourceIndex : board.Slots.Count;
            for (int index = start; index < end; index++)
            {
                if (board.Slots[index].IsEmpty) continue;
                var state = board.FindBlueprint(board.Slots[index].BlueprintDefinitionId);
                if (state != null) values.Add(state);
            }
            return values;
        }

        private static IEnumerable<BlueprintState> GetActive(BlueprintBoardState board)
        {
            if (board == null) yield break;
            foreach (var slot in board.Slots)
            {
                if (slot == null || slot.IsEmpty) continue;
                var state = board.FindBlueprint(slot.BlueprintDefinitionId);
                if (state != null) yield return state;
            }
        }

        private void RequireResolver()
        {
            if (resolver == null) throw new InvalidOperationException("Tag, race, and tier queries require an IBlueprintDefinitionResolver.");
        }
    }
}
