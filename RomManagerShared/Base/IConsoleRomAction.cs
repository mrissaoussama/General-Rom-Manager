namespace RomManagerShared.Base;

// ──────────────────────────────────────────────────────────────────────────────
// Console-specific ROM action (context-menu entry model)
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Represents a single action that can be triggered on a set of selected ROMs,
/// typically shown in a context menu or toolbar.  Implement this in every
/// per-console manager that should expose commands to the UI.
/// </summary>
public interface IConsoleRomAction
{
    /// <summary>Short display name (e.g. "Check for Missing Updates").</summary>
    string Name { get; }

    /// <summary>Tooltip / longer description for the action.</summary>
    string Description { get; }

    /// <summary>
    /// Avalonia resource key or file path for a 16×16 icon.
    /// The UI may show a generic icon when this is null.
    /// </summary>
    string? IconKey { get; }

    /// <summary>
    /// Category used to group actions in the context menu
    /// (e.g. "Updates", "Organise", "Info").
    /// </summary>
    string Category { get; }

    /// <summary>True when the action supports more than one selected ROM.</summary>
    bool SupportsBulk { get; }

    /// <summary>
    /// Returns true if the action can be executed for the given selection.
    /// Called before each menu open — keep it fast (no I/O).
    /// </summary>
    bool CanExecute(IEnumerable<Rom> selectedRoms);

    /// <summary>Performs the action; <paramref name="progress"/> takes 0–100 for the Operations panel.</summary>
    Task ExecuteAsync(IEnumerable<Rom> selectedRoms, IServiceProvider services,
        CancellationToken cancellationToken = default, IProgress<double>? progress = null);
}

/// <summary>An action that can put back what its last run changed (offered as Undo in the Operations panel).</summary>
public interface IUndoableRomAction
{
    /// <summary>True when the last run changed something that <see cref="UndoAsync"/> can restore.</summary>
    bool CanUndo { get; }

    Task UndoAsync(IServiceProvider services, CancellationToken cancellationToken = default);
}

// ──────────────────────────────────────────────────────────────────────────────
// Marker interface for managers that expose context menu actions
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Implement this on any <see cref="ConsoleManager{T}"/> subclass to expose
/// console-specific actions to the Avalonia UI layer.
/// </summary>
public interface IHasContextMenuActions
{
    /// <summary>
    /// Returns all actions available for this console.
    /// The UI will filter them via <see cref="IConsoleRomAction.CanExecute"/>
    /// based on the current selection.
    /// </summary>
    IEnumerable<IConsoleRomAction> GetContextMenuActions();
}

// ──────────────────────────────────────────────────────────────────────────────
// Abstract base — reduces boilerplate for simple actions
// ──────────────────────────────────────────────────────────────────────────────

public abstract class ConsoleRomActionBase : IConsoleRomAction
{
    public abstract string Name { get; }
    public virtual string Description => Name;
    public virtual string? IconKey => null;
    public virtual string Category => "General";
    public virtual bool SupportsBulk => false;

    public virtual bool CanExecute(IEnumerable<Rom> selectedRoms)
        => selectedRoms.Any();

    public abstract Task ExecuteAsync(IEnumerable<Rom> selectedRoms, IServiceProvider services,
        CancellationToken cancellationToken = default, IProgress<double>? progress = null);
}
