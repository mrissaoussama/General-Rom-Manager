using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;
using AvaloniaUI.ViewModels;

namespace AvaloniaUI.Views;

/// <summary>
/// Modal dialog showing the download queue with progress, speed, and cancel buttons.
/// Open via Tools › Downloads…
/// </summary>
public sealed class DownloadManagerWindow : Window
{
    private readonly DownloadManagerViewModel _vm;

    public DownloadManagerWindow(DownloadManagerViewModel vm)
    {
        _vm = vm;
        Title = "Downloads";
        Width = 640;
        Height = 400;
        CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;

        Content = BuildContent();
    }

    private Control BuildContent()
    {
        var root = new DockPanel { LastChildFill = true };

        // ── Header toolbar ────────────────────────────────────────────────
        var toolbar = new DockPanel { Margin = new Thickness(8, 6) };

        var titleLabel = new TextBlock
        {
            Text = "Download Queue",
            FontWeight = FontWeight.SemiBold,
            VerticalAlignment = VerticalAlignment.Center,
        };
        DockPanel.SetDock(titleLabel, Dock.Left);
        toolbar.Children.Add(titleLabel);

        // Right-side buttons
        var rightPanel = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            Spacing = 4,
            HorizontalAlignment = HorizontalAlignment.Right,
        };

        var pauseAllBtn = new Button { Content = "⏸ Pause All", Padding = new Thickness(8, 3) };
        pauseAllBtn.Bind(Button.CommandProperty, new Binding("PauseAllCommand") { Source = _vm });

        var resumeAllBtn = new Button { Content = "▶ Resume All", Padding = new Thickness(8, 3) };
        resumeAllBtn.Bind(Button.CommandProperty, new Binding("ResumeAllCommand") { Source = _vm });

        var clearBtn = new Button { Content = "✕ Clear done", Padding = new Thickness(8, 3) };
        clearBtn.Bind(Button.CommandProperty, new Binding("ClearCompletedCommand") { Source = _vm });

        rightPanel.Children.Add(pauseAllBtn);
        rightPanel.Children.Add(resumeAllBtn);
        rightPanel.Children.Add(clearBtn);
        DockPanel.SetDock(rightPanel, Dock.Right);
        toolbar.Children.Add(rightPanel);

        var countBadge = new TextBlock
        {
            VerticalAlignment = VerticalAlignment.Center,
            Margin = new Thickness(8, 0),
            Opacity = 0.7,
        };
        countBadge.Bind(TextBlock.TextProperty,
            new Binding("ActiveCount") { Source = _vm, StringFormat = "{0} active" });
        toolbar.Children.Add(countBadge);

        var toolbarBorder = new Border
        {
            Child = toolbar,
            BorderThickness = new Thickness(0, 0, 0, 1),
        };
        DockPanel.SetDock(toolbarBorder, Dock.Top);
        root.Children.Add(toolbarBorder);

        // ── Download list ─────────────────────────────────────────────────
        var downloadList = new ItemsControl();
        downloadList.Bind(ItemsControl.ItemsSourceProperty,
            new Binding("Downloads") { Source = _vm });

        downloadList.ItemTemplate = new FuncDataTemplate<DownloadEntryViewModel>((entry, _) =>
        {
            if (entry is null) return new TextBlock { Text = "" };

            var outer = new Border
            {
                BorderThickness = new Thickness(0, 0, 0, 1),
                Padding = new Thickness(8, 6),
            };

            var row = new Grid { Margin = new Thickness(0, 2) };
            row.RowDefinitions.Add(new RowDefinition(GridLength.Auto));   // name + status row
            row.RowDefinitions.Add(new RowDefinition(GridLength.Auto));   // progress row

            // ─ Row 0: file name + status badge + action buttons ──────────
            var infoRow = new DockPanel();

            // Action buttons (right)
            var btnPanel = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 3 };

            var pauseBtn = new Button { Content = "⏸", Padding = new Thickness(5, 2) };
            ToolTip.SetTip(pauseBtn, "Pause");
            pauseBtn.Bind(Button.CommandProperty, new Binding("PauseCommand") { Source = entry });
            pauseBtn.Bind(Button.IsVisibleProperty, new Binding("CanPause") { Source = entry });

            var resumeBtn = new Button { Content = "▶", Padding = new Thickness(5, 2) };
            ToolTip.SetTip(resumeBtn, "Resume");
            resumeBtn.Bind(Button.CommandProperty, new Binding("ResumeCommand") { Source = entry });
            resumeBtn.Bind(Button.IsVisibleProperty, new Binding("CanResume") { Source = entry });

            var retryBtn = new Button { Content = "↺", Padding = new Thickness(5, 2) };
            ToolTip.SetTip(retryBtn, "Retry");
            retryBtn.Bind(Button.CommandProperty, new Binding("RetryCommand") { Source = entry });
            retryBtn.Bind(Button.IsVisibleProperty, new Binding("CanRetry") { Source = entry });

            var cancelBtn = new Button { Content = "✕", Padding = new Thickness(5, 2) };
            ToolTip.SetTip(cancelBtn, "Cancel");
            cancelBtn.Bind(Button.CommandProperty, new Binding("CancelCommand") { Source = entry });

            btnPanel.Children.Add(pauseBtn);
            btnPanel.Children.Add(resumeBtn);
            btnPanel.Children.Add(retryBtn);
            btnPanel.Children.Add(cancelBtn);
            DockPanel.SetDock(btnPanel, Dock.Right);
            infoRow.Children.Add(btnPanel);

            // Status badge
            var statusBadge = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 10,
                Opacity = 0.6,
                Margin = new Thickness(8, 0),
            };
            statusBadge.Bind(TextBlock.TextProperty, new Binding("Status") { Source = entry });
            DockPanel.SetDock(statusBadge, Dock.Right);
            infoRow.Children.Add(statusBadge);

            // File name
            var nameLabel = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                TextTrimming = TextTrimming.CharacterEllipsis,
                FontSize = 12,
            };
            nameLabel.Bind(TextBlock.TextProperty, new Binding("FileName") { Source = entry });
            infoRow.Children.Add(nameLabel);

            Grid.SetRow(infoRow, 0);
            row.Children.Add(infoRow);

            // ─ Row 1: progress bar + speed/size text ─────────────────────
            var progressRow = new DockPanel { Margin = new Thickness(0, 4, 0, 0) };

            var infoLabel = new TextBlock
            {
                VerticalAlignment = VerticalAlignment.Center,
                FontSize = 11,
                Opacity = 0.7,
                MinWidth = 140,
                TextAlignment = TextAlignment.Right,
            };
            // Show speed when downloading, size otherwise
            infoLabel.Bind(TextBlock.TextProperty, new Binding("ProgressText") { Source = entry });
            DockPanel.SetDock(infoLabel, Dock.Right);
            progressRow.Children.Add(infoLabel);

            var progressBar = new ProgressBar
            {
                Minimum = 0,
                Maximum = 100,
                Height = 10,
                Margin = new Thickness(0, 0, 8, 0),
            };
            progressBar.Bind(ProgressBar.ValueProperty, new Binding("ProgressPercent") { Source = entry });
            progressRow.Children.Add(progressBar);

            Grid.SetRow(progressRow, 1);
            row.Children.Add(progressRow);

            outer.Child = row;
            return outer;
        }, true);

        root.Children.Add(new ScrollViewer
        {
            Content = downloadList,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto,
        });

        return root;
    }
}
