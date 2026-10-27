using RomManagerShared.Configuration;
using System.IO;

namespace RomManagerShared.PSVita.Configuration;

/// <summary>
/// Provides PS Vita specific configuration values
/// </summary>
public static class PSVitaConfiguration
{
    #region Path Configuration
    /// <summary>
    /// Gets the configured ROM path for PS Vita
    /// Creates directory if it doesn't exist
    /// </summary>
    public static string GetRomPath()
    {
        var path = RomManagerConfiguration.GetConsoleRomPath("PSVita");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Gets the configured license path for PS Vita
    /// Creates directory if it doesn't exist
    /// </summary>
    public static string GetLicensePath()
    {
        var path = Path.Combine(
            RomManagerConfiguration.GetConsoleRomPath("PSVita"),
            "Licenses"
        );
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Gets the path to the param.sfo tool DLL
    /// </summary>
    public static string GetParamSfoToolPath()
    {
        return RomManagerConfiguration.GetConsoleToolPath("PSVita", "ParamSfo");
    }
    #endregion

    #region Utility Methods
    /// <summary>
    /// Gets the path for organizing Vita ROMs
    /// </summary>
    public static string GetOrganizedRomPath()
    {
        var path = Path.Combine(GetRomPath(), "Organized");
        Directory.CreateDirectory(path);
        return path;
    }

    /// <summary>
    /// Gets the path for Vita tools
    /// </summary>
    public static string GetToolsPath()
    {
        var path = Path.Combine(GetRomPath(), "Tools");
        Directory.CreateDirectory(path);
        return path;
    }
    #endregion
}