using System;
using BlueprintCivilizations.Blueprints;
using BlueprintCivilizations.UI.Views;

namespace BlueprintCivilizations.UI.Presenters
{
    /// <summary>Keeps the Details Panel synchronized with the board presenter's single stable selection.</summary>
    public sealed class BlueprintDetailsPresenter : IDisposable
    {
        private readonly IBlueprintDetailsView view;
        private readonly BlueprintBoardPresenter boardPresenter;
        private readonly IBlueprintDetailsResolver resolver;
        private bool disposed;

        public BlueprintDetailsPresenter(IBlueprintDetailsView view, BlueprintBoardPresenter boardPresenter,
            IBlueprintDetailsResolver resolver)
        {
            this.view = view ?? throw new ArgumentNullException(nameof(view));
            this.boardPresenter = boardPresenter ?? throw new ArgumentNullException(nameof(boardPresenter));
            this.resolver = resolver ?? throw new ArgumentNullException(nameof(resolver));
            boardPresenter.SelectionChanged += OnSelectionChanged;
            boardPresenter.Placement.EventRaised += OnPlacementChanged;
            Refresh();
        }

        public void Refresh()
        {
            BlueprintBoardState board = boardPresenter.Placement.State;
            BlueprintState selected = string.IsNullOrWhiteSpace(boardPresenter.SelectedBlueprintId)
                ? null
                : board.FindBlueprint(boardPresenter.SelectedBlueprintId);
            view.Render(resolver.Resolve(selected, board));
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            boardPresenter.SelectionChanged -= OnSelectionChanged;
            boardPresenter.Placement.EventRaised -= OnPlacementChanged;
            view.Dispose();
        }

        private void OnSelectionChanged(string _) => Refresh();
        private void OnPlacementChanged(BlueprintEvent _) => Refresh();
    }
}
