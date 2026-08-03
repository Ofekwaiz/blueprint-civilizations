using System;
using System.Collections.Generic;
using System.Linq;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.Core;

namespace BlueprintCivilizations.Blueprints
{
    public sealed class BlueprintDefinitionMetadata
    {
        public BlueprintDefinitionMetadata(string definitionId, string raceId, ContentTier tier, IEnumerable<string> tags)
        {
            DefinitionId = definitionId ?? "";
            RaceId = raceId ?? "";
            Tier = tier;
            Tags = (tags ?? Enumerable.Empty<string>()).Where(value => !string.IsNullOrWhiteSpace(value)).ToArray();
        }

        public string DefinitionId { get; }
        public string RaceId { get; }
        public ContentTier Tier { get; }
        public IReadOnlyList<string> Tags { get; }
    }

    public interface IBlueprintDefinitionResolver
    {
        bool TryResolve(string definitionId, out BlueprintDefinitionMetadata metadata);
    }

    /// <summary>Read-only adapter from authored content to relationship metadata.</summary>
    public sealed class ContentCatalogBlueprintDefinitionResolver : IBlueprintDefinitionResolver
    {
        private readonly GameContentCatalog catalog;

        public ContentCatalogBlueprintDefinitionResolver(GameContentCatalog catalog) =>
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

        public bool TryResolve(string definitionId, out BlueprintDefinitionMetadata metadata)
        {
            metadata = null;
            if (!catalog.TryGet<ContentDefinition>(definitionId, out var definition, includeDisabled: true)) return false;

            switch (definition)
            {
                case UnitDefinition unit:
                    metadata = new BlueprintDefinitionMetadata(unit.Id, unit.Race?.Id, unit.Tier, unit.Tags);
                    return true;
                case StructureDefinition structure:
                    metadata = new BlueprintDefinitionMetadata(structure.Id, structure.Race?.Id, structure.Tier, structure.Tags);
                    return true;
                default:
                    return false;
            }
        }
    }
}
