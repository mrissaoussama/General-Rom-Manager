// Runs every console's parsers (wired exactly like the app) against real files.
// Usage:
//   ParserHarness <ConsoleTypeName> <directory> [maxFiles]
//   ParserHarness all <paths.json> [maxFiles]      paths.json: { "PS4Console": "Y:\roms\PS4", ... }
using System.Diagnostics;
using System.Reflection;
using System.Text.Json;
using Microsoft.Extensions.DependencyInjection;
using RomManagerShared;
using RomManagerShared.Base;
using RomManagerShared.Base.Database;
using RomManagerShared.Configuration;
using RomManagerShared.Interfaces;
using RomManagerShared.Utils;

if (args.Length < 2)
{
    Console.WriteLine("Usage: ParserHarness <ConsoleTypeName|all> <directory|paths.json> [maxFiles]");
    return 1;
}

var configPath = Path.Combine(AppContext.BaseDirectory, "config.json");
RomManagerConfiguration.Load(configPath);

var asm = Assembly.Load("RomManagerShared");
var services = new ServiceCollection();
services.AddLogging();
typeof(AvaloniaUI.App)
    .GetMethod("RegisterConsoleServices", BindingFlags.NonPublic | BindingFlags.Static)!
    .Invoke(null, [services, asm]);
services.AddDbContext<AppDbContext>(_ => { }, ServiceLifetime.Scoped);
services.AddScoped<IUnitOfWork>(sp => sp.GetRequiredService<AppDbContext>());
var provider = services.BuildServiceProvider();

var paths = args[0] == "all"
    ? JsonSerializer.Deserialize<Dictionary<string, string>>(File.ReadAllText(args[1]))!
    : new Dictionary<string, string> { [args[0]] = args[1] };
var max = args.Length > 2 ? int.Parse(args[2]) : 5;

var consoleTypes = asm.GetTypes()
    .Where(t => typeof(GamingConsole).IsAssignableFrom(t) && !t.IsAbstract && !t.IsInterface && t != typeof(GamingConsole));

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
        var task = (Task)processFile.Invoke(mgr, [f])!;
        if (await Task.WhenAny(task, Task.Delay(TimeSpan.FromSeconds(90))) != task)
        {
            Console.WriteLine($"   TIMEOUT {Path.GetFileName(f)}");
            continue;
        }
        if (task.IsFaulted)
        {
            Console.WriteLine($"   THROW {Path.GetFileName(f)}: {task.Exception!.GetBaseException().Message}");
            continue;
        }
        var added = romList.Skip(before).ToList();
        Console.WriteLine($"   {sw.ElapsedMilliseconds,6}ms {Path.GetFileName(f)} -> {added.Count} rom(s)");
        foreach (var r in added.Take(3))
            Console.WriteLine($"          {r.GetType().Name}: {r}");
    }
}
Console.WriteLine($"Parser errors are logged to {RomManagerConfiguration.GetErrorLogPath()}");
return 0;
