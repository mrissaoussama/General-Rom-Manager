using RomManagerShared.Utils;

namespace RomManagerShared.Base;

/// <summary>The lifecycle state of a queued operation.</summary>
public enum OperationState
{
    Pending,
    Running,
    Completed,
    Failed,
    Cancelled,
    Undone,
}

/// <summary>
/// Describes one discrete unit of work that can be queued, executed,
/// cancelled, and optionally undone.
/// </summary>
public sealed class QueuedOperation
{
    private static int _nextId;

    public int Id { get; } = Interlocked.Increment(ref _nextId);
    public required string Name { get; init; }
    public string? Description { get; init; }
    public string Category { get; init; } = "General";

    /// <summary>The async work to perform. Receives the CancellationToken and a progress callback (0–100).</summary>
    public required Func<CancellationToken, IProgress<double>, Task> ExecuteAsync { get; init; }

    /// <summary>Optional undo logic. If set, the operation is considered undoable.</summary>
    public Func<CancellationToken, Task>? UndoAsync { get; init; }

    public OperationState State { get; internal set; } = OperationState.Pending;
    public double ProgressPercent { get; internal set; }
    public string? ErrorMessage { get; internal set; }
    public DateTime? StartedAt { get; internal set; }
    public DateTime? CompletedAt { get; internal set; }

    internal CancellationTokenSource Cts { get; set; } = new();

    public bool CanCancel => State is OperationState.Pending or OperationState.Running;
    public bool CanUndo => State == OperationState.Completed && UndoAsync is not null;
    public bool IsFinished => State is OperationState.Completed or OperationState.Failed
                                                                 or OperationState.Cancelled
                                                                 or OperationState.Undone;
}

/// <summary>
/// A concurrency-limited operation queue. Operations run sequentially by
/// default (maxConcurrent=1) or N-at-a-time. Supports cancel, undo, and
/// state snapshots.
/// </summary>
public sealed class OperationQueue
{
    private readonly SemaphoreSlim _semaphore;
    private readonly List<QueuedOperation> _operations = [];
    private readonly object _lock = new();

    /// <summary>Fired whenever any operation changes state.</summary>
    public event Action<QueuedOperation>? OperationStateChanged;

    public OperationQueue(int maxConcurrent = 1)
    {
        _semaphore = new SemaphoreSlim(maxConcurrent, maxConcurrent);
    }

    /// <summary>Thread-safe snapshot of all operations.</summary>
    public IReadOnlyList<QueuedOperation> Operations
    {
        get { lock (_lock) return _operations.ToList(); }
    }

    /// <summary>Enqueue an operation and begin processing it when a slot opens.</summary>
    public QueuedOperation Enqueue(QueuedOperation op)
    {
        lock (_lock) _operations.Add(op);
        OperationStateChanged?.Invoke(op);
        _ = RunAsync(op);
        return op;
    }

    /// <summary>Shorthand to enqueue a simple named action.</summary>
    public QueuedOperation Enqueue(string name, Func<CancellationToken, IProgress<double>, Task> execute,
        Func<CancellationToken, Task>? undo = null, string category = "General")
    {
        return Enqueue(new QueuedOperation
        {
            Name = name,
            ExecuteAsync = execute,
            UndoAsync = undo,
            Category = category,
        });
    }

    /// <summary>Cancel a specific operation.</summary>
    public void Cancel(QueuedOperation op)
    {
        if (!op.CanCancel) return;
        op.Cts.Cancel();
        op.State = OperationState.Cancelled;
        op.CompletedAt = DateTime.Now;
        OperationStateChanged?.Invoke(op);
    }

    /// <summary>Cancel all pending/running operations.</summary>
    public void CancelAll()
    {
        lock (_lock)
        {
            foreach (var op in _operations.Where(o => o.CanCancel))
                Cancel(op);
        }
    }

    /// <summary>Undo a completed operation (if undoable).</summary>
    public async Task UndoAsync(QueuedOperation op)
    {
        if (!op.CanUndo) return;
        try
        {
            op.State = OperationState.Running;
            OperationStateChanged?.Invoke(op);
            await op.UndoAsync!(CancellationToken.None);
            op.State = OperationState.Undone;
        }
        catch (Exception ex)
        {
            op.ErrorMessage = $"Undo failed: {ex.Message}";
            op.State = OperationState.Failed;
            FileUtils.Log($"[OperationQueue] Undo failed for '{op.Name}': {ex.Message}");
        }
        finally
        {
            OperationStateChanged?.Invoke(op);
        }
    }

    /// <summary>Remove all finished operations from the list.</summary>
    public void ClearFinished()
    {
        lock (_lock) _operations.RemoveAll(o => o.IsFinished);
    }

    /// <summary>Serialisable snapshot of current queue state.</summary>
    public IReadOnlyList<OperationSnapshot> GetSnapshot()
    {
        lock (_lock)
            return _operations.Select(o => new OperationSnapshot
            {
                Id = o.Id,
                Name = o.Name,
                Category = o.Category,
                State = o.State,
                Progress = o.ProgressPercent,
                Error = o.ErrorMessage,
                StartedAt = o.StartedAt,
                CompletedAt = o.CompletedAt,
            }).ToList();
    }

    // ── Internal ──────────────────────────────────────────────────────────

    private async Task RunAsync(QueuedOperation op)
    {
        try
        {
            await _semaphore.WaitAsync(op.Cts.Token);
        }
        catch (OperationCanceledException)
        {
            op.State = OperationState.Cancelled;
            op.CompletedAt = DateTime.Now;
            OperationStateChanged?.Invoke(op);
            return;
        }

        try
        {
            op.State = OperationState.Running;
            op.StartedAt = DateTime.Now;
            OperationStateChanged?.Invoke(op);

            var progress = new Progress<double>(pct =>
            {
                op.ProgressPercent = pct;
                OperationStateChanged?.Invoke(op);
            });

            await op.ExecuteAsync(op.Cts.Token, progress);

            op.State = OperationState.Completed;
            op.ProgressPercent = 100;
        }
        catch (OperationCanceledException)
        {
            op.State = OperationState.Cancelled;
        }
        catch (Exception ex)
        {
            op.State = OperationState.Failed;
            op.ErrorMessage = ex.Message;
            FileUtils.Log($"[OperationQueue] '{op.Name}' failed: {ex.Message}");
        }
        finally
        {
            op.CompletedAt = DateTime.Now;
            _semaphore.Release();
            OperationStateChanged?.Invoke(op);
        }
    }
}

/// <summary>Snapshot of a single operation for serialisation / display.</summary>
public sealed class OperationSnapshot
{
    public int Id { get; init; }
    public string Name { get; init; } = "";
    public string Category { get; init; } = "";
    public OperationState State { get; init; }
    public double Progress { get; init; }
    public string? Error { get; init; }
    public DateTime? StartedAt { get; init; }
    public DateTime? CompletedAt { get; init; }
}
