namespace Wism.Client.Modules.Profiles
{
    public sealed class ModKitCompatibilityIssue
    {
        public ModKitCompatibilityIssue(
            ModKitValidationSeverity severity,
            string code,
            string message,
            string path)
        {
            Severity = severity;
            Code = code;
            Message = message;
            Path = path;
        }

        public ModKitValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string Path { get; }
    }
}
