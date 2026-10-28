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
    private readonly DataGrid _grid;
    private readonly TextBox _search;
    private readonly ContextMenu _menu;

    private ConsoleViewModelBase? Vm => DataContext as ConsoleViewModelBase;
    // The card under the pointer when the cards' menu opened (cards have no multi-selection).
    private RomViewModel? _cardTarget;

    private IReadOnlyList<RomViewModel> Selection => Vm is { IsCardView: true }
        ? (_cardTarget is null ? [] : [_cardTarget])
        : _grid.SelectedItems.OfType<RomViewModel>().ToList();

    public ConsoleView()
    {
        AvaloniaXamlLoader.Load(this);
        _grid = this.FindControl<DataGrid>("RomsGrid")!;
        _search = this.FindControl<TextBox>("SearchBox")!;
        _menu = this.FindControl<ContextMenu>("RomMenu")!;
        _menu.Opening += (_, _) => BuildMenu(_menu);
        this.FindControl<Button>("ColumnsButton")!.Click += (s, _) => ShowColumnsFlyout((Control)s!);
        _grid.KeyDown += OnGridKeyDown;
        // Cards are virtualized; free covers of recycled cards so large libraries keep memory flat.
        _cards = this.FindControl<ItemsRepeater>("Cards")!;
        _cards.ElementClearing += (_, e) =>
            (e.Element.DataContext as RomViewModel)?.ReleaseCover();
        _cards.AddHandler(KeyDownEvent, OnCardsKeyDown, RoutingStrategies.Tunnel);
        // Right-click on a card: the same menu as the table, for that card.
        var cardMenu = new ContextMenu();
        cardMenu.Opening += (_, _) => BuildMenu(cardMenu);
        _cards.ContextMenu = cardMenu;
        _cards.AddHandler(ContextRequestedEvent, (_, e) =>
        {
            _cardTarget = (e.Source as Control)?.FindAncestorOfType<Button>(includeSelf: true)?.DataContext as RomViewModel;
            if (_cardTarget is not null && Vm is { } vm) vm.SelectedRom = _cardTarget;
        }, RoutingStrategies.Tunnel);
    }

    private readonly ItemsRepeater _cards;

    // Arrow keys, Home/End and PageUp/PageDown move focus between cards; Delete/F2/Enter act on the focused card.
    private void OnCardsKeyDown(object? sender, KeyEventArgs e)
    {
        if (Vm is not { } vm || TopLevel.GetTopLevel(this)?.FocusManager?.GetFocusedElement() is not Control focused) return;
        var card = focused.FindAncestorOfType<Button>(includeSelf: true);
        if (card?.DataContext is not RomViewModel rom) return;
        var index = _cards.GetElementIndex(card);
        if (index < 0) return;

        ICommand? command = e.Key switch
        {
            Key.Delete => vm.DeleteCommand,
            Key.F2 => vm.RenameCommand,
            Key.Enter when e.KeyModifiers == KeyModifiers.Alt => vm.ViewPropertiesCommand,
            _ => null,
        };
        if (command is not null)
        {
            command.Execute(new List<RomViewModel> { rom });
            e.Handled = true;
            return;
        }

        var count = _cards.ItemsSourceView?.Count ?? 0;
        var target = CardNavigation.Target(index, count, CardColumns(), RowsPerPage(card), e.Key);
        if (target is not { } t || t == index) return;
        if (_cards.GetOrCreateElement(t) is Control next)
        {
            next.BringIntoView();
            next.Focus(NavigationMethod.Directional);
            e.Handled = true;
        }
    }

    // Columns of the uniform grid: realized cards sharing the first realized card's row.
    private int CardColumns()
    {
        var realized = _cards.Children.Where(c => c.IsVisible && _cards.GetElementIndex(c) >= 0).ToList();
        if (realized.Count == 0) return 1;
        var top = realized.Min(c => c.Bounds.Y);
        return Math.Max(1, realized.Count(c => Math.Abs(c.Bounds.Y - top) < 1));
    }

    private int RowsPerPage(Control card) =>
        _cards.Parent is ScrollViewer sv && card.Bounds.Height > 0 ? Math.Max(1, (int)(sv.Viewport.Height / card.Bounds.Height)) : 1;

    public void FocusSearch()
    {
        _search.Focus();
        _search.SelectAll();
    }

    private ConsoleViewModelBase? _subscribed;

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (_subscribed is not null) _subscribed.RomRevealed -= OnRomRevealed;
        _subscribed = Vm;
        if (_subscribed is not null) _subscribed.RomRevealed += OnRomRevealed;
        BuildColumns();
    }

    // A related game/update/DLC was clicked in the details: bring its row (or card) into view.
    private void OnRomRevealed(RomViewModel row) => Dispatcher.UIThread.Post(() =>
    {
        if (Vm is { IsCardView: true })
        {
            var index = _cards.ItemsSourceView?.IndexOf(row) ?? -1;
            if (index >= 0) _cards.GetOrCreateElement(index).BringIntoView();
        }
        else
            _grid.ScrollIntoView(row, null);
    }, DispatcherPriority.Background);

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

    private static List<string> VisibleColumns(ConsoleViewModelBase vm)
    {
        var settings = Settings.Instance;
        if (!settings.VisibleColumns.TryGetValue(vm.ConsoleName, out var visible))
        {
            settings.VisibleColumns[vm.ConsoleName] = visible = [.. vm.DefaultColumns];
            settings.Save();
        }
        return visible;
    }

    private void BuildColumns()
    {
        _grid.Columns.Clear();
        if (Vm is not { } vm) return;
        var visible = VisibleColumns(vm);
        foreach (var (prop, header, usesDict) in vm.GetColumnDescriptors())
        {
            _grid.Columns.Add(new DataGridTextColumn
            {
                Header = Labels.Property(prop, header),
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
        var visible = VisibleColumns(vm);
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
        var reset = new Button { Content = Strings.Settings_ResetToDefaults, Margin = new Thickness(8, 0, 8, 8) };
        reset.Click += (_, _) =>
        {
            Settings.Instance.VisibleColumns[vm.ConsoleName] = [.. vm.DefaultColumns];
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

    private void BuildMenu(ContextMenu menu)
    {
        menu.Items.Clear();
        if (Vm is not { } vm) return;
        var sel = Selection;
        if (sel.Count == 0)
        {
            menu.Items.Add(new MenuItem { Header = Strings.ConsoleView_NoROMSelected, IsEnabled = false });
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

        menu.Items.Add(Item(Strings.ConsoleView_ShowInFolder, vm.RevealInExplorerCommand, single));
        menu.Items.Add(Item(Strings.ConsoleView_OpenWithDefaultApp, vm.OpenWithDefaultAppCommand, single));
        menu.Items.Add(Item(Strings.ConsoleView_LaunchInEmulator, vm.LaunchInEmulatorCommand, single));
        menu.Items.Add(new Separator());
        menu.Items.Add(Item(Strings.ConsoleView_CopyTitleID + n, vm.CopyTitleIdsCommand));
        menu.Items.Add(Item(Strings.ConsoleView_CopyTitle + n, vm.CopyTitlesCommand));
        menu.Items.Add(Item(Strings.ConsoleView_CopyFileName + n, vm.CopyFileNamesCommand));
        menu.Items.Add(new Separator());
        menu.Items.Add(Item(Strings.ConsoleView_Rename + n, vm.RenameCommand, gesture: "F2"));
        menu.Items.Add(Item(Strings.ConsoleView_MoveTo + n, vm.MoveCommand));
        menu.Items.Add(Item(Strings.ConsoleView_Properties, vm.ViewPropertiesCommand, single));
        menu.Items.Add(Item(Strings.ConsoleView_CalculateHashes + n, vm.CalculateHashesCommand));
        var batch = new MenuItem { Header = Strings.ConsoleView_Batch };
        batch.Items.Add(Item(Strings.ConsoleView_VerifyWithDat + n, vm.VerifySelectionCommand));
        batch.Items.Add(Item(Strings.ConsoleView_GroupByGame + n, vm.GroupByGameCommand));
        batch.Items.Add(Item(Strings.ConsoleView_ExportSelection + n, vm.ExportSelectionCommand));
        var related = vm.RelatedRows(sel);
        var selectRelated = new MenuItem { Header = string.Format(Strings.ConsoleView_SelectUpdatesAndDlc, related.Count), IsEnabled = related.Count > 0 && !vm.IsCardView };
        selectRelated.Click += (_, _) =>
        {
            foreach (var row in related) _grid.SelectedItems.Add(row);
        };
        batch.Items.Add(selectRelated);
        menu.Items.Add(batch);
        var saves = new MenuItem { Header = Strings.ConsoleView_SaveData };
        saves.Items.Add(Item(Strings.ConsoleView_OpenSaveFolder, vm.OpenSaveFolderCommand, single));
        saves.Items.Add(Item(Strings.ConsoleView_BackUpSaves + n, vm.BackupSavesCommand));
        saves.Items.Add(Item(Strings.ConsoleView_RestoreSaves, vm.RestoreSavesCommand, single));
        menu.Items.Add(saves);
        if (ConsoleViewModelBase.AvailableConversions(sel) is { Count: > 0 } conversions)
        {
            var convert = new MenuItem { Header = Strings.ConsoleView_Convert + n };
            foreach (var tool in conversions)
            {
                var item = new MenuItem { Header = tool.DisplayName };
                ToolTip.SetTip(item, string.Format(Strings.ConsoleView_KeepsTheOriginalWritesNextToIt, tool.OutputExtension));
                item.Click += async (_, _) => await vm.ConvertAsync(tool, sel);
                convert.Items.Add(item);
            }
            menu.Items.Add(convert);
        }
        menu.Items.Add(new Separator());
        menu.Items.Add(Item(Strings.ConsoleView_Delete + n, vm.DeleteCommand, gesture: "Delete"));

        var actions = vm.GetContextActionsForSelection(sel.Select(r => r.RomSource)).GroupBy(a => a.Category).ToList();
        if (actions.Count > 0) menu.Items.Add(new Separator());
        foreach (var group in actions)
        {
            var sub = new MenuItem { Header = group.Key };
            foreach (var action in group)
            {
                var item = new MenuItem { Header = action.Name, Icon = MenuIcon(action.IconKey) };
                ToolTip.SetTip(item, action.Description);
                item.Click += (_, _) => vm.RunAction(action, sel);
                sub.Items.Add(item);
            }
            menu.Items.Add(sub);
        }
    }

    // IconKey: an image file (a plugin's icons are resolved next to its DLL) or an application resource.
    private static Control? MenuIcon(string? key)
    {
        if (string.IsNullOrWhiteSpace(key)) return null;
        try
        {
            IImage? image = File.Exists(key) ? new Avalonia.Media.Imaging.Bitmap(key)
                : Application.Current?.TryFindResource(key, out var resource) == true ? resource as IImage : null;
            return image is null ? null : new Image { Source = image, Width = 16, Height = 16 };
        }
        catch (Exception ex)
        {
            AppLog.Warn($"[Menu] icon '{key}': {ex.Message}");
            return null;
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
