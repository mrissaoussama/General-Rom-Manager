using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using RomManagerShared.Utils;

namespace AvaloniaUI.Views;

/// <summary>
/// Scrollable in-app log viewer.
/// Shows the last 500 lines captured by <see cref="FileUtils.Log"/> 
/// and allows opening the full log file.
/// Open via Tools › View Log…
/// </summary>
public sealed class LogWindow : Window
{
    public LogWindow()
    {
        Title = "Application Log";
        Width = 780;
        Height = 500;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Content = BuildContent();
    }

    private Control BuildContent()
    {
        var root = new DockPanel { LastChildFill = true };

        // ── Toolbar ───────────────────────────────────────────────────────
        var toolbar = new DockPanel { Margin = new Thickness(8, 6) };

        var titleLabel = new TextBlock
        {
            Text = "Recent log messages (last 500 lines)",
            VerticalAlignment = VerticalAlignment.Center,
            Opacity = 0.7,
            FontSize = 12,
        };
        DockPanel.SetDock(titleLabel, Dock.Left);
        toolbar.Children.Add(titleLabel);

        var openFileBtn = new Button
        {
            Content = "Open Log File…",
            Padding = new Thickness(8, 3),
            HorizontalAlignment = HorizontalAlignment.Right,
            Margin = new Thickness(4, 0, 0, 0),
        };
        openFileBtn.Click += (_, _) =>
        {
            try { System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo(FileUtils.LogFilePath) { UseShellExecute = true }); }
            catch { /* ignore if file doesn't exist yet */ }
        };
        DockPanel.SetDock(openFileBtn, Dock.Right);
        toolbar.Children.Add(openFileBtn);

        var refreshBtn = new Button
        {
            Content = "↻ Refresh",
            Padding = new Thickness(8, 3),
            Margin = new Thickness(0, 0, 4, 0),
        };
        DockPanel.SetDock(refreshBtn, Dock.Right);
        toolbar.Children.Add(refreshBtn);

        var toolbarBorder = new Border
        {
            Child = toolbar,
            BorderThickness = new Thickness(0, 0, 0, 1),
        };
        DockPanel.SetDock(toolbarBorder, Dock.Top);
        root.Children.Add(toolbarBorder);

        // ── Log text box ──────────────────────────────────────────────────
        var logBox = new TextBox
        {
            IsReadOnly = true,
            AcceptsReturn = true,
            TextWrapping = TextWrapping.NoWrap,
            FontFamily = new FontFamily("Cascadia Mono,Consolas,Monospace"),
            FontSize = 11,
            Margin = new Thickness(0),
            BorderThickness = new Thickness(0),
            Background = Avalonia.Media.Brushes.Transparent,
        };

        void LoadLogs()
        {
            var lines = FileUtils.GetRecentLogs();
            logBox.Text = lines.Count > 0
                ? string.Join(Environment.NewLine, lines)
                : "(No log entries yet — log messages appear here after ROM scanning.)";
            // Scroll to end
            logBox.CaretIndex = logBox.Text.Length;
        }

        LoadLogs();

        refreshBtn.Click += (_, _) => LoadLogs();

        var scroll = new ScrollViewer
        {
            Content = logBox,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        };

        root.Children.Add(scroll);

        return root;
    }
}
