using System;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.Content.Definitions;
using BlueprintCivilizations.UI.ViewModels;

namespace BlueprintCivilizations.UI.Presenters
{
    public interface IBlueprintBoardPresentationResolver
    {
        bool TryResolve(string definitionId, out BlueprintCardViewModel card);
    }

    public sealed class ContentCatalogBlueprintBoardPresentationResolver : IBlueprintBoardPresentationResolver
    {
        private readonly GameContentCatalog catalog;
        public ContentCatalogBlueprintBoardPresentationResolver(GameContentCatalog catalog) =>
            this.catalog = catalog ?? throw new ArgumentNullException(nameof(catalog));

        public bool TryResolve(string definitionId, out BlueprintCardViewModel card)
        {
            if (!catalog.TryGet<ContentDefinition>(definitionId, out var definition, includeDisabled: true))
            {
                card = new BlueprintCardViewModel(definitionId, definitionId, $"Broken content reference: {definitionId}", null);
                return false;
            }

            string type = definition switch
            {
                UnitDefinition => "Unit Blueprint",
                StructureDefinition => "Structure Blueprint",
                _ => "Blueprint"
            };
            string tooltip = $"{definition.DisplayName}\n{type}\nID: {definition.Id}\n{definition.Description}";
            card = new BlueprintCardViewModel(definition.Id, definition.DisplayName, tooltip, definition.Icon);
            return definition is UnitDefinition or StructureDefinition;
        }
    }
}
