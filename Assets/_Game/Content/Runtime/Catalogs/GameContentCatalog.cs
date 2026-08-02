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
            if (string.IsNullOrWhiteSpace(id))
            {
                definition = null;
                return false;
            }
            if (byId == null) RebuildIndex();
            if (byId.TryGetValue(id, out var raw) && raw is T typed && (includeDisabled || typed.IsEnabled))
            {
                definition = typed;
                return true;
            }
            definition = null;
            return false;
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

#if UNITY_EDITOR
        /// <summary>Replaces catalog contents through editor tooling and invalidates the index.</summary>
        public void EditorSetDefinitions(IEnumerable<ContentDefinition> values)
        {
            definitions = (values ?? Enumerable.Empty<ContentDefinition>()).Where(v => v != null).Distinct().ToList();
            byId = null;
        }
#endif
    }
}
