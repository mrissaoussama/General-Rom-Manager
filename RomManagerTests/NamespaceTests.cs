using System.Runtime.CompilerServices;
using System.Text.RegularExpressions;
using AwesomeAssertions;

namespace RomManagerTests;

public partial class NamespaceTests
{
    [GeneratedRegex(@"^namespace\s+([\w.]+)", RegexOptions.Multiline)]
    private static partial Regex Namespace();

    // Namespaces follow folders in RomManagerShared (vendored DotNet3dsToolkit code keeps its own).
    [Fact]
    public void RomManagerShared_NamespacesMatchFolders() => Check();

    private static void Check([CallerFilePath] string caller = "")
    {
        var root = Path.GetFullPath(Path.Combine(Path.GetDirectoryName(caller)!, "..", "RomManagerShared"));
        var wrong = Directory.EnumerateFiles(root, "*.cs", SearchOption.AllDirectories)
            .Where(f => !f.Split(Path.DirectorySeparatorChar).Any(p => p is "bin" or "obj" or "Migrations"))
            .Select(f => (File: f, Ns: Namespace().Match(File.ReadAllText(f)).Groups[1].Value))
            .Where(x => x.Ns.Length > 0 && !x.Ns.StartsWith("DotNet3dsToolkit", StringComparison.Ordinal))
            .Where(x =>
            {
                var rel = Path.GetRelativePath(root, Path.GetDirectoryName(x.File)!);
                var expected = "RomManagerShared" + (rel == "." ? "" : "." + rel.Replace(Path.DirectorySeparatorChar, '.'));
                return x.Ns != expected;
            })
            .Select(x => $"{Path.GetRelativePath(root, x.File)}: {x.Ns}")
            .ToList();

        wrong.Should().BeEmpty();
    }
}
