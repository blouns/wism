using System.Collections.Generic;
using System.Linq;

namespace Wism.Client.Core.Validation
{
    public sealed class WorldValidationReport
    {
        public WorldValidationReport(IEnumerable<WorldValidationIssue> issues)
        {
            this.Issues = issues == null
                ? new List<WorldValidationIssue>()
                : new List<WorldValidationIssue>(issues);
        }

        public IReadOnlyList<WorldValidationIssue> Issues { get; }

        public bool IsValid => this.Issues.Count == 0;

        public string Summary => this.IsValid
            ? "World validation passed."
            : string.Join("; ", this.Issues.Select(issue => issue.ToString()));
    }
}
