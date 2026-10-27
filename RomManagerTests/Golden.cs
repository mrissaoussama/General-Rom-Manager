using System.Runtime.CompilerServices;
using System.Text.Json;
using System.Text.Json.Nodes;
using AwesomeAssertions;
using RomManagerShared.Base;
using RomManagerShared.Base.Library;

namespace RomManagerTests;

/// <summary>
/// Golden files: a parser's whole output (every field, as stored in the library) compared with
/// <c>RomManagerTests/Golden/&lt;name&gt;.json</c>. A missing file is written and the test passes, except on CI.
/// After an intended parser change, run the tests with <c>UPDATE_GOLDEN=1</c> and review the diff.
/// </summary>
public static class Golden
{
    private const string Placeholder = "<fixture>";

    public static void Verify(string name, IEnumerable<Rom> roms, string fixtureDir, [CallerFilePath] string caller = "")
    {
        var actual = Normalize(RomSnapshot.Serialize(roms), fixtureDir);
        var path = Path.Combine(Path.GetDirectoryName(caller)!, "Golden", name + ".json");

        if (!File.Exists(path))
        {
            Environment.GetEnvironmentVariable("CI").Should().BeNull("golden file {0} is missing: run the tests locally and commit it", path);
            Directory.CreateDirectory(Path.GetDirectoryName(path)!);
            File.WriteAllText(path, actual);
            return;
        }
        if (Environment.GetEnvironmentVariable("UPDATE_GOLDEN") == "1")
        {
            File.WriteAllText(path, actual);
            return;
        }

        actual.Should().Be(File.ReadAllText(path).ReplaceLineEndings("\n"),
            "the parser output for {0} changed; if intended, rerun with UPDATE_GOLDEN=1 and review the diff", name);
    }

    // Indented, machine-independent: the fixture folder becomes <fixture>, separators become '/'.
    private static string Normalize(string snapshot, string fixtureDir)
    {
        var node = JsonNode.Parse(snapshot)!;
        Rewrite(node, Path.TrimEndingDirectorySeparator(Path.GetFullPath(fixtureDir)));
        return node.ToJsonString(new JsonSerializerOptions { WriteIndented = true, Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping })
            .ReplaceLineEndings("\n") + "\n";
    }

    private static void Rewrite(JsonNode? node, string dir)
    {
        switch (node)
        {
            case JsonObject obj:
                foreach (var key in obj.Select(kv => kv.Key).ToList())
                {
                    if (obj[key] is JsonValue v && v.TryGetValue<string>(out var s)) obj[key] = Fix(s, dir);
                    else Rewrite(obj[key], dir);
                }
                break;
            case JsonArray arr:
                for (int i = 0; i < arr.Count; i++)
                {
                    if (arr[i] is JsonValue v && v.TryGetValue<string>(out var s)) arr[i] = Fix(s, dir);
                    else Rewrite(arr[i], dir);
                }
                break;
        }
    }

    private static string Fix(string value, string dir) =>
        value.StartsWith(dir, StringComparison.OrdinalIgnoreCase)
            ? Placeholder + value[dir.Length..].Replace('\\', '/')
            : value;
}
