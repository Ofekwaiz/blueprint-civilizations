using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Core;
using BlueprintCivilizations.Content.Definitions;
using UnityEngine;

namespace BlueprintCivilizations.Content.Catalogs
{
    [CreateAssetMenu(menuName = "Blueprint Civilizations/Game Content Catalog", fileName = "GameContentCatalog")]
    public sealed class GameContentCatalog : ScriptableObject
    {
        [SerializeField] private List<ContentDefinition> definitions = new();
        private Dictionary<string, ContentDefinition> byId;

        /// <summary>All authored definitions, including disabled definitions retained for save compatibility.</summary>
        public IReadOnlyList<ContentDefinition> Definitions => definitions.AsReadOnly();

        private void OnEnable() => byId = null;

#if UNITY_EDITOR
        private void OnValidate() => byId = null;
#endif

        /// <summary>Rebuilds the stable-ID index and rejects missing or duplicate entries.</summary>
        public void RebuildIndex()
        {
            byId = new Dictionary<string, ContentDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitions)
            {
                if (definition == null) throw new InvalidOperationException("Content catalog contains a missing definition reference.");
                if (string.IsNullOrWhiteSpace(definition.Id))
                    throw new InvalidOperationException($"Content definition '{definition.name}' has no stable ID.");
                if (!byId.TryAdd(definition.Id, definition))
                    throw new InvalidOperationException($"Duplicate content ID '{definition.Id}'.");
            }
        }

        /// <summary>Resolves an enabled definition by stable ID and expected type.</summary>
        public bool TryGet<T>(string id, out T definition, bool includeDisabled = false) where T : ContentDefinition
        {
            return TryGet(id, out definition, out _, includeDisabled);
        }

        /// <summary>Resolves by stable ID and returns an actionable reason when lookup fails.</summary>
        public bool TryGet<T>(string id, out T definition, out string error, bool includeDisabled = false) where T : ContentDefinition
        {
            if (string.IsNullOrWhiteSpace(id))
            {
                definition = null;
                error = $"A stable content ID is required to resolve {typeof(T).Name}.";
                return false;
            }
            if (byId == null) RebuildIndex();
            if (!byId.TryGetValue(id, out var raw))
            {
                definition = null;
                error = $"No content definition with stable ID '{id}' exists in catalog '{name}'. Rebuild the catalog or restore the asset.";
                return false;
            }
            if (raw is not T typed)
            {
                definition = null;
                error = $"Content ID '{id}' resolves to {raw.GetType().Name}, not {typeof(T).Name}.";
                return false;
            }
            if (!includeDisabled && !typed.IsEnabled)
            {
                definition = null;
                error = $"Content ID '{id}' is disabled. Pass includeDisabled=true only for editor, migration, or compatibility queries.";
                return false;
            }

            definition = typed;
            error = "";
            return true;
        }

        /// <summary>Resolves by stable ID or throws an actionable programmer-facing exception.</summary>
        public T GetRequired<T>(string id, bool includeDisabled = false) where T : ContentDefinition
        {
            if (TryGet(id, out T definition, out string error, includeDisabled)) return definition;
            throw new KeyNotFoundException(error);
        }

        /// <summary>Enumerates units by optional race and tier filters.</summary>
        public IEnumerable<UnitDefinition> GetUnits(RaceDefinition race = null, ContentTier? tier = null, bool includeDisabled = false)
        {
            return definitions.OfType<UnitDefinition>()
                .Where(u => includeDisabled || u.IsEnabled)
                .Where(u => race == null || u.Race == race)
                .Where(u => tier == null || u.Tier == tier.Value);
        }

        /// <summary>Enumerates enabled definitions of a requested type.</summary>
        public IEnumerable<T> GetDefinitions<T>(bool includeDisabled = false) where T : ContentDefinition
        {
            return definitions.OfType<T>().Where(definition => includeDisabled || definition.IsEnabled);
        }

        /// <summary>Filters definitions by authored tags using ordinal, case-insensitive comparison.</summary>
        public IEnumerable<T> GetByTags<T>(IEnumerable<string> tags, bool requireAll = true, bool includeDisabled = false)
            where T : ContentDefinition
        {
            var requested = (tags ?? Enumerable.Empty<string>())
                .Where(tag => !string.IsNullOrWhiteSpace(tag))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();
            var source = GetDefinitions<T>(includeDisabled);
            if (requested.Length == 0) return source;

            return source.Where(definition => requireAll
                ? requested.All(tag => definition.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase))
                : requested.Any(tag => definition.Tags.Contains(tag, StringComparer.OrdinalIgnoreCase)));
        }

#if UNITY_EDITOR
        /// <summary>Replaces catalog contents through editor tooling and invalidates the index.</summary>
        public void EditorSetDefinitions(IEnumerable<ContentDefinition> values)
        {
            definitions = (values ?? Enumerable.Empty<ContentDefinition>())
                .Where(value => value != null)
                .Distinct()
                .OrderBy(value => value.Id, StringComparer.Ordinal)
                .ToList();
            byId = null;
        }
#endif
    }
}
