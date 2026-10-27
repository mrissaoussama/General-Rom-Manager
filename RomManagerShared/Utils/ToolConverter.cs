using System.Diagnostics;
using RomManagerShared.Configuration;

namespace RomManagerShared.Utils;

/// <summary>
/// A conversion done by a user-supplied program (ADR 0002: never bundled, never in the scan path).
/// Argument tokens: <c>{in}</c> input file, <c>{out}</c> output file, <c>{outdir}</c> output folder.
/// </summary>
public sealed record ConversionTool(string Id, string DisplayName, string Executable, string[] InputExtensions,
    string OutputExtension, string Arguments)
{
    public bool Accepts(string path) => FileMagic.HasExtension(path, InputExtensions);

    /// <summary><paramref name="configured"/> when set, else <c>&lt;BaseDirectory&gt;/Tools/&lt;exe&gt;</c>.</summary>
    public string ResolvePath(string? configured) =>
        !string.IsNullOrWhiteSpace(configured)
            ? configured
            : RomManagerConfiguration.GetPathOrDefault($"Tools:{Id}", Path.Combine("Tools", Executable + (OperatingSystem.IsWindows() ? ".exe" : "")));
}

/// <summary>Known conversions. Originals are kept; the output is written next to the input.</summary>
public static class ConversionTools
{
    public static readonly IReadOnlyList<ConversionTool> All =
    [
        new("chdman-cd", "CHD (chdman, CD)", "chdman", ["cue", "gdi", "toc"], "chd", "createcd -i \"{in}\" -o \"{out}\""),
        new("chdman-dvd", "CHD (chdman, DVD)", "chdman", ["iso"], "chd", "createdvd -i \"{in}\" -o \"{out}\""),
        new("dolphin-rvz", "RVZ (DolphinTool)", "DolphinTool", ["iso", "gcm", "wbfs"], "rvz",
            "convert -i \"{in}\" -o \"{out}\" -f rvz -b 131072 -c zstd -l 5"),
        new("maxcso", "CSO (maxcso)", "maxcso", ["iso"], "cso", "\"{in}\" -o \"{out}\""),
        new("nsz", "NSZ (nsz)", "nsz", ["nsp"], "nsz", "-C -o \"{outdir}\" \"{in}\""),
    ];

    public static ConversionTool? Find(string id) => All.FirstOrDefault(t => t.Id == id);
}

/// <summary>Runs a <see cref="ConversionTool"/> in <c>Cache/Temp/&lt;guid&gt;</c> and moves the result next to the input.</summary>
public static class ToolConverter
{
    /// <summary>Converts <paramref name="input"/>; returns the new file. Throws with the tool's last output on failure.</summary>
    public static async Task<string> ConvertAsync(ConversionTool tool, string exePath, string input, CancellationToken ct = default,
        string? tempRoot = null)
    {
        if (!File.Exists(exePath)) throw new FileNotFoundException($"{tool.DisplayName}: program not found", exePath);
        var final = Path.Combine(Path.GetDirectoryName(input)!, Path.GetFileNameWithoutExtension(input) + "." + tool.OutputExtension);
        if (File.Exists(final)) throw new IOException($"{Path.GetFileName(final)} already exists");

        var temp = Path.Combine(tempRoot ?? Path.Combine(RomManagerConfiguration.GetCachePath(), "Temp"), Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(temp);
        try
        {
            var output = Path.Combine(temp, Path.GetFileName(final));
            var args = tool.Arguments.Replace("{in}", input).Replace("{out}", output).Replace("{outdir}", temp);
            var psi = new ProcessStartInfo(exePath, args)
            {
                WorkingDirectory = temp,
                UseShellExecute = false,
                CreateNoWindow = true,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
            };
            using var process = Process.Start(psi) ?? throw new InvalidOperationException($"{tool.DisplayName} did not start");
            var log = new Queue<string>();
            void Keep(string? line)
            {
                if (string.IsNullOrWhiteSpace(line)) return;
                lock (log)
                {
                    log.Enqueue(line);
                    if (log.Count > 20) log.Dequeue();
                }
            }
            process.OutputDataReceived += (_, e) => Keep(e.Data);
            process.ErrorDataReceived += (_, e) => Keep(e.Data);
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            try
            {
                await process.WaitForExitAsync(ct);
            }
            catch (OperationCanceledException)
            {
                try { process.Kill(entireProcessTree: true); } catch (InvalidOperationException) { }
                throw;
            }

            // Some tools (nsz) choose the output name themselves.
            var produced = File.Exists(output) ? output
                : Directory.EnumerateFiles(temp, "*." + tool.OutputExtension).FirstOrDefault();
            if (process.ExitCode != 0 || produced is null)
            {
                string tail;
                lock (log) tail = string.Join(" | ", log.TakeLast(3));
                throw new InvalidOperationException($"{tool.DisplayName} failed (exit {process.ExitCode}): {tail}");
            }
            File.Move(produced, final);
            return final;
        }
        finally
        {
            try { Directory.Delete(temp, recursive: true); } catch (IOException) { } catch (UnauthorizedAccessException) { }
        }
    }
}
