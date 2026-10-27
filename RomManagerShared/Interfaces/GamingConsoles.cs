namespace RomManagerShared.Interfaces;

/// <summary>
/// Base type for console marker classes. Each console module declares one (e.g. <c>SwitchConsole</c>) with a
/// <see cref="ConsoleInfoAttribute"/>; its class name is the console's id in settings and the library.
/// </summary>
public class GamingConsole
{
    public string Name { get; set; } = "";
    public string Icon { get; set; } = "";
}

/// <summary>How a console is shown: full name, manufacturer (badge colour) and short badge text.</summary>
[AttributeUsage(AttributeTargets.Class, Inherited = false)]
public sealed class ConsoleInfoAttribute(string displayName, string manufacturer, string badge) : Attribute
{
    public string DisplayName { get; } = displayName;
    public string Manufacturer { get; } = manufacturer;
    public string Badge { get; } = badge;
}
