using Microsoft.EntityFrameworkCore;
using RomManagerShared.Base;
using RomManagerShared.Base.Library;
using RomManagerShared.Configuration;
using RomManagerShared.Utils;

namespace RomManagerShared.Base.Database;

// ──────────────────────────────────────────────────────────────────────────────
// AppDbContext — the library (one row per file, roms as JSON snapshots) and computed hashes.
// Nothing console-specific: console modules can be added or removed without schema changes.
// ──────────────────────────────────────────────────────────────────────────────

public class AppDbContext : DbContext, IUnitOfWork
{
    // ── Constructors ───────────────────────────────────────────────────────

    /// <summary>Default constructor for production/runtime use.</summary>
    public AppDbContext() { }

    /// <summary>Constructor for testing / factory scenarios with pre-configured options.</summary>
    public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

    // ── Shared / cross-platform sets ─────────────────────────────────────

    public DbSet<RomHash> RomHashes { get; set; } = null!;
    public DbSet<RomHashProperty> RomHashProperties { get; set; } = null!;
    public DbSet<LibraryFile> LibraryFiles { get; set; } = null!;
    public DbSet<RomMetadataOverride> RomMetadataOverrides { get; set; } = null!;

    // ── Configuration ─────────────────────────────────────────────────────

    protected override void OnConfiguring(DbContextOptionsBuilder options)
    {
        if (options.IsConfigured) return; // respect factory-supplied options in tests

        var dbPath = GetDbPath();

        // Ensure the directory exists before SQLite tries to create the file
        try
        {
            var dir = Path.GetDirectoryName(dbPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);
        }
        catch { /* best-effort; SQLite will emit a clearer error if path is unavailable */ }

        options.UseSqlite($"Data Source={dbPath}");
    }

    public static string GetDbPath()
    {
        try
        {
            return RomManagerConfiguration.GetSqliteDBPath();
        }
        catch
        {
            // Fallback when config is not yet loaded (e.g. design-time migrations)
            return Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "GeneralRomManager", "Data", "roms.db");
        }
    }

    // ── Model creation ────────────────────────────────────────────────────

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<LibraryFile>()
            .HasIndex(f => new { f.Console, f.Path })
            .IsUnique();
        modelBuilder.Entity<RomMetadataOverride>()
            .HasIndex(o => new { o.Console, o.RomKey, o.RomType })
            .IsUnique();

        base.OnModelCreating(modelBuilder);
    }

    // ── IUnitOfWork ───────────────────────────────────────────────────────

    private Microsoft.EntityFrameworkCore.Storage.IDbContextTransaction? _currentTransaction;

    public async Task BeginTransactionAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is not null)
            throw new InvalidOperationException("A transaction is already in progress.");
        _currentTransaction = await Database.BeginTransactionAsync(cancellationToken);
    }

    public async Task CommitAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null) throw new InvalidOperationException("No active transaction.");
        try
        {
            await SaveChangesAsync(cancellationToken);
            await _currentTransaction.CommitAsync(cancellationToken);
        }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public async Task RollbackAsync(CancellationToken cancellationToken = default)
    {
        if (_currentTransaction is null) throw new InvalidOperationException("No active transaction.");
        try { await _currentTransaction.RollbackAsync(cancellationToken); }
        finally
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
    }

    public override async ValueTask DisposeAsync()
    {
        if (_currentTransaction is not null)
        {
            await _currentTransaction.DisposeAsync();
            _currentTransaction = null;
        }
        await base.DisposeAsync();
        GC.SuppressFinalize(this);
    }
}

