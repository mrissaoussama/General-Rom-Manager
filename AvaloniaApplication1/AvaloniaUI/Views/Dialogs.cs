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

    private const int PreviewLimit = 50;

    public FormatNameWindow(IReadOnlyList<Rom> roms, IEnumerable<string> consoleProperties, string consoleName)
    {
        Title = roms.Count == 1 ? "Rename" : $"Rename {roms.Count} ROMs";
        Width = 760;
        SizeToContent = SizeToContent.Height;
        CanResize = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var template = new TextBox { Text = Settings.Instance.RenameFormatString };
        var rows = new ObservableCollection<string>();
        var summary = new TextBlock { Opacity = 0.75 };
        var ok = new Button { Content = "Rename", IsDefault = true };

        // Dry run of the real renamer: shows the final names, including sanitising and de-duplication.
        void Preview()
        {
            rows.Clear();
            try
            {
                var results = FileRenamer.PreviewRename(roms.Take(PreviewLimit), template.Text ?? "");
                foreach (var r in results)
                    rows.Add(r.SourcePath == r.DestinationPath
                        ? $"{Path.GetFileName(r.SourcePath)}   (unchanged)"
                        : $"{Path.GetFileName(r.SourcePath)}  →  {Path.GetFileName(r.DestinationPath)}");
                var changed = results.Count(r => r.SourcePath != r.DestinationPath);
                summary.Text = $"{changed} of {results.Count} file(s) change" + (roms.Count > PreviewLimit ? $" (first {PreviewLimit} shown)" : "");
                ok.IsEnabled = true;
            }
            catch (Exception ex)
            {
                summary.Text = $"Invalid template: {ex.Message}";
                ok.IsEnabled = false;
            }
        }
        template.TextChanged += (_, _) => Preview();

        var previewList = new ListBox
        {
            ItemsSource = rows,
            MaxHeight = 260,
            FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
            FontSize = 11,
        };

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
        root.Children.Add(summary);
        root.Children.Add(previewList);
        root.Children.Add(new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right,
            Children = { cancel, ok },
        });
        Content = root;
        Preview();
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

/// <summary>Lists planned changes (one line each) and asks to go ahead; <see cref="Confirmed"/> holds the answer.</summary>
public sealed class PreviewWindow : Window
{
    public bool Confirmed { get; private set; }

    public PreviewWindow(string title, string summary, IEnumerable<string> lines, string confirmText)
    {
        Title = title;
        Width = 820;
        Height = 480;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        var ok = new Button { Content = confirmText, IsDefault = true };
        var cancel = new Button { Content = "Cancel", IsCancel = true };
        ok.Click += (_, _) => { Confirmed = true; Close(); };
        cancel.Click += (_, _) => Close();

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal, Spacing = 8, HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(0, 10, 0, 0), Children = { cancel, ok },
        };
        var header = new TextBlock { Text = summary, TextWrapping = TextWrapping.Wrap, Margin = new Thickness(0, 0, 0, 8) };
        var list = new ListBox
        {
            ItemsSource = lines.ToList(),
            FontFamily = new FontFamily("Cascadia Mono, Consolas, monospace"),
            FontSize = 11,
        };
        DockPanel.SetDock(header, Dock.Top);
        DockPanel.SetDock(buttons, Dock.Bottom);
        Content = new DockPanel { Margin = new Thickness(16), Children = { header, buttons, list } };
    }
}
