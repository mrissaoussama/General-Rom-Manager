namespace RomManagerShared.Base.Database;

// ──────────────────────────────────────────────────────────────────────────────
// Unit of Work
// ──────────────────────────────────────────────────────────────────────────────

/// <summary>
/// Coordinates a set of repository operations that should be committed together.
/// Register as <c>IUnitOfWork</c> (scoped) in DI so that a single <see cref="AppDbContext"/>
/// is shared across all repositories within one request / UI action scope.
/// </summary>
public interface IUnitOfWork : IAsyncDisposable
{
    /// <summary>
    /// Saves all pending changes to the underlying store.
    /// Returns the number of state entries written.
    /// </summary>
    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);

    /// <summary>Begins a database transaction.</summary>
    Task BeginTransactionAsync(CancellationToken cancellationToken = default);

    /// <summary>Commits the active transaction.</summary>
    Task CommitAsync(CancellationToken cancellationToken = default);

    /// <summary>Rolls back the active transaction.</summary>
    Task RollbackAsync(CancellationToken cancellationToken = default);
}
