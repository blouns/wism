namespace Wism.Client.Core.Validation
{
    public sealed class WorldValidationIssue
    {
        public WorldValidationIssue(string code, string message, int? x = null, int? y = null)
        {
            this.Code = code;
            this.Message = message;
            this.X = x;
            this.Y = y;
        }

        public string Code { get; }

        public string Message { get; }

        public int? X { get; }

        public int? Y { get; }

        public override string ToString()
        {
            return this.X.HasValue && this.Y.HasValue
                ? $"{this.Code}@{this.X},{this.Y}: {this.Message}"
                : $"{this.Code}: {this.Message}";
        }
    }
}
