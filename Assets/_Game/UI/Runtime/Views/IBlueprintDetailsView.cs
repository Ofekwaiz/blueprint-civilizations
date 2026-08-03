using System;
using BlueprintCivilizations.UI.ViewModels;

namespace BlueprintCivilizations.UI.Views
{
    /// <summary>Presentation boundary for the Blueprint Details Panel.</summary>
    public interface IBlueprintDetailsView : IDisposable
    {
        void Render(BlueprintDetailsViewModel model);
    }
}
