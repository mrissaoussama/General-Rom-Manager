namespace AvaloniaUI.ViewModels;

/// <summary>First-run wizard: welcome → ROM folders → theme → done. Shown once, until finished or closed.</summary>
public sealed partial class OnboardingViewModel : DialogViewModel
{
    public const int StepCount = 4;
    private readonly Settings _settings;
    private readonly Action<Settings> _persist;

    public OnboardingViewModel(Settings settings, Action<Settings>? persist = null)
    {
        _settings = settings;
        _persist = persist ?? (s => s.Save());
        Folders = [.. ConsoleModules.Descriptors.Select(d =>
            new OnboardingFolder(d.Id, d.DisplayName, new PathField(settings.ConsoleLibraryPaths.GetValueOrDefault(d.Id, ""))))];
        _themeIndex = settings.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 };
        _downloadBoxArt = settings.DownloadBoxArt;
    }

    /// <summary>True when nothing is configured yet (no folder in settings or config.json) and the wizard never ran.</summary>
    public static bool ShouldShow(Settings settings, ILibraryRoots roots) =>
        !settings.OnboardingCompleted && roots.ConfiguredConsoles().Count == 0;

    public override string Title => "Welcome to RomManager";
    public override double Width => 620;
    public override double Height => 500;

    public IReadOnlyList<OnboardingFolder> Folders { get; }
    public IReadOnlyList<string> ThemeNames => SettingsViewModel.ThemeNames;

    [ObservableProperty]
    [NotifyPropertyChangedFor(nameof(IsWelcome), nameof(IsFolders), nameof(IsTheme), nameof(IsDone), nameof(StepText), nameof(CanGoBack))]
    [NotifyCanExecuteChangedFor(nameof(NextCommand), nameof(BackCommand), nameof(ConfirmCommand))]
    private int _step;

    [ObservableProperty] private int _themeIndex;

    /// <summary>The one online feature that runs without being asked for; chosen here, changeable in Settings → Online.</summary>
    [ObservableProperty] private bool _downloadBoxArt;

    public bool IsWelcome => Step == 0;
    public bool IsFolders => Step == 1;
    public bool IsTheme => Step == 2;
    public bool IsDone => Step == StepCount - 1;
    public bool CanGoBack => Step > 0;
    public string StepText => $"Step {Step + 1} of {StepCount}";
    public int ChosenFolders => Folders.Count(f => !string.IsNullOrWhiteSpace(f.Path.Value));

    partial void OnThemeIndexChanged(int value) => SettingsViewModel.PreviewTheme(value);

    [RelayCommand(CanExecute = nameof(CanGoNext))]
    private void Next()
    {
        Step++;
        if (IsDone) OnPropertyChanged(nameof(ChosenFolders));
    }

    private bool CanGoNext() => Step < StepCount - 1;

    [RelayCommand(CanExecute = nameof(CanGoBack))]
    private void Back() => Step--;

    protected override bool CanConfirm() => IsDone;

    [RelayCommand]
    private async Task Browse(OnboardingFolder folder)
    {
        if (await Dialogs.PickFolderAsync($"Select {folder.DisplayName} ROM folder") is { } path) folder.Path.Value = path;
    }

    /// <summary>Saves the folders and theme on Finish. Either way the wizard does not come back on the next start.</summary>
    public override void OnClosed()
    {
        if (Confirmed)
        {
            foreach (var f in Folders)
            {
                if (string.IsNullOrWhiteSpace(f.Path.Value)) _settings.ConsoleLibraryPaths.Remove(f.Id);
                else _settings.ConsoleLibraryPaths[f.Id] = f.Path.Value.Trim();
            }
            _settings.Theme = SettingsViewModel.ThemeKey(ThemeIndex);
            _settings.DownloadBoxArt = DownloadBoxArt;
            if (RomViewModel.CoverCache is { } covers) covers.AllowDownloads = DownloadBoxArt;
        }
        else
            SettingsViewModel.PreviewTheme(_settings.Theme switch { "Light" => 1, "Dark" => 2, _ => 0 });
        _settings.OnboardingCompleted = true;
        _persist(_settings);
    }
}

public sealed record OnboardingFolder(string Id, string DisplayName, PathField Path);
