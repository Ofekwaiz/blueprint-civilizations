using System;
using System.Collections.Generic;
using BlueprintCivilizations.Content.Definitions;

namespace BlueprintCivilizations.Content.Validation
{
    /// <summary>Definitions and diagnostics services available during validation.</summary>
    public sealed class ValidationContext
    {
        public ValidationContext(
            IEnumerable<ContentDefinition> allDefinitions,
            Func<ContentDefinition, string> assetPathResolver = null)
        {
            AllDefinitions = allDefinitions ?? Array.Empty<ContentDefinition>();
            AssetPathResolver = assetPathResolver;
        }

        public IEnumerable<ContentDefinition> AllDefinitions { get; }
        public Func<ContentDefinition, string> AssetPathResolver { get; }

        public string GetAssetPath(ContentDefinition definition) => AssetPathResolver?.Invoke(definition) ?? "";
    }
}
