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

        public IReadOnlyList<ContentDefinition> Definitions => definitions;

        public void RebuildIndex()
        {
            byId = new Dictionary<string, ContentDefinition>(StringComparer.OrdinalIgnoreCase);
            foreach (var definition in definitions.Where(d => d != null))
            {
                if (string.IsNullOrWhiteSpace(definition.Id)) continue;
                if (!byId.TryAdd(definition.Id, definition))
                    throw new InvalidOperationException($"Duplicate content ID '{definition.Id}'.");
            }
        }

        public bool TryGet<T>(string id, out T definition, bool includeDisabled = false) where T : ContentDefinition
        {
            if (byId == null) RebuildIndex();
            if (byId.TryGetValue(id, out var raw) && raw is T typed && (includeDisabled || typed.IsEnabled))
            {
                definition = typed;
                return true;
            }
            definition = null;
            return false;
        }

        public IEnumerable<UnitDefinition> GetUnits(RaceDefinition race = null, ContentTier? tier = null, bool includeDisabled = false)
        {
            return definitions.OfType<UnitDefinition>()
                .Where(u => includeDisabled || u.IsEnabled)
                .Where(u => race == null || u.Race == race)
                .Where(u => tier == null || u.Tier == tier.Value);
        }

#if UNITY_EDITOR
        public void EditorSetDefinitions(IEnumerable<ContentDefinition> values)
        {
            definitions = values.Where(v => v != null).Distinct().ToList();
            byId = null;
        }
#endif
    }
}
