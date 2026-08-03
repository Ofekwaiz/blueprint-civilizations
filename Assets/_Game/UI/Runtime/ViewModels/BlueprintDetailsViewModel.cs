using System;
using System.Collections.Generic;
using UnityEngine;

namespace BlueprintCivilizations.UI.ViewModels
{
    /// <summary>One read-only labeled value in a Blueprint Details section.</summary>
    public sealed class BlueprintDetailsValueViewModel
    {
        public BlueprintDetailsValueViewModel(string label, string value, string tooltip = "")
        {
            Label = label ?? "";
            Value = value ?? "";
            Tooltip = tooltip ?? "";
        }

        public string Label { get; }
        public string Value { get; }
        public string Tooltip { get; }
    }

    /// <summary>One future modifier contribution to a calculated current statistic.</summary>
    public sealed class BlueprintStatModifierViewModel
    {
        public BlueprintStatModifierViewModel(string source, string value)
        {
            Source = source ?? "";
            Value = value ?? "";
        }

        public string Source { get; }
        public string Value { get; }
    }

    /// <summary>Authored base value, calculated current value, and optional inspectable modifiers.</summary>
    public sealed class BlueprintStatViewModel
    {
        public BlueprintStatViewModel(string label, string baseValue, string currentValue, string unit,
            IEnumerable<BlueprintStatModifierViewModel> modifiers = null, string tooltip = "")
        {
            Label = label ?? "";
            BaseValue = baseValue ?? "";
            CurrentValue = currentValue ?? "";
            Unit = unit ?? "";
            Modifiers = new List<BlueprintStatModifierViewModel>(modifiers ?? Array.Empty<BlueprintStatModifierViewModel>()).AsReadOnly();
            Tooltip = tooltip ?? "";
        }

        public string Label { get; }
        public string BaseValue { get; }
        public string CurrentValue { get; }
        public string Unit { get; }
        public IReadOnlyList<BlueprintStatModifierViewModel> Modifiers { get; }
        public string Tooltip { get; }
    }

    /// <summary>One labeled Details Panel section containing ordinary values and base/current statistics.</summary>
    public sealed class BlueprintDetailsSectionViewModel
    {
        public BlueprintDetailsSectionViewModel(string heading, IEnumerable<BlueprintDetailsValueViewModel> values,
            IEnumerable<BlueprintStatViewModel> stats = null)
        {
            Heading = heading ?? "";
            Values = new List<BlueprintDetailsValueViewModel>(values ?? Array.Empty<BlueprintDetailsValueViewModel>()).AsReadOnly();
            Stats = new List<BlueprintStatViewModel>(stats ?? Array.Empty<BlueprintStatViewModel>()).AsReadOnly();
        }

        public string Heading { get; }
        public IReadOnlyList<BlueprintDetailsValueViewModel> Values { get; }
        public IReadOnlyList<BlueprintStatViewModel> Stats { get; }
    }

    /// <summary>Complete immutable projection rendered by the modular Blueprint Details view.</summary>
    public sealed class BlueprintDetailsViewModel
    {
        public const string DefaultEmptyMessage = "Select a Blueprint to inspect its production and combat profile.";

        public BlueprintDetailsViewModel(bool isEmpty, string emptyMessage, Sprite icon, string displayName,
            string definitionId, bool showDeveloperId, string diagnostic,
            IEnumerable<BlueprintDetailsSectionViewModel> sections)
        {
            IsEmpty = isEmpty;
            EmptyMessage = emptyMessage ?? DefaultEmptyMessage;
            Icon = icon;
            DisplayName = displayName ?? "";
            DefinitionId = definitionId ?? "";
            ShowDeveloperId = showDeveloperId;
            Diagnostic = diagnostic ?? "";
            Sections = new List<BlueprintDetailsSectionViewModel>(sections ?? Array.Empty<BlueprintDetailsSectionViewModel>()).AsReadOnly();
        }

        public bool IsEmpty { get; }
        public string EmptyMessage { get; }
        public Sprite Icon { get; }
        public string DisplayName { get; }
        public string DefinitionId { get; }
        public bool ShowDeveloperId { get; }
        public string Diagnostic { get; }
        public IReadOnlyList<BlueprintDetailsSectionViewModel> Sections { get; }

        public static BlueprintDetailsViewModel Empty(string message = DefaultEmptyMessage) =>
            new(true, message, null, "", "", false, "", Array.Empty<BlueprintDetailsSectionViewModel>());

        public static BlueprintDetailsViewModel Error(string definitionId, string diagnostic, bool showDeveloperId) =>
            new(false, DefaultEmptyMessage, null, "Blueprint unavailable", definitionId, showDeveloperId,
                diagnostic, Array.Empty<BlueprintDetailsSectionViewModel>());
    }
}
