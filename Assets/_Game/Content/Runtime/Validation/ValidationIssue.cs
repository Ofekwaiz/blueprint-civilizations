namespace BlueprintCivilizations.Content.Validation
{
    /// <summary>Severity of an authored-content validation finding.</summary>
    public enum ValidationSeverity { Info, Warning, Error, Critical }

    /// <summary>Actionable validation result suitable for tools, builds, and tests.</summary>
    public readonly struct ValidationIssue
    {
        public ValidationSeverity Severity { get; }
        public string DefinitionId { get; }
        public string AssetPath { get; }
        public string FieldName { get; }
        public string Message { get; }
        public string SuggestedFix { get; }

        public ValidationIssue(
            ValidationSeverity severity,
            string definitionId,
            string assetPath,
            string fieldName,
            string message,
            string suggestedFix)
        {
            Severity = severity;
            DefinitionId = definitionId ?? "";
            AssetPath = assetPath ?? "";
            FieldName = fieldName ?? "";
            Message = message;
            SuggestedFix = suggestedFix ?? "";
        }

        public override string ToString()
        {
            string field = string.IsNullOrWhiteSpace(FieldName) ? "" : $" Field: {FieldName}.";
            string path = string.IsNullOrWhiteSpace(AssetPath) ? "" : $" Asset: {AssetPath}.";
            string fix = string.IsNullOrWhiteSpace(SuggestedFix) ? "" : $" Suggested fix: {SuggestedFix}";
            return $"[{Severity}] {DefinitionId}: {Message}{field}{path}{fix}";
        }
    }
}
