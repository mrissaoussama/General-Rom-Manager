using System.Diagnostics;

namespace RomManagerShared.Utils;

/// <summary>Starts a user-configured emulator for a rom. The emulator is user-supplied (ADR 0002).</summary>
public static class EmulatorLauncher
{
    public const string DefaultArguments = "\"{rom}\"";

    /// <summary>
    /// Arguments template tokens: <c>{rom}</c> full path, <c>{dir}</c> its folder, <c>{name}</c> file name
    /// without extension. Quote tokens in the template where the emulator expects quoted paths.
    /// </summary>
    public static ProcessStartInfo BuildStartInfo(string emulatorPath, string? argumentsTemplate, string romPath)
    {
        var args = (string.IsNullOrWhiteSpace(argumentsTemplate) ? DefaultArguments : argumentsTemplate)
            .Replace("{rom}", romPath)
            .Replace("{dir}", Path.GetDirectoryName(romPath) ?? "")
            .Replace("{name}", Path.GetFileNameWithoutExtension(romPath));
        return new ProcessStartInfo(emulatorPath, args)
        {
            UseShellExecute = false,
            WorkingDirectory = Path.GetDirectoryName(emulatorPath) ?? "",
        };
    }

    public static void Launch(string emulatorPath, string? argumentsTemplate, string romPath)
    {
        if (!File.Exists(emulatorPath))
            throw new FileNotFoundException("Emulator not found", emulatorPath);
        using var _ = Process.Start(BuildStartInfo(emulatorPath, argumentsTemplate, romPath));
    }
}
