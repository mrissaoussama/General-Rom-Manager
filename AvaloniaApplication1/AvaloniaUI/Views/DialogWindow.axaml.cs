namespace AvaloniaUI.Views;

/// <summary>Hosts a <see cref="DialogViewModel"/>; the view comes from <see cref="ViewLocator"/>.</summary>
public partial class DialogWindow : Window
{
    public DialogWindow() => InitializeComponent();

    public DialogWindow(DialogViewModel vm) : this()
    {
        DataContext = vm;
        Width = vm.Width;
        if (double.IsNaN(vm.Height)) SizeToContent = SizeToContent.Height;
        else Height = vm.Height;
        vm.CloseRequested += Close;
    }
}
