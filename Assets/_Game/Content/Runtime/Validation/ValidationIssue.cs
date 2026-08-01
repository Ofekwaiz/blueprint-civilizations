namespace BlueprintCivilizations.Content.Validation
{
    public enum ValidationSeverity { Info, Warning, Error }

    public readonly struct ValidationIssue
    {
        public ValidationSeverity Severity { get; }
        public string Message { get; }
        public ValidationIssue(ValidationSeverity severity, string message)
        {
            Severity = severity;
            Message = message;
        }
        public override string ToString() => $"[{Severity}] {Message}";
    }
}
