using Avalonia.Input;
using Avalonia.Markup.Xaml;

namespace AvaloniaUI.Views;

public partial class MainView : UserControl
{
    public MainView()
    {
        AvaloniaXamlLoader.Load(this);
        KeyBindings.Add(new KeyBinding
        {
            Gesture = new KeyGesture(Key.F, KeyModifiers.Control),
            Command = new RelayCommand(() => this.GetVisualDescendants().OfType<ConsoleView>().FirstOrDefault()?.FocusSearch()),
        });
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
