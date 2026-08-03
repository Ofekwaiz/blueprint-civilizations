using System;
using BlueprintCivilizations.Blueprints;
using BlueprintCivilizations.Content.Catalogs;
using BlueprintCivilizations.UI.ViewModels;
using BlueprintCivilizations.UI.Views;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.UI.Presenters
{
    /// <summary>Runtime composition root for attaching the reusable board panel to any UIDocument.</summary>
    public static class BlueprintBoardPanelFactory
    {
        public static BlueprintBoardPanelController Attach(VisualElement host, VisualTreeAsset boardLayout,
            StyleSheet boardStyle, VisualTreeAsset detailsLayout, StyleSheet detailsStyle,
            BlueprintBoardState state, GameContentCatalog catalog, bool enableInteractionDiagnostics = false,
            float dragThreshold = BlueprintBoardInteractionState.DefaultDragThreshold)
        {
            if (host == null) throw new ArgumentNullException(nameof(host));
            if (boardLayout == null) throw new ArgumentNullException(nameof(boardLayout));
            if (boardStyle == null) throw new ArgumentNullException(nameof(boardStyle));
            if (detailsLayout == null) throw new ArgumentNullException(nameof(detailsLayout));
            if (detailsStyle == null) throw new ArgumentNullException(nameof(detailsStyle));
            if (state == null) throw new ArgumentNullException(nameof(state));
            if (catalog == null) throw new ArgumentNullException(nameof(catalog));

            var panelRoot = boardLayout.CloneTree();
            panelRoot.styleSheets.Add(boardStyle);
            host.Add(panelRoot);
            try
            {
                VisualElement detailsHost = panelRoot.Q("blueprint-details-host") ??
                    throw new InvalidOperationException("Blueprint Board UXML is missing required element 'blueprint-details-host'.");
                VisualElement detailsRoot = detailsLayout.CloneTree();
                detailsRoot.styleSheets.Add(detailsStyle);
                detailsHost.Add(detailsRoot);

                var definitionResolver = new ContentCatalogBlueprintDefinitionResolver(catalog);
                var adjacency = new BlueprintAdjacencyService(definitionResolver);
                var placement = new BlueprintPlacementService(state, new BlueprintValidationService(definitionResolver));
                var boardPresenter = new BlueprintBoardPresenter(
                    new BlueprintBoardView(panelRoot, dragThreshold, enableInteractionDiagnostics), placement,
                    adjacency, new ContentCatalogBlueprintBoardPresentationResolver(catalog));
                try
                {
                    var detailsPresenter = new BlueprintDetailsPresenter(new BlueprintDetailsView(detailsRoot), boardPresenter,
                        new ContentCatalogBlueprintDetailsResolver(catalog, adjacency,
                            Debug.isDebugBuild || Application.isEditor));
                    return new BlueprintBoardPanelController(boardPresenter, detailsPresenter);
                }
                catch
                {
                    boardPresenter.Dispose();
                    throw;
                }
            }
            catch
            {
                panelRoot.RemoveFromHierarchy();
                throw;
            }
        }
    }
}
