using System.Collections;
using Avalonia.Media.Imaging;
using RomManagerShared.Base;
using RomManagerShared.Utils;

namespace AvaloniaUI;

public static class DynamicRomPropertyUI
{
    // ── Property discovery ────────────────────────────────────────────────

    public static IEnumerable<PropertyInfo> GetVisibleProperties(Type type)
    {
        return type.GetProperties()
            .Where(p => p.GetCustomAttribute<UiPropertyAttribute>()?.Visible ?? false)
            .OrderBy(p => p.GetCustomAttribute<UiPropertyAttribute>()?.Order ?? 999);
    }

    // ── Main control factory ──────────────────────────────────────────────

    /// <summary>
    /// Creates the appropriate Avalonia control for a given ROM property.
    /// Uses the <see cref="UiPropertyAttribute"/> to decide the rendering strategy.
    /// Falls back to auto-detection when the attribute is absent.
    /// </summary>
    public static Control CreateControlForProperty(PropertyInfo property, object romInstance)
    {
        var attr   = property.GetCustomAttribute<UiPropertyAttribute>();
        var value  = property.GetValue(romInstance);
        var pType  = attr?.PropertyType ?? DeterminePropertyType(property.PropertyType);

        return pType switch
        {
            UiPropertyType.Image    => CreateImageControl(value),
            UiPropertyType.FileSize => new TextBlock { Text = FileUtils.FormatFileSize((long?)value ?? 0) },
            UiPropertyType.List     => CreateListControl(value as IEnumerable),
            UiPropertyType.Region   => new TextBlock { Text = FormatEnumerable(value) },
            UiPropertyType.Language => new TextBlock { Text = FormatEnumerable(value) },
            UiPropertyType.Boolean  => new CheckBox { IsChecked = value as bool?, IsEnabled = false },
            _                       => new TextBlock { Text = value?.ToString() ?? "N/A" }
        };
    }

    // ── Private helpers ───────────────────────────────────────────────────

    private static Control CreateImageControl(object? value)
    {
        if (value is string imagePath && !string.IsNullOrEmpty(imagePath) && File.Exists(imagePath))
        {
            try
            {
                return new Image
                {
                    Source = new Bitmap(imagePath),
                    Width  = 100,
                    Height = 100
                };
            }
            catch { /* fall through */ }
        }
        return new TextBlock { Text = "No image" };
    }

    private static Control CreateListControl(IEnumerable? items)
    {
        if (items is null) return new TextBlock { Text = "—" };

        var listBox = new ListBox { MaxHeight = 120 };
        foreach (var item in items)
            listBox.Items.Add(item?.ToString() ?? string.Empty);

        return listBox.Items.Count == 0 ? new TextBlock { Text = "—" } : listBox;
    }

    private static string FormatEnumerable(object? value)
    {
        if (value is IEnumerable items and not string)
            return string.Join(", ", items.Cast<object>().Select(i => i?.ToString() ?? "?"));
        return value?.ToString() ?? "—";
    }

    private static UiPropertyType DeterminePropertyType(Type type)
    {
        if (type == typeof(string))                return UiPropertyType.Text;
        if (type == typeof(long) || type == typeof(long?)) return UiPropertyType.FileSize;
        if (type == typeof(int)  || type == typeof(double)) return UiPropertyType.Number;
        if (type == typeof(DateTime) || type == typeof(DateOnly)) return UiPropertyType.Date;
        if (type == typeof(bool) || type == typeof(bool?)) return UiPropertyType.Boolean;
        if (typeof(IEnumerable).IsAssignableFrom(type) && type != typeof(string)) return UiPropertyType.List;
        return UiPropertyType.Text;
    }
}
