using System;
using BlueprintCivilizations.UI.ViewModels;

namespace BlueprintCivilizations.UI.Views
{
    /// <summary>Small presentation boundary used by the Blueprint Board presenter and its tests.</summary>
    public interface IBlueprintBoardView : IDisposable
    {
        event Action<string> SelectionRequested;
        event Action<string> BenchRequested;
        event Action<string, int> ReorderRequested;
        event Action<BlueprintBoardDropRequest> DropPreviewRequested;
        event Action<BlueprintBoardDropRequest> DropRequested;
        event Action UndoRequested;
        event Action RedoRequested;

        void Render(BlueprintBoardViewModel model);
        void SetSelectionState(string selectedBlueprintId, System.Collections.Generic.IEnumerable<string> adjacentBlueprintIds);
        void ShowDropPreview(BlueprintBoardDropRequest request, bool isValid, string message);
        void ShowStatus(string message, bool isError);
        void LogInteractionDiagnostic(string message);
    }
}
