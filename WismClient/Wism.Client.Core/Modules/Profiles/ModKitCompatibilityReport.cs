using System.Collections.Generic;
using System.Linq;
using Wism.Client.Data.Entities;

namespace Wism.Client.Modules.Profiles
{
    public sealed class ModKitCompatibilityReport
    {
        private readonly List<ModKitCompatibilityIssue> issues = new List<ModKitCompatibilityIssue>();

        public ModKitCompatibilityStatus Status { get; set; } = ModKitCompatibilityStatus.Verified;
        public ModKitSelectionEntity Selection { get; set; }
        public IReadOnlyList<ModKitCompatibilityIssue> Issues => issues;
        public bool IsGreen => Status == ModKitCompatibilityStatus.Verified && !issues.Any(issue => issue.Severity == ModKitValidationSeverity.Error);
        public bool IsLoadable => !issues.Any(issue => issue.Severity == ModKitValidationSeverity.Error);

        public void Add(ModKitValidationSeverity severity, string code, string message, string path)
        {
            issues.Add(new ModKitCompatibilityIssue(severity, code, message, path));
        }
    }
}
