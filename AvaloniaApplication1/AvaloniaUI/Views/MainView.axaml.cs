using Avalonia.Input;
using Avalonia.Markup.Xaml;
using Avalonia.Platform.Storage;

namespace AvaloniaUI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        AvaloniaXamlLoader.Load(this);
        DragDrop.SetAllowDrop(this, true);
        AddHandler(DragDrop.DragOverEvent, (_, e) =>
            e.DragEffects = e.DataTransfer.Contains(DataFormat.File) ? DragDropEffects.Copy : DragDropEffects.None);
        AddHandler(DragDrop.DropEvent, OnDrop);
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.F, KeyModifiers.Control),
            Command = new RelayCommand(() => this.GetVisualDescendants().OfType<ConsoleView>().FirstOrDefault()?.FocusSearch()),
        });
    }

    // Dropped folders become ROM folders; a dropped file adds the folder it is in.
    private async void OnDrop(object? sender, DragEventArgs e)
    {
        if (DataContext is not MainViewModel vm || e.DataTransfer.TryGetFiles() is not { } items) return;
        var folders = items
            .Select(i => i.TryGetLocalPath())
            .OfType<string>()
            .Select(p => Directory.Exists(p) ? p : Path.GetDirectoryName(p))
            .OfType<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();
        try
        {
            foreach (var folder in folders)
                await vm.AddLibraryFolderAsync(folder);
        }
        catch (Exception ex) { FileUtils.Log($"[Drop] {ex}"); }
    }

    protected override void OnDataContextChanged(EventArgs e)
    {
        base.OnDataContextChanged(e);
        if (DataContext is not MainViewModel vm) return;
        // Keep the log scrolled to the newest entry.
        var list = this.FindControl<ListBox>("LogList")!;
        vm.Log.Lines.CollectionChanged += (_, _) =>
        {
            if (vm.Log.Lines.Count > 0) list.ScrollIntoView(vm.Log.Lines.Count - 1);
        };
    }
}
