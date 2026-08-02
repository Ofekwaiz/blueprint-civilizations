using System.Linq;
using BlueprintCivilizations.Content.Validation;
using BlueprintCivilizations.Editor.ContentStudio;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;

namespace BlueprintCivilizations.Editor
{
    /// <summary>Blocks player builds when authored content contains errors or critical issues.</summary>
    public sealed class ContentBuildValidator : IPreprocessBuildWithReport
    {
        public int callbackOrder => 0;

        public void OnPreprocessBuild(BuildReport report)
        {
            var definitions = ContentTools.GetAllDefinitions();
            var blocking = ContentValidator.ValidateAll(definitions, AssetDatabase.GetAssetPath)
                .Where(issue => issue.Severity is ValidationSeverity.Error or ValidationSeverity.Critical)
                .ToList();
            if (blocking.Count == 0) return;

            string summary = string.Join("\n", blocking.Take(10).Select(issue => issue.ToString()));
            throw new BuildFailedException($"Content validation failed with {blocking.Count} blocking issue(s).\n{summary}");
        }
    }
}
