// Runs every console's parsers (wired exactly like the app) against real files.
// Usage:
//   ParserHarness <ConsoleTypeName> <directory> [maxFiles]
//   ParserHarness all <paths.json> [maxFiles]      paths.json: { "PS4Console": "Y:\roms\PS4", ... }
//   add --library to run the incremental scanner twice (temp DB) and compare timings
//   add --snapshot <file> to save results, --compare <file> to diff against a saved snapshot (exit 2 on diff)
//   add --json to print one JSON document (per file: console, ms, roms, reason) on stdout; progress goes to stderr
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RomManagerShared;
using RomManagerShared.Base;
using Microsoft.EntityFrameworkCore;
using RomManagerShared.Base.Database;
using RomManagerShared.Base.Library;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

if (args.Length < 2)
{
    Console.WriteLine("Usage: ParserHarness <ConsoleTypeName|all> <directory|paths.json> [maxFiles]");
    return 1;
}

// --json: stdout carries only the JSON document.
var stdout = Console.Out;
var jsonOutput = args.Contains("--json");
if (jsonOutput) Console.SetOut(Console.Error);

var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
RomManagerConfiguration.Load(configPath);

var services = new ServiceCollection();
services.AddLogging();
// Console modules are referenced by the harness, so they sit next to it.
ConsoleModules.Load("RomManager.Consoles.*.dll", AppContext.BaseDirectory);
ConsoleModules.Register(services);
services.AddDbContext<AppDbContext>(_ => { }, ServiceLifetime.Scoped);
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
var provider = services.BuildServiceProvider();

var paths = args[0] == "all"
    ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(args[1]))!
    : new Dictionary<string, string> { [args[0]] = args[1] };
var max = args.Length > 2 ? int.Parse(args[2]) : 5;

var consoleTypes = ConsoleModules.ConsoleTypes;

// --library: run the real incremental scanner twice against a throwaway database.
if (args.Contains("--library"))
{
    var dbPath = Path.Combine(Path.GetTempPath(), $"harness-{Guid.NewGuid():N}.db");
    using var db = new AppDbContext(new DbContextOptionsBuilder<AppDbContext>().UseSqlite($"Data Source={dbPath}").Options);
    db.Database.Migrate();
    var scanner = new LibraryScanner(db);
    var scan = typeof(LibraryScanner).GetMethod(nameof(LibraryScanner.ScanAsync))!;
    foreach (var ct in consoleTypes.Where(t => paths.ContainsKey(t.Name)))
    {
        using var scope = provider.CreateScope();
        var mgr = scope.ServiceProvider.GetRequiredService(typeof(ConsoleManager<>).MakeGenericType(ct));
        foreach (var pass in new[] { "first", "second" })
        {
            var sw = Stopwatch.StartNew();
            var task = (Task<List<Rom>>)scan.MakeGenericMethod(ct)
                .Invoke(scanner, [mgr, ct.Name, paths[ct.Name], false, null, CancellationToken.None, null])!;
            var roms = await task;
            Console.WriteLine($"## {ct.Name} {pass} scan: {roms.Count} rom(s) in {sw.ElapsedMilliseconds}ms");
        }
    }
    return 0;
}

string? OptionValue(string name)
{
    var i = Array.IndexOf(args, name);
    return i >= 0 && i + 1 < args.Length ? args[i + 1] : null;
}
var snapshotPath = OptionValue("--snapshot");
var comparePath = OptionValue("--compare");
var results = new SortedDictionary<string, string>(StringComparer.OrdinalIgnoreCase);
var report = new List<object>();

foreach (var ct in consoleTypes)
{
    if (!paths.TryGetValue(ct.Name, out var dir)) continue;
    using var scope = provider.CreateScope();
    object mgr;
    try { mgr = scope.ServiceProvider.GetRequiredService(typeof(ConsoleManager<>).MakeGenericType(ct)); }
    catch (Exception ex) { Console.WriteLine($"## {ct.Name}: DI FAIL {ex.GetBaseException().Message}"); continue; }

    var exec = mgr.GetType().GetProperty("RomParserExecutor")!.GetValue(mgr)!;
    var parsers = (System.Collections.IEnumerable)exec.GetType().GetField("Parsers")!.GetValue(exec)!;
    var exts = ((List<string>)exec.GetType().GetMethod("GetSupportedExtensions")!.Invoke(exec, null)!).Distinct().ToList();
    Console.WriteLine($"## {ct.Name} [{mgr.GetType().Name}] parsers: {string.Join(",", parsers.Cast<object>().Select(p => p.GetType().Name))}");

    var sw = Stopwatch.StartNew();
    List<string> files;
    try { files = FileUtils.GetFilesInDirectoryWithExtensions(dir, exts).Where(f => !f.Contains("$RECYCLE")).Take(max).ToList(); }
    catch (Exception ex) { Console.WriteLine($"   enumerate FAIL {ex.Message}"); continue; }
    Console.WriteLine($"   enumerate {sw.ElapsedMilliseconds}ms, testing {files.Count}");

    var romList = (List<Rom>)mgr.GetType().GetProperty("RomList")!.GetValue(mgr)!;
    var processFile = mgr.GetType().GetMethod("ProcessFile")!;
    foreach (var f in files)
    {
        int before = romList.Count;
        sw.Restart();
        var (task, messages) = await ParseDiagnostics.CaptureAsync(async () =>
        {
            var t = (Task)processFile.Invoke(mgr, [f])!;
            await Task.WhenAny(t, Task.Delay(TimeSpan.FromSeconds(90)));
            return t;
        });
        if (!task.IsCompleted)
        {
            Console.WriteLine($"   TIMEOUT {Path.GetFileName(f)}");
            report.Add(new { console = ct.Name, file = f, ms = sw.ElapsedMilliseconds, error = "timeout" });
            continue;
        }
        if (task.IsFaulted)
        {
            Console.WriteLine($"   THROW {Path.GetFileName(f)}: {task.Exception!.GetBaseException().Message}");
            report.Add(new { console = ct.Name, file = f, ms = sw.ElapsedMilliseconds, error = task.Exception!.GetBaseException().Message });
            continue;
        }
        var added = romList.Skip(before).ToList();
        results[f] = RomSnapshot.Serialize(added);
        report.Add(new
        {
            console = ct.Name,
            file = f,
            ms = sw.ElapsedMilliseconds,
            roms = JsonDocument.Parse(results[f]).RootElement,
            reason = added.Count == 0 ? ParseDiagnostics.Summarize(messages) : null,
        });
        Console.WriteLine($"   {sw.ElapsedMilliseconds,6}ms {Path.GetFileName(f)} -> {added.Count} rom(s)");
        foreach (var r in added.Take(3))
            Console.WriteLine($"          {r.GetType().Name}: {r}");
    }
}
Console.WriteLine($"Parser errors are logged to {RomManagerConfiguration.GetErrorLogPath()}");
if (jsonOutput)
    stdout.WriteLine(JsonSerializer.Serialize(report, new JsonSerializerOptions
    {
        WriteIndented = true,
        DefaultIgnoreCondition = System.Text.Json.Serialization.JsonIgnoreCondition.WhenWritingNull,
    }));

if (snapshotPath is not null)
{
    File.WriteAllText(snapshotPath, JsonSerializer.Serialize(results, new JsonSerializerOptions { WriteIndented = true }));
    Console.WriteLine($"Snapshot written: {snapshotPath} ({results.Count} files)");
}
if (comparePath is not null)
{
    var baseline = JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(comparePath))!;
    int diffs = 0;
    foreach (var (file, json) in results)
    {
        if (!baseline.TryGetValue(file, out var old)) { Console.WriteLine($"NEW     {file}"); diffs++; }
        else if (old != json) { Console.WriteLine($"CHANGED {file}\n  was: {old}\n  now: {json}"); diffs++; }
    }
    foreach (var file in baseline.Keys.Where(k => !results.ContainsKey(k)))
    {
        Console.WriteLine($"MISSING {file}");
        diffs++;
    }
    Console.WriteLine(diffs == 0 ? "Snapshot matches." : $"{diffs} difference(s).");
    return diffs == 0 ? 0 : 2;
}
return 0;
