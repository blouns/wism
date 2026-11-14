using System;
using System.IO;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;

namespace Wism.Client.Data
{
    public sealed class SaveCompatibilityException : IOException
    {
        public SaveCompatibilityException(string message) : base(message) { }
    }

    public sealed class SaveFormatMetadata
    {
        public string Format { get; set; }
        public int SchemaVersion { get; set; }
        public int MinimumReaderVersion { get; set; }
        public string ProducerVersion { get; set; }
    }

    /// <summary>Validates the save envelope before any live game or scene changes.</summary>
    public static class SaveFileFormat
    {
        public const int CurrentVersion = 1;
        public const string FormatName = "WISM.Save";

        public static SaveFormatMetadata CreateMetadata(string producerVersion)
        {
            return new SaveFormatMetadata
            {
                Format = FormatName,
                SchemaVersion = CurrentVersion,
                MinimumReaderVersion = CurrentVersion,
                ProducerVersion = producerVersion ?? "unknown"
            };
        }

        public static JObject Read(string json)
        {
            var root = JObject.Parse(json, new JsonLoadSettings
            {
                DuplicatePropertyNameHandling = DuplicatePropertyNameHandling.Error
            });
            foreach (var property in root.Properties())
                if (string.Equals(property.Name, "SaveFormat", StringComparison.OrdinalIgnoreCase) && property.Name != "SaveFormat")
                    throw new SaveCompatibilityException("This save has an incorrectly named format header.");
            var metadata = root["SaveFormat"];
            if (metadata == null)
            {
                // Version zero used the same payload, without an envelope or AI display options.
                // Never manufacture missing campaign state or rewrite the caller's source file.
                if (!(root["WismGameEntity"] is JObject))
                    throw new SaveCompatibilityException("This legacy save is missing its campaign data.");
                if (root["ShowAiCombat"] == null) root["ShowAiCombat"] = true;
                if (root["ObserveAiMovement"] == null) root["ObserveAiMovement"] = true;
                root["SaveFormat"] = JObject.FromObject(CreateMetadata("legacy-unversioned"));
                return root;
            }

            if (!(metadata is JObject header) || header["Format"]?.Type != JTokenType.String ||
                (string)header["Format"] != FormatName)
                throw new SaveCompatibilityException("This save has an invalid or unrecognized format header.");
            int schema = ReadVersion(header, "SchemaVersion");
            int minimum = ReadVersion(header, "MinimumReaderVersion");
            if (minimum > schema)
                throw new SaveCompatibilityException("This save has inconsistent compatibility versions.");
            if (schema > CurrentVersion || minimum > CurrentVersion)
                throw new SaveCompatibilityException("This save requires a newer WISM save reader. Update the game to load it; the save has not been changed.");
            if (!(root["WismGameEntity"] is JObject))
                throw new SaveCompatibilityException("This save is missing its campaign data.");
            return root;
        }

        private static int ReadVersion(JObject header, string name)
        {
            var token = header[name];
            if (token?.Type != JTokenType.Integer || !int.TryParse(token.ToString(), out int version) || version < 1)
                throw new SaveCompatibilityException("This save has an invalid compatibility version.");
            return version;
        }
    }
}
