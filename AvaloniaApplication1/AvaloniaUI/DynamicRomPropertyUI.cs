using System.Collections;
using Avalonia.Media.Imaging;

namespace AvaloniaUI;

public static class DynamicRomPropertyUI
{
    public static IEnumerable<PropertyInfo> GetVisibleProperties(Type type)
    {
        return type.GetProperties()
            .Where(p => p.GetCustomAttribute<UiPropertyAttribute>()?.Visible ?? false)
            .OrderBy(p => p.GetCustomAttribute<UiPropertyAttribute>()?.Order ?? 999);
    }

    public static Control CreateControlForProperty(PropertyInfo property, Rom rom)
    {
        var value = property.GetValue(rom);
        var attr = property.GetCustomAttribute<UiPropertyAttribute>();

        switch (attr?.PropertyType)
        {
            case UiPropertyType.Image:
                if (value is string imagePath && !string.IsNullOrEmpty(imagePath))
                    try
                    {
                        if (File.Exists(imagePath))
                        {
                            var image = new Image
                            {
                                Source = new Bitmap(imagePath),
                                Width = 100,
                                Height = 100
                            };
                            return image;
                        }
                    }
                    catch
                    {
                    }

                return new TextBlock { Text = "No image" };

            case UiPropertyType.List:
                if (value is IEnumerable<object> list)
                    return new TextBlock
                    {
                        Text = string.Join(", ", list.Select(i => i.ToString()))
                    };
                return new TextBlock { Text = "Empty list" };

            default:
                return new TextBlock { Text = value?.ToString() ?? "N/A" };
        }
    }


    public static Control CreateControlForProperty(PropertyInfo prop, object romInstance)
    {
        var attr = prop.GetCustomAttribute<UiPropertyAttribute>();
        var value = prop.GetValue(romInstance);
        var propType = attr?.PropertyType ?? DeterminePropertyType(prop.PropertyType);

        // Create appropriate control based on property type
        switch (propType)
        {
            case UiPropertyType.Text:
                return new TextBox { Text = value?.ToString() };
            case UiPropertyType.FileSize:
                return new TextBlock { Text = FileUtils.FormatFileSize((long?)value ?? 0) };
            // case UiPropertyType.List:
            //     return CreateListControl(value as IEnumerable<>);
            // Add cases for other types
            default:
                return new TextBlock { Text = value?.ToString() ?? "null" };
        }
    }

    private static UiPropertyType DeterminePropertyType(Type type)
    {
        // Auto-detect appropriate UI type based on property type
        if (type == typeof(string)) return UiPropertyType.Text;
        if (type == typeof(int) || type == typeof(double)) return UiPropertyType.Number;
        if (type == typeof(DateTime)) return UiPropertyType.Date;
        // Add more type mappings
        return UiPropertyType.Text;
    }

    private static Control CreateListControl(IEnumerable items)
    {
        // Create a control to display lists
        var listBox = new ListBox();
        if (items != null)
            foreach (var item in items)
                listBox.Items.Add(item.ToString());
        return listBox;
    }
}