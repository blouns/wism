using System.Collections.Generic;
using System.Linq;

namespace Wism.Client.Modules.Profiles
{
    public sealed class ModKitValidationReport
    {
        private readonly List<ModKitValidationIssue> issues = new List<ModKitValidationIssue>();

        public IReadOnlyList<ModKitValidationIssue> Issues => issues;

        public bool IsValid => !issues.Any(issue => issue.Severity == ModKitValidationSeverity.Error);

        public void Add(ModKitValidationSeverity severity, string code, string message, string path)
        {
            issues.Add(new ModKitValidationIssue(severity, code, message, path));
        }
    }
}
