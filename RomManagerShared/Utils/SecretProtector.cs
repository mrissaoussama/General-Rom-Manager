using System.Security.Cryptography;
using System.Text;

namespace RomManagerShared.Utils;

/// <summary>
/// Encrypts plugin secrets (API keys) at rest. Windows: DPAPI for the current user, stored as <c>dpapi:&lt;base64&gt;</c>.
/// Other systems have no built-in equivalent; values stay as they are and the settings file is made readable by the
/// user only (see <see cref="RestrictToUser"/>).
/// </summary>
public static class SecretProtector
{
    private const string Prefix = "dpapi:";
    private static readonly byte[] Entropy = "RomManager plugin secret"u8.ToArray();

    public static string Protect(string value)
    {
        if (value.Length == 0 || !OperatingSystem.IsWindows()) return value;
        return Prefix + Convert.ToBase64String(ProtectedData.Protect(Encoding.UTF8.GetBytes(value), Entropy, DataProtectionScope.CurrentUser));
    }

    /// <summary>The plain value; empty when it was encrypted for another user or machine.</summary>
    public static string Unprotect(string stored)
    {
        if (!stored.StartsWith(Prefix, StringComparison.Ordinal)) return stored;
        if (!OperatingSystem.IsWindows()) return "";
        try
        {
            return Encoding.UTF8.GetString(ProtectedData.Unprotect(Convert.FromBase64String(stored[Prefix.Length..]), Entropy, DataProtectionScope.CurrentUser));
        }
        catch (Exception ex) when (ex is CryptographicException or FormatException)
        {
            AppLog.Warn($"[Plugins] a saved secret cannot be decrypted (another user or machine?): {ex.Message}");
            return "";
        }
    }

    /// <summary>On Linux and macOS: owner read/write only (0600).</summary>
    public static void RestrictToUser(string file)
    {
        if (OperatingSystem.IsWindows()) return;
        try { File.SetUnixFileMode(file, UnixFileMode.UserRead | UnixFileMode.UserWrite); }
        catch (Exception ex) { AppLog.Warn($"[Plugins] cannot restrict {file}: {ex.Message}"); }
    }
}
