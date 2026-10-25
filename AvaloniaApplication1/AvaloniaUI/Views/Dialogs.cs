namespace AvaloniaUI.Views;

/// <summary>Rename template editor with live preview; <see cref="Result"/> is the template, or null if cancelled.</summary>
public sealed class FormatNameWindow : Window
{
    private static readonly string[] GeneralPlaceholders =
    [
        "{TitleName}", "{TitleID}", "{Region}", "{Version}", "{Publisher}", "{Developer}", "{ReleaseDate}",
        "{Platform}", "{ProductCode}", "{Size}", "{Genres}", "{Players}", "{Type}", "{MinFirmware}", "{DLCCount}",
    ];

    public string? Result { get; private set; }

    public FormatNameWindow(Rom sample, IEnumerable<string> consoleProperties, string consoleName)
    {
        Title = "Rename";
        Width = 600;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        string Preview(string fmt)
        {
            try { return FileRenamer.GenerateFileName([sample], fmt); }
            catch { return "(invalid template)"; }
        }

        var template = new TextBox { Text = Settings.Instance.RenameFormatString };
        var preview = new TextBlock { Opacity = 0.75, TextWrapping = TextWrapping.Wrap, Text = "→ " + Preview(template.Text ?? "") };
        template.TextChanged += (_, _) => preview.Text = "→ " + Preview(template.Text ?? "");

        WrapPanel Chips(IEnumerable<string> placeholders)
        {
            var wrap = new WrapPanel();
            foreach (var ph in placeholders)
            {
                var chip = new Button { Content = ph, Classes = { "small" }, Margin = new Thickness(0, 0, 4, 4) };
                chip.Click += (_, _) =>
                {
                    var pos = template.CaretIndex;
                    template.Text = (template.Text ?? "").Insert(pos, ph);
                    template.CaretIndex = pos + ph.Length;
                };
                wrap.Children.Add(chip);
            }
            return wrap;
        }

        var ok = new Button { Content = "Rename", IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        ok.Click += (_, _) => { Result = template.Text; Close(); };
        cancel.Click += (_, _) => Close();

        var root = new StackPanel { Margin = new Thickness(16), Spacing = 8 };
        root.Children.Add(new TextBlock { Text = "Template", FontWeight = FontWeight.SemiBold });
        root.Children.Add(template);
        root.Children.Add(new TextBlock { Text = "General", Opacity = 0.7 });
        root.Children.Add(Chips(GeneralPlaceholders));
        var specific = consoleProperties.Take(12).Select(p => "{" + p + "}").ToList();
        if (specific.Count > 0)
        {
            root.Children.Add(new TextBlock { Text = consoleName, Opacity = 0.7 });
            root.Children.Add(Chips(specific));
        }
        root.Children.Add(preview);
        root.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancel, ok },
        });
        Content = root;
    }
}

/// <summary>Read-only list of every property of a rom.</summary>
public sealed class PropertiesWindow : Window
{
    public PropertiesWindow(RomViewModel rom)
    {
        Title = $"Properties — {rom.Title}";
        Width = 560;
        Height = 520;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var list = new StackPanel { Margin = new Thickness(12), Spacing = 2 };
        foreach (var kv in rom.Properties)
        {
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("140,*") };
            row.Children.Add(new TextBlock { Text = kv.Key, FontWeight = FontWeight.SemiBold, TextTrimming = TextTrimming.CharacterEllipsis });
            var value = new SelectableTextBlock { Text = kv.Value, TextWrapping = TextWrapping.Wrap };
            Grid.SetColumn(value, 1);
            row.Children.Add(value);
            list.Children.Add(row);
        }
        Content = new ScrollViewer { Content = list };
    }
}
