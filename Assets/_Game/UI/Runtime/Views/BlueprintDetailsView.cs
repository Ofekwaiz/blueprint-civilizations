using System;
using BlueprintCivilizations.UI.ViewModels;
using UnityEngine;
using UnityEngine.UIElements;

namespace BlueprintCivilizations.UI.Views
{
    /// <summary>UI Toolkit renderer for resolved Blueprint details. It contains no definition or statistic logic.</summary>
    public sealed class BlueprintDetailsView : IBlueprintDetailsView
    {
        private readonly VisualElement root;
        private readonly VisualElement emptyState;
        private readonly Label emptyMessage;
        private readonly VisualElement content;
        private readonly Image icon;
        private readonly Label iconFallback;
        private readonly Label displayName;
        private readonly Label developerId;
        private readonly Label diagnostic;
        private readonly VisualElement sections;
        private bool disposed;

        public BlueprintDetailsView(VisualElement root)
        {
            this.root = root ?? throw new ArgumentNullException(nameof(root));
            emptyState = Require<VisualElement>("blueprint-details-empty");
            emptyMessage = Require<Label>("blueprint-details-empty-message");
            content = Require<VisualElement>("blueprint-details-content");
            icon = Require<Image>("blueprint-details-icon");
            iconFallback = Require<Label>("blueprint-details-icon-fallback");
            displayName = Require<Label>("blueprint-details-name");
            developerId = Require<Label>("blueprint-details-developer-id");
            diagnostic = Require<Label>("blueprint-details-diagnostic");
            sections = Require<VisualElement>("blueprint-details-sections");
        }

        public void Render(BlueprintDetailsViewModel model)
        {
            if (disposed) throw new ObjectDisposedException(nameof(BlueprintDetailsView));
            if (model == null) throw new ArgumentNullException(nameof(model));

            emptyState.EnableInClassList("blueprint-details--hidden", !model.IsEmpty);
            content.EnableInClassList("blueprint-details--hidden", model.IsEmpty);
            emptyMessage.text = model.EmptyMessage;
            if (model.IsEmpty)
            {
                sections.Clear();
                return;
            }

            displayName.text = model.DisplayName;
            developerId.text = $"Definition ID: {model.DefinitionId}";
            developerId.EnableInClassList("blueprint-details--hidden", !model.ShowDeveloperId);
            diagnostic.text = model.Diagnostic;
            diagnostic.EnableInClassList("blueprint-details--hidden", string.IsNullOrWhiteSpace(model.Diagnostic));
            icon.sprite = model.Icon;
            icon.EnableInClassList("blueprint-details--hidden", model.Icon == null);
            iconFallback.EnableInClassList("blueprint-details--hidden", model.Icon != null);

            sections.Clear();
            foreach (BlueprintDetailsSectionViewModel section in model.Sections)
            {
                var sectionElement = new VisualElement();
                sectionElement.AddToClassList("blueprint-details-section");
                var heading = new Label(section.Heading);
                heading.AddToClassList("blueprint-details-section-heading");
                sectionElement.Add(heading);

                foreach (BlueprintDetailsValueViewModel value in section.Values)
                    sectionElement.Add(CreateValueRow(value));
                foreach (BlueprintStatViewModel stat in section.Stats)
                    sectionElement.Add(CreateStatRow(stat));
                sections.Add(sectionElement);
            }
        }

        public void Dispose()
        {
            if (disposed) return;
            disposed = true;
            sections.Clear();
        }

        private static VisualElement CreateValueRow(BlueprintDetailsValueViewModel model)
        {
            var row = new VisualElement { tooltip = model.Tooltip };
            row.AddToClassList("blueprint-details-value-row");
            var label = new Label(model.Label);
            label.AddToClassList("blueprint-details-value-label");
            var value = new Label(model.Value);
            value.AddToClassList("blueprint-details-value");
            row.Add(label);
            row.Add(value);
            return row;
        }

        private static VisualElement CreateStatRow(BlueprintStatViewModel model)
        {
            var row = new VisualElement { tooltip = BuildStatTooltip(model) };
            row.AddToClassList("blueprint-details-stat-row");

            var label = new Label(model.Label);
            label.AddToClassList("blueprint-details-stat-label");
            row.Add(label);

            var values = new VisualElement();
            values.AddToClassList("blueprint-details-stat-values");
            var baseValue = new Label($"Base  {WithUnit(model.BaseValue, model.Unit)}");
            baseValue.AddToClassList("blueprint-details-stat-base");
            var currentValue = new Label($"Current  {WithUnit(model.CurrentValue, model.Unit)}");
            currentValue.AddToClassList("blueprint-details-stat-current");
            values.Add(baseValue);
            values.Add(currentValue);
            row.Add(values);
            return row;
        }

        private static string BuildStatTooltip(BlueprintStatViewModel model)
        {
            string tooltip = string.IsNullOrWhiteSpace(model.Tooltip)
                ? $"Current {model.Label}: {WithUnit(model.CurrentValue, model.Unit)}\nBase: {WithUnit(model.BaseValue, model.Unit)}"
                : model.Tooltip;
            foreach (BlueprintStatModifierViewModel modifier in model.Modifiers)
                tooltip += $"\n{modifier.Source}: {modifier.Value}";
            return tooltip;
        }

        private static string WithUnit(string value, string unit) =>
            string.IsNullOrWhiteSpace(unit) ? value : $"{value} {unit}";

        private T Require<T>(string name) where T : VisualElement =>
            root.Q<T>(name) ?? throw new InvalidOperationException($"Blueprint Details UXML is missing required element '{name}'.");
    }
}
