using Newtonsoft.Json;

namespace Wism.Client.Data
{
    public static class Cloner
    {
        public static T Clone<T>(T source)
        {
            if (ReferenceEquals(source, null))
                return default(T);

            var settings = new JsonSerializerSettings { ContractResolver = new JsonContractResolver() };

            return JsonConvert.DeserializeObject<T>(JsonConvert.SerializeObject(source, settings), settings);
        }
    }
}
