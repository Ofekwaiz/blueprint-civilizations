using System;
using BlueprintCivilizations.Blueprints;

namespace BlueprintCivilizations.UI.Presenters
{
    /// <summary>Disposable runtime panel composition containing the Board and Details presenters.</summary>
    public sealed class BlueprintBoardPanelController : IDisposable
    {
        private bool disposed;

        public BlueprintBoardPanelController(BlueprintBoardPresenter board, BlueprintDetailsPresenter details)
        {
            Board = board ?? throw new ArgumentNullException(nameof(board));
            Details = details ?? throw new ArgumentNullException(nameof(details));
        }

        public BlueprintBoardPresenter Board { get; }
        public BlueprintDetailsPresenter Details { get; }
        public BlueprintPlacementService Placement => Board.Placement;
        public string SelectedBlueprintId => Board.SelectedBlueprintId;

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            Details.Dispose();
            Board.Dispose();
        }
    }
}
