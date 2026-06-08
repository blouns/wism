namespace Wism.Client.Modules.Worlds
{
    public sealed class WorldKitValidationIssue
    {
        public WorldKitValidationIssue(
            WorldKitValidationSeverity severity,
            string code,
            string message,
            string path,
            string jsonPath = "",
            int? x = null,
            int? y = null)
        {
            Severity = severity;
            Code = code;
            Message = message;
            Path = path;
            JsonPath = jsonPath ?? string.Empty;
            X = x;
            Y = y;
        }

        public WorldKitValidationSeverity Severity { get; }
        public string Code { get; }
        public string Message { get; }
        public string Path { get; }
        public string JsonPath { get; }
        public int? X { get; }
        public int? Y { get; }

        public override string ToString()
        {
            var coordinate = X.HasValue && Y.HasValue ? $" @{X},{Y}" : string.Empty;
            var json = string.IsNullOrWhiteSpace(JsonPath) ? string.Empty : $" {JsonPath}";
            return $"{Severity} {Code}: {Message}{coordinate}{json} ({Path})";
        }
    }
}
