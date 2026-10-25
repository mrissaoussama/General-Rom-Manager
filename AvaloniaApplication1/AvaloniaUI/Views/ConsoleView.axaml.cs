using System.Windows.Input;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace AvaloniaUI.Views;

/// <summary>
/// Console page. Columns and the context menu depend on the console type and plugins,
/// so they are built here; everything they do goes through view-model commands.
/// </summary>
public partial class ConsoleView : UserControl
{
    private static readonly List<string> DefaultColumns = ["TitleID", "Title", "RomType", "Region", "Version"];

    private readonly DataGrid _grid;
    private readonly TextBox _search;
    private readonly ContextMenu _menu;

    private ConsoleViewModelBase? Vm => DataContext as ConsoleViewModelBase;
    private IReadOnlyList<RomViewModel> Selection => _grid.SelectedItems.OfType<RomViewModel>().ToList();

    public ConsoleView()
    {
        AvaloniaXamlLoader.Load(this);
        _grid = this.FindControl<DataGrid>("RomsGrid")!;
        _search = this.FindControl<TextBox>("SearchBox")!;
        _menu = this.FindControl<ContextMenu>("RomMenu")!;
        _menu.Opening += (_, _) => BuildMenu();
        this.FindControl<Button>("ColumnsButton")!.Click += (s, _) => ShowColumnsFlyout((Control)s!);
        _grid.KeyDown += OnGridKeyDown;
    }

    public void FocusSearch()
    {
        _search.Focus();
        _search.SelectAll();
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        BuildColumns();
    }

    protected override void OnAttachedToVisualTree(VisualTreeAttachmentEventArgs e)
    {
        base.OnAttachedToVisualTree(e);
        Settings.ColumnsChanged += OnColumnsChanged;
        Settings.CompactModeChanged += ApplyCompact;
        ApplyCompact();
    }

    protected override void OnDetachedFromVisualTree(VisualTreeAttachmentEventArgs e)
    {
        Settings.ColumnsChanged -= OnColumnsChanged;
        Settings.CompactModeChanged -= ApplyCompact;
        base.OnDetachedFromVisualTree(e);
    }

    private void OnColumnsChanged(string console)
    {
        if (string.Equals(console, Vm?.ConsoleName, StringComparison.OrdinalIgnoreCase))
            Dispatcher.UIThread.Post(BuildColumns);
    }

    private void ApplyCompact() => Dispatcher.UIThread.Post(() =>
    {
        var compact = Settings.Instance.IsCompactMode;
        _grid.RowHeight = compact ? 22 : double.NaN;
        _grid.FontSize = compact ? 11 : 12;
    });

    // ── Columns ───────────────────────────────────────────────────────────

    private List<string> VisibleColumns(string console)
    {
        var settings = Settings.Instance;
        if (!settings.VisibleColumns.TryGetValue(console, out var visible))
        {
            settings.VisibleColumns[console] = visible = [.. DefaultColumns];
            settings.Save();
        }
        return visible;
    }

    private void BuildColumns()
    {
        _grid.Columns.Clear();
        if (Vm is not { } vm) return;
        var visible = VisibleColumns(vm.ConsoleName);
        foreach (var (prop, header, usesDict) in vm.GetColumnDescriptors())
        {
            _grid.Columns.Add(new DataGridTextColumn
            {
                Header = header,
                Binding = usesDict
                    ? new Binding(nameof(RomViewModel.PropertiesDictionary)) { Converter = new DictionaryItemConverter(), ConverterParameter = prop }
                    : new Binding(prop),
                CanUserSort = !usesDict,
                IsVisible = visible.Contains(prop),
                Tag = prop,
            });
        }
    }

    private void ShowColumnsFlyout(Control anchor)
    {
        if (Vm is not { } vm) return;
        var visible = VisibleColumns(vm.ConsoleName);
        var panel = new WrapPanel { MaxWidth = 460, Margin = new Thickness(8) };
        foreach (var (prop, header, _) in vm.GetColumnDescriptors())
        {
            var cb = new CheckBox { Content = header, IsChecked = visible.Contains(prop), Margin = new Thickness(0, 0, 12, 4) };
            cb.IsCheckedChanged += (_, _) =>
            {
                if (cb.IsChecked == true && !visible.Contains(prop)) visible.Add(prop);
                else if (cb.IsChecked != true) visible.Remove(prop);
                Settings.Instance.Save();
                foreach (var col in _grid.Columns.Where(c => Equals(c.Tag, prop)))
                    col.IsVisible = cb.IsChecked == true;
            };
            panel.Children.Add(cb);
        }
        var reset = new Button { Content = "Reset to defaults", Margin = new Thickness(8, 0, 8, 8) };
        reset.Click += (_, _) =>
        {
            Settings.Instance.VisibleColumns[vm.ConsoleName] = [.. DefaultColumns];
            Settings.Instance.Save();
            BuildColumns();
        };
        new Flyout
        {
            Placement = PlacementMode.BottomEdgeAlignedRight,
            Content = new StackPanel { Children = { panel, reset } },
        }.ShowAt(anchor);
    }

    // ── Context menu + keys ───────────────────────────────────────────────

    private void BuildMenu()
    {
        _menu.Items.Clear();
        if (Vm is not { } vm) return;
        var sel = Selection;
        if (sel.Count == 0)
        {
            _menu.Items.Add(new MenuItem { Header = "No ROM selected", IsEnabled = false });
            return;
        }
        var single = sel.Count == 1;
        var n = single ? "" : $" ({sel.Count})";

        MenuItem Item(string header, ICommand command, bool enabled = true, string? gesture = null) => new()
        {
            Header = header,
            Command = command,
            CommandParameter = sel,
            IsEnabled = enabled,
            InputGesture = gesture is null ? null : KeyGesture.Parse(gesture),
        };

        _menu.Items.Add(Item("Show in folder", vm.RevealInExplorerCommand, single));
        _menu.Items.Add(Item("Open with default app", vm.OpenWithDefaultAppCommand, single));
        _menu.Items.Add(new Separator());
        _menu.Items.Add(Item("Copy title ID" + n, vm.CopyTitleIdsCommand));
        _menu.Items.Add(Item("Copy title" + n, vm.CopyTitlesCommand));
        _menu.Items.Add(Item("Copy file name" + n, vm.CopyFileNamesCommand));
        _menu.Items.Add(new Separator());
        _menu.Items.Add(Item("Rename…" + n, vm.RenameCommand, gesture: "F2"));
        _menu.Items.Add(Item("Move to…" + n, vm.MoveCommand));
        _menu.Items.Add(Item("Properties…", vm.ViewPropertiesCommand, single));
        _menu.Items.Add(new Separator());
        _menu.Items.Add(Item("Delete" + n, vm.DeleteCommand, gesture: "Delete"));

        var actions = vm.GetContextActionsForSelection(sel.Select(r => r.RomSource)).GroupBy(a => a.Category).ToList();
        foreach (var group in actions)
        {
            var sub = new MenuItem { Header = group.Key };
            foreach (var action in group)
            {
                var item = new MenuItem { Header = action.Name };
                ToolTip.SetTip(item, action.Description);
                item.Click += (_, _) => vm.RunAction(action, sel);
                sub.Items.Add(item);
            }
            _menu.Items.Add(sub);
        }
    }

    private void OnGridKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is not { } vm || Selection is not { Count: > 0 } sel) return;
        ICommand? command = e.Key switch
        {
            Key.Delete => vm.DeleteCommand,
            Key.F2 => vm.RenameCommand,
            Key.Enter => vm.ViewPropertiesCommand,
            _ => null,
        };
        if (command is null) return;
        command.Execute(sel);
        e.Handled = true;
    }
}
