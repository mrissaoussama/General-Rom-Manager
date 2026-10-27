namespace AvaloniaUI.ViewModels;

/// <summary>Live tail of the application log.</summary>
public sealed partial class LogViewModel : ViewModelBase
{
    private const int MaxLines = 500;

    public ObservableCollection<string> Lines { get; } = [];

    public LogViewModel()
    {
        Reload();
        FileUtils.LogAdded += entry => Dispatcher.UIThread.Post(() => Append(entry));
    }

    private void Append(string entry)
    {
        Lines.Add(entry);
        while (Lines.Count > MaxLines) Lines.RemoveAt(0);
    }

    [RelayCommand]
    private void Reload()
    {
        Lines.Clear();
        foreach (var line in FileUtils.GetRecentLogs()) Lines.Add(line);
    }

    [RelayCommand]
    private void OpenFile() => Shell.OpenWithDefaultApp(FileUtils.LogFilePath);

    [RelayCommand]
    private Task Copy() => Shell.CopyAsync(string.Join(Environment.NewLine, Lines));
}

/// <summary>Row in the operations panel; <see cref="Refresh"/> re-reads the wrapped operation.</summary>
public sealed partial class OperationItemViewModel(QueuedOperation op) : ViewModelBase
{
    public QueuedOperation Operation { get; } = op;
    public string Text => $"[{Operation.Category}] {Operation.Name}"
        + (string.IsNullOrEmpty(Operation.ErrorMessage) ? "" : $" — {Operation.ErrorMessage}")
        + (HasWarning ? $" — {Operation.Warning}" : "");
    public bool HasWarning => Operation.State == OperationState.Completed && !string.IsNullOrEmpty(Operation.Warning);
    public string State => HasWarning ? Strings.Operations_PartlyDone : Operation.State.ToString();
    public bool IsRunning => Operation.State == OperationState.Running;
    public double Progress => Operation.ProgressPercent;
    public bool CanCancel => Operation.CanCancel;
    public bool CanUndo => Operation.CanUndo;

    public void Refresh() => OnPropertyChanged(string.Empty);
}

public sealed partial class OperationsViewModel : ViewModelBase
{
    private readonly OperationQueue _queue;

    public ObservableCollection<OperationItemViewModel> Items { get; } = [];
    public bool IsEmpty => Items.Count == 0;

    public OperationsViewModel(OperationQueue queue)
    {
        _queue = queue;
        _queue.OperationStateChanged += op => Dispatcher.UIThread.Post(() => Sync(op));
        Items.CollectionChanged += (_, _) => OnPropertyChanged(nameof(IsEmpty));
    }

    private void Sync(QueuedOperation op)
    {
        var item = Items.FirstOrDefault(i => i.Operation.Id == op.Id);
        if (item is null) Items.Add(new OperationItemViewModel(op));
        else item.Refresh();
    }

    [RelayCommand]
    private void Cancel(OperationItemViewModel item)
    {
        _queue.Cancel(item.Operation);
        item.Refresh();
    }

    [RelayCommand]
    private async Task Undo(OperationItemViewModel item)
    {
        await _queue.UndoAsync(item.Operation);
        item.Refresh();
    }

    [RelayCommand]
    private void CancelAll()
    {
        _queue.CancelAll();
        foreach (var item in Items) item.Refresh();
    }

    [RelayCommand]
    private void ClearFinished()
    {
        _queue.ClearFinished();
        var live = _queue.Operations.Select(o => o.Id).ToHashSet();
        foreach (var item in Items.Where(i => !live.Contains(i.Operation.Id)).ToList())
            Items.Remove(item);
    }
}
