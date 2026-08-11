using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using Wism.Client.Data;
using Wism.Client.Data.Entities;

namespace Wism.Client.Core.Telemetry
{
    public sealed class GameStateFingerprint
    {
        private readonly IReadOnlyDictionary<string, string> fields;

        private GameStateFingerprint(string hash, int canonicalByteCount, IReadOnlyDictionary<string, string> fields)
        {
            this.Hash = hash;
            this.CanonicalByteCount = canonicalByteCount;
            this.fields = fields;
        }

        public string Hash { get; }

        public int CanonicalByteCount { get; }

        public int FieldCount => this.fields.Count;

        public static GameStateFingerprint Capture(Game game)
        {
            if (game is null)
            {
                throw new ArgumentNullException(nameof(game));
            }

            return From(GamePersistance.SnapshotGame(game));
        }

        public static GameStateFingerprint From(GameEntity snapshot)
        {
            if (snapshot is null)
            {
                throw new ArgumentNullException(nameof(snapshot));
            }

            var settings = new JsonSerializerSettings
            {
                ContractResolver = new JsonContractResolver(),
                Formatting = Formatting.None,
                NullValueHandling = NullValueHandling.Include
            };
            var token = JToken.Parse(JsonConvert.SerializeObject(snapshot, settings));
            if (token is JObject root)
            {
                root.Remove(nameof(GameEntity.Timestamp));
            }

            var canonical = Sort(token).ToString(Formatting.None);
            var fields = new SortedDictionary<string, string>(StringComparer.Ordinal);
            Flatten(token, "$", fields);

            return new GameStateFingerprint(
                ComputeHash(canonical),
                Encoding.UTF8.GetByteCount(canonical),
                fields);
        }

        public static GameStateDivergence LocateFirstDivergence(
            int commandIndex,
            GameStateFingerprint expected,
            GameStateFingerprint actual)
        {
            if (commandIndex < 0)
            {
                throw new ArgumentOutOfRangeException(nameof(commandIndex));
            }

            if (expected is null)
            {
                throw new ArgumentNullException(nameof(expected));
            }

            if (actual is null)
            {
                throw new ArgumentNullException(nameof(actual));
            }

            if (string.Equals(expected.Hash, actual.Hash, StringComparison.Ordinal))
            {
                return null;
            }

            var paths = expected.fields.Keys
                .Concat(actual.fields.Keys)
                .Distinct(StringComparer.Ordinal)
                .OrderBy(path => path, StringComparer.Ordinal);
            foreach (var path in paths)
            {
                expected.fields.TryGetValue(path, out var expectedValue);
                actual.fields.TryGetValue(path, out var actualValue);
                if (!string.Equals(expectedValue, actualValue, StringComparison.Ordinal))
                {
                    return new GameStateDivergence(
                        commandIndex,
                        path,
                        expectedValue ?? "<missing>",
                        actualValue ?? "<missing>");
                }
            }

            return new GameStateDivergence(commandIndex, "$", expected.Hash, actual.Hash);
        }

        private static JToken Sort(JToken token)
        {
            if (token is JObject obj)
            {
                return new JObject(obj.Properties()
                    .OrderBy(property => property.Name, StringComparer.Ordinal)
                    .Select(property => new JProperty(property.Name, Sort(property.Value))));
            }

            if (token is JArray array)
            {
                return new JArray(array.Select(Sort));
            }

            return token.DeepClone();
        }

        private static void Flatten(JToken token, string path, IDictionary<string, string> fields)
        {
            if (token is JObject obj)
            {
                fields[$"{path}#object"] = obj.Count.ToString();
                foreach (var property in obj.Properties().OrderBy(property => property.Name, StringComparer.Ordinal))
                {
                    Flatten(property.Value, $"{path}.{property.Name}", fields);
                }

                return;
            }

            if (token is JArray array)
            {
                fields[$"{path}#array"] = array.Count.ToString();
                for (var i = 0; i < array.Count; i++)
                {
                    Flatten(array[i], $"{path}[{i}]", fields);
                }

                return;
            }

            fields[path] = token.ToString(Formatting.None);
        }

        private static string ComputeHash(string canonical)
        {
            using (var sha = SHA256.Create())
            {
                var bytes = sha.ComputeHash(Encoding.UTF8.GetBytes(canonical));
                var result = new StringBuilder(bytes.Length * 2);
                foreach (var value in bytes)
                {
                    result.Append(value.ToString("x2"));
                }

                return result.ToString();
            }
        }
    }

    public sealed class GameStateDivergence
    {
        public GameStateDivergence(int commandIndex, string path, string expected, string actual)
        {
            this.CommandIndex = commandIndex;
            this.Path = path;
            this.Expected = expected;
            this.Actual = actual;
        }

        public int CommandIndex { get; }

        public string Path { get; }

        public string Expected { get; }

        public string Actual { get; }
    }
}
