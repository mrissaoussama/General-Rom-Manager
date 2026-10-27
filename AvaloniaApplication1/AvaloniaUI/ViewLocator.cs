using System.Runtime.CompilerServices;

namespace AvaloniaUI;

/// <summary>
/// Maps view models to views. Views are cached per view model so switching pages keeps
/// scroll position, sorting and column layout.
/// </summary>
public class ViewLocator : IDataTemplate
{
    private readonly ConditionalWeakTable<object, Control> _cache = new();

    public Control? Build(object? param)
    {
        if (param is null) return null;
        if (_cache.TryGetValue(param, out var cached)) return cached;

        Control? view = param switch
        {
            DashboardViewModel => new DashboardView(),
            ConsoleViewModelBase => new ConsoleView(),
            RomDetailsViewModel => new RomDetailsView(),
            ConfirmDialogViewModel => new Views.DialogViews.ConfirmDialogView(),
            PreviewDialogViewModel => new Views.DialogViews.PreviewDialogView(),
            PropertiesDialogViewModel => new Views.DialogViews.PropertiesDialogView(),
            ChooseConsoleDialogViewModel => new Views.DialogViews.ChooseConsoleDialogView(),
            RenameDialogViewModel => new Views.DialogViews.RenameDialogView(),
            SettingsViewModel => new Views.DialogViews.SettingsView(),
            OnboardingViewModel => new Views.DialogViews.OnboardingView(),
            _ => null,
        };
        if (view is null) return new TextBlock { Text = "No view for " + param.GetType().Name };
        if (param is not (RomDetailsViewModel or DialogViewModel)) _cache.Add(param, view);
        return view;
    }

    public bool Match(object? data) => data is ViewModelBase;
}
