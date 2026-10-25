using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.Json.Serialization.Metadata;

namespace RomManagerShared.Base.Library;

/// <summary>Serializes roms of any concrete type, keeping the runtime type.</summary>
public static class RomSnapshot
{
    private sealed record Entry(string Type, JsonElement Data);

    private static readonly JsonSerializerOptions Options = new()
    {
        ReferenceHandler = ReferenceHandler.IgnoreCycles,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        TypeInfoResolver = new DefaultJsonTypeInfoResolver { Modifiers = { IncludeNonPublicSetters } },
    };

    public static string Serialize(IEnumerable<Rom> roms)
    {
        var entries = roms.Select(rom => new Entry(
            $"{rom.GetType().FullName}, {rom.GetType().Assembly.GetName().Name}",
            JsonSerializer.SerializeToElement(rom, rom.GetType(), Options)));
        return JsonSerializer.Serialize(entries, Options);
    }

    public static List<Rom> Deserialize(string json)
    {
        var entries = JsonSerializer.Deserialize<List<Entry>>(json, Options) ?? [];
        var roms = new List<Rom>(entries.Count);
        foreach (var entry in entries)
        {
            var type = Type.GetType(entry.Type) ?? ConsoleModules.FindType(entry.Type.Split(',')[0]);
            if (type is null || !typeof(Rom).IsAssignableFrom(type))
                type = typeof(Rom);
            if (entry.Data.Deserialize(type, Options) is Rom rom)
                roms.Add(rom);
        }
        return roms;
    }

    // Several rom properties use internal/private setters.
    private static void IncludeNonPublicSetters(JsonTypeInfo typeInfo)
    {
        if (typeInfo.Kind != JsonTypeInfoKind.Object) return;
        foreach (var property in typeInfo.Properties)
        {
            if (property.Set is not null || property.AttributeProvider is not PropertyInfo info) continue;
            var setter = info.GetSetMethod(nonPublic: true);
            if (setter is not null)
                property.Set = (obj, value) => setter.Invoke(obj, [value]);
        }
    }
}
