namespace AvaloniaUI.ViewModels;

public abstract class ViewModelBase : ObservableObject
{
    /// <summary>Dialogs and pickers; tests swap <see cref="DialogService.Current"/>.</summary>
    protected static IDialogService Dialogs => DialogService.Current;
}