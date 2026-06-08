using System.Collections.Generic;
using System.Linq;

namespace Wism.Client.Modules.Worlds
{
    public sealed class WorldKitValidationReport
    {
        private readonly List<WorldKitValidationIssue> issues = new List<WorldKitValidationIssue>();

        public int SchemaVersion { get; set; } = 1;
        public string Status => IsValid ? "Passed" : "Failed";
        public bool IsValid => !issues.Any(issue => issue.Severity == WorldKitValidationSeverity.Error);
        public string WorldId { get; set; } = string.Empty;
        public string ModRoot { get; set; } = string.Empty;
        public string WorldRoot { get; set; } = string.Empty;
        public WorldKitValidationCoverage Coverage { get; } = new WorldKitValidationCoverage();
        public IReadOnlyList<WorldKitValidationIssue> Issues => issues;
        public int IssueCount => issues.Count;
        public string[] ProofHints { get; set; } = new string[0];

        public void Add(
            WorldKitValidationSeverity severity,
            string code,
            string message,
            string path,
            string jsonPath = "",
            int? x = null,
            int? y = null)
        {
            issues.Add(new WorldKitValidationIssue(severity, code, message, path, jsonPath, x, y));
        }
    }
}
