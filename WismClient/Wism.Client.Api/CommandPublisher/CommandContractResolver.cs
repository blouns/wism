using Newtonsoft.Json;
using Newtonsoft.Json.Serialization;
using System;
using System.Reflection;

public class CommandContractResolver : DefaultContractResolver
{
    protected override JsonProperty CreateProperty(MemberInfo member, MemberSerialization memberSerialization)
    {
        var property = base.CreateProperty(member, memberSerialization);

        // Exclude properties marked with [JsonIgnore]
        if (Attribute.IsDefined(member, typeof(JsonIgnoreAttribute)))
        {
            property.ShouldSerialize = _ => false;
            return property;
        }

        // Exclude specific property names (common pitfalls)
        string[] excludedProperties = { "GameState", "Map", "Logger", "ServiceProvider" };
        if (Array.Exists(excludedProperties, name => name == property.PropertyName))
        {
            property.ShouldSerialize = _ => false;
            return property;
        }

        // Always include public properties
        if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
        {
            // Handle nested objects (e.g., Position, Hero)
            property.ObjectCreationHandling = ObjectCreationHandling.Auto;
        }

        return property;
    }
}