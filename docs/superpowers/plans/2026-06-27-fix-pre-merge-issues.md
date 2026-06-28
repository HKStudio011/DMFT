# Pre-Merge Bug Fixes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Fix 5 issues flagged before merge: NRE in Settings page, DB read-before-migrate timing, circular dependency in DI, duplicate config loading code, missing ExtraArgs validation, and silent error swallowing.

**Architecture:** One-line null-safety fix on Settings page; add `InitializeFromDbAsync` to `IYtDlpConfigProvider` and `IDownloadQueue` interfaces so DB reads happen after `Migrate()` instead of during DI construction; remove the broken factory singleton for `DownloadQueue`; extract shared config-reading logic into a Core helper; add input validation for ExtraArgs; replace silent `catch {}` with logged warnings.

**Tech Stack:** .NET 10 / MAUI Blazor / EF Core SQLite

## Global Constraints

- All new code goes in `DMFT.Core` or `DMFT.Shared` except platform-specific entry points.
- `DMFT.DMFT.Services.YtDlpConfigProvider` and `DMFT.Web.Services.YtDlpConfigProvider` are separate classes with slightly different `ExecutablePath` fallback logic — keep both, extract only the DB-reading parts.
- `IDownloadQueue` is registered twice in both entry points (concrete + factory singleton) — the factory has a circular dependency bug, remove it.
- `IYtDlpConfigProvider` interface lives in `DMFT.Core/Services/IYtDlpConfigProvider.cs`.
- `IDownloadQueue` interface lives in `DMFT.Core/Services/DownloadQueue.cs`.
- `AppDbContext` and `IDbContextFactory<AppDbContext>` are in `DMFT.Core.Data`.
- Use `ILogger<T>` for warnings in Web, `System.Diagnostics.Debug.WriteLine` for MAUI.
- Follow existing code style: no XML doc comments, file-scoped namespaces, implicit usings.

---

### Task 1: Fix NRE in `Settings.razor`

**Files:**
- Modify: `DMFT/DMFT.Shared/Pages/Settings.razor`

**Interfaces:**
- Consumes: `_qualityPresets` dictionary with string keys `"best"`, `"1080p"`, `"720p"`, `"480p"`, `"audio"`
- Fixes: `QualityPreset = match.Key` at line 190

**Root cause:** `FirstOrDefault` on a `Dictionary<string, T>` returns `default(KeyValuePair<string, T>)` when no match exists — the `Key` property of a default KVP is `null` for a reference-type key. Stored format strings that don't match any preset key cause a `NullReferenceException`.

- [ ] **Step 1: Add null-coalescing fallback on match.Key**

Edit `Settings.razor` line 189-190:

```csharp
var match = _qualityPresets.FirstOrDefault(p => p.Value.format == formatKey.Value);
QualityPreset = match.Key ?? "best";
```

- [ ] **Step 2: Build & verify**

Build: `dotnet build "DMFT/DMFT.Web/DMFT.Web.csproj" -c Release`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT.Shared/Pages/Settings.razor
git commit -m "fix: null-coalesce QualityPreset fallback when format doesn't match any preset"
```

---

### Task 2: Add `InitializeFromDbAsync` to interfaces and implementations

**Files:**
- Modify: `DMFT.Core/Services/IYtDlpConfigProvider.cs`
- Modify: `DMFT.Core/Services/DownloadQueue.cs` (interface + class)
- Modify: `DMFT/DMFT/Services/YtDlpConfigProvider.cs`
- Modify: `DMFT/DMFT.Web/Services/YtDlpConfigProvider.cs`

**Interfaces:**
- Adds: `Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory)` to `IYtDlpConfigProvider`
- Adds: `Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory)` to `IDownloadQueue`
- Changes: Both `YtDlpConfigProvider` constructors no longer call `LoadConfig()` — DB reads move to `InitializeFromDbAsync`
- Changes: `DownloadQueue` gets `MaxConcurrent` and `DelayBetweenMs` set via `InitializeFromDbAsync`

- [ ] **Step 1: Add method to `IYtDlpConfigProvider`**

Replace file content:

```csharp
using DMFT.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Core.Services;

public interface IYtDlpConfigProvider
{
    string ExecutablePath { get; }
    string ExtraArguments { get; }
    string OutputTemplate { get; }
    string FormatString { get; }
    Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory);
}
```

- [ ] **Step 2: Add method to `IDownloadQueue` + implement in `DownloadQueue`**

Edit `DMFT.Core/Services/DownloadQueue.cs`:

Add to interface:
```csharp
Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory);
```

Add to class (before `EnqueueDownloadAsync`):
```csharp
public async Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory)
{
    try
    {
        using var db = await dbFactory.CreateDbContextAsync();
        var conc = await db.AppSettings.FindAsync("maxConcurrent");
        if (conc != null && int.TryParse(conc.Value, out var c))
            MaxConcurrent = Math.Max(1, c);

        var delay = await db.AppSettings.FindAsync("delayBetweenMs");
        if (delay != null && int.TryParse(delay.Value, out var d))
            DelayBetweenMs = Math.Max(500, d);
    }
    catch (Exception ex)
    {
        System.Diagnostics.Debug.WriteLine($"[DownloadQueue] Failed to load settings from DB: {ex.Message}");
    }
}
```

- [ ] **Step 3: Remove `LoadConfig` from MAUI `YtDlpConfigProvider` constructor + implement `InitializeFromDbAsync`**

Replace `DMFT/DMFT/Services/YtDlpConfigProvider.cs` with:

```csharp
using DMFT.Core.Data;
using DMFT.Core.Services;
using DMFT.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Services;

public class YtDlpConfigProvider : IYtDlpConfigProvider
{
    public string ExecutablePath { get; }
    public string ExtraArguments { get; private set; } = "--restrict-filenames --no-warnings";
    public string OutputTemplate { get; private set; } = "";
    public string FormatString { get; private set; } = "bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best";

    public YtDlpConfigProvider(IStoragePathProvider storage)
    {
        var ytDlpPath = Path.Combine(storage.GetAppDataPath(), "yt-dlp");
        ExecutablePath = Path.Combine(ytDlpPath, "yt-dlp.exe");
        if (!File.Exists(ExecutablePath))
            ExecutablePath = Path.Combine(AppContext.BaseDirectory, "yt-dlp", "yt-dlp.exe");
    }

    public async Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory)
    {
        try
        {
            using var db = await dbFactory.CreateDbContextAsync();

            var extraArgs = (await db.AppSettings.FindAsync("ytdlp_extra_args"))?.Value;
            if (!string.IsNullOrWhiteSpace(extraArgs))
                ExtraArguments = extraArgs;

            var outputTemplate = (await db.AppSettings.FindAsync("ytdlp_output_template"))?.Value;
            if (!string.IsNullOrWhiteSpace(outputTemplate))
                OutputTemplate = outputTemplate;

            var formatString = (await db.AppSettings.FindAsync("ytdlp_format"))?.Value;
            if (!string.IsNullOrWhiteSpace(formatString))
                FormatString = formatString;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[YtDlpConfigProvider] Failed to load config from DB: {ex.Message}");
        }
    }
}
```

- [ ] **Step 4: Same for Web `YtDlpConfigProvider`**

Replace `DMFT/DMFT.Web/Services/YtDlpConfigProvider.cs` with the same content, except constructor uses `"yt-dlp"` fallback instead of full path:

```csharp
using DMFT.Core.Data;
using DMFT.Core.Services;
using DMFT.Shared.Services;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Web.Services;

public class YtDlpConfigProvider : IYtDlpConfigProvider
{
    public string ExecutablePath { get; }
    public string ExtraArguments { get; private set; } = "--restrict-filenames --no-warnings";
    public string OutputTemplate { get; private set; } = "";
    public string FormatString { get; private set; } = "bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best";

    public YtDlpConfigProvider(IStoragePathProvider storage)
    {
        var ytDlpPath = Path.Combine(storage.GetAppDataPath(), "yt-dlp");
        ExecutablePath = Path.Combine(ytDlpPath, "yt-dlp.exe");
        if (!File.Exists(ExecutablePath))
            ExecutablePath = "yt-dlp";
    }

    public async Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory)
    {
        try
        {
            using var db = await dbFactory.CreateDbContextAsync();

            var extraArgs = (await db.AppSettings.FindAsync("ytdlp_extra_args"))?.Value;
            if (!string.IsNullOrWhiteSpace(extraArgs))
                ExtraArguments = extraArgs;

            var outputTemplate = (await db.AppSettings.FindAsync("ytdlp_output_template"))?.Value;
            if (!string.IsNullOrWhiteSpace(outputTemplate))
                OutputTemplate = outputTemplate;

            var formatString = (await db.AppSettings.FindAsync("ytdlp_format"))?.Value;
            if (!string.IsNullOrWhiteSpace(formatString))
                FormatString = formatString;
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[YtDlpConfigProvider] Failed to load config from DB: {ex.Message}");
        }
    }
}
```

- [ ] **Step 5: Build to verify interface changes**

Build: `dotnet build "DMFT/DMFT.Web/DMFT.Web.csproj" -c Release`
Expected: Build fails because entry points still use old patterns (constructor args changed). This is expected — Task 3 fixes the entry points.

- [ ] **Step 6: Commit**

```bash
git add DMFT.Core/Services/IYtDlpConfigProvider.cs DMFT.Core/Services/DownloadQueue.cs DMFT/DMFT/Services/YtDlpConfigProvider.cs DMFT/DMFT.Web/Services/YtDlpConfigProvider.cs
git commit -m "refactor: add InitializeFromDbAsync to IYtDlpConfigProvider and IDownloadQueue"
```

---

### Task 3: Fix entry points — remove factory singleton, call initializers after Migrate

**Files:**
- Modify: `DMFT/DMFT/MauiProgram.cs`
- Modify: `DMFT/DMFT.Web/Program.cs`

**Interfaces:**
- Consumes: `IYtDlpConfigProvider.InitializeFromDbAsync`, `IDownloadQueue.InitializeFromDbAsync`

- [ ] **Step 1: Update MauiProgram.cs**

Replace the entire `CreateMauiApp` method. The key changes:
- `YtDlpConfigProvider` DI registration loses `IDbContextFactory` parameter (constructor changed in Task 2)
- Remove the `IDownloadQueue` factory singleton block (lines 48-67)
- After `Migrate()`, resolve both services and call `InitializeFromDbAsync`

```csharp
using DMFT.Core.Data;
using DMFT.Core.Services;
using DMFT.Services;
using DMFT.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DMFT;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Platform services
        builder.Services.AddSingleton<IFormFactor, FormFactor>();
        builder.Services.AddSingleton<IStoragePathProvider, StoragePathProvider>();
        builder.Services.AddSingleton<IFolderPicker, FolderPicker>();

        // yt-dlp config
        builder.Services.AddSingleton<IYtDlpConfigProvider, YtDlpConfigProvider>();

        // EF Core + SQLite
        builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var storage = sp.GetRequiredService<IStoragePathProvider>();
            options.UseSqlite($"Data Source={storage.GetDatabasePath()}");
        });

        // Core services
        builder.Services.AddSingleton<DownloadService>();
        builder.Services.AddSingleton<IVideoLinkParser, VideoLinkParser>();
        builder.Services.AddSingleton<IMediaDownloader, YtDlpService>();
        builder.Services.AddSingleton<IYtDlpUpdateService, YtDlpUpdateService>();
        builder.Services.AddSingleton<IDownloadEngine, DownloadEngine>();
        builder.Services.AddSingleton<ITikTokSoundExtractor, TikTokSoundExtractor>();
        builder.Services.AddSingleton<IDownloadQueue, DownloadQueue>();
        builder.Services.AddSingleton<ToastService>();

        // App update
        builder.Services.AddSingleton<IAppUpdateService>(sp =>
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DMFT/2.0");
            return new AppUpdateService(http);
        });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Auto-apply pending EF Core migrations on startup
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using (var context = factory.CreateDbContext())
                {
                    context.Database.Migrate();
                }

                // Load persisted settings from DB after migration
                var config = scope.ServiceProvider.GetRequiredService<IYtDlpConfigProvider>();
                var queue = scope.ServiceProvider.GetRequiredService<IDownloadQueue>();
                Task.WhenAll(
                    config.InitializeFromDbAsync(factory),
                    queue.InitializeFromDbAsync(factory)
                ).GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database migration failed: {ex.Message}");
        }

        return app;
    }
}
```

- [ ] **Step 2: Update Web Program.cs**

Same pattern — remove the `IDownloadQueue` factory singleton block (lines 40-58 in original), simplify `YtDlpConfigProvider` registration, call initializers after `MigrateAsync()`.

```csharp
using DMFT.Core.Data;
using DMFT.Core.Services;
using DMFT.Shared.Services;
using DMFT.Web.Components;
using DMFT.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

// Platform services
builder.Services.AddSingleton<IFormFactor, FormFactor>();
builder.Services.AddSingleton<IStoragePathProvider, StoragePathProvider>();
builder.Services.AddSingleton<IFolderPicker, FolderPicker>();

// yt-dlp config
builder.Services.AddSingleton<IYtDlpConfigProvider, YtDlpConfigProvider>();

// EF Core + SQLite
builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
{
    var storage = sp.GetRequiredService<IStoragePathProvider>();
    options.UseSqlite($"Data Source={storage.GetDatabasePath()}");
});

// Core services
builder.Services.AddSingleton<DownloadService>();
builder.Services.AddSingleton<IVideoLinkParser, VideoLinkParser>();
builder.Services.AddSingleton<IMediaDownloader, YtDlpService>();
builder.Services.AddSingleton<IYtDlpUpdateService, YtDlpUpdateService>();
builder.Services.AddSingleton<IDownloadEngine, DownloadEngine>();
builder.Services.AddSingleton<ITikTokSoundExtractor, TikTokSoundExtractor>();
builder.Services.AddSingleton<IDownloadQueue, DownloadQueue>();
builder.Services.AddSingleton<ToastService>();

// App update
builder.Services.AddSingleton<IAppUpdateService>(sp =>
{
    var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    http.DefaultRequestHeaders.UserAgent.ParseAdd("DMFT/2.0");
    return new AppUpdateService(http);
});

var app = builder.Build();

// Auto-apply pending EF Core migrations on startup
try
{
    using (var scope = app.Services.CreateScope())
    {
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using (var context = factory.CreateDbContext())
        {
            await context.Database.MigrateAsync();
        }

        // Load persisted settings from DB after migration
        var config = scope.ServiceProvider.GetRequiredService<IYtDlpConfigProvider>();
        var queue = scope.ServiceProvider.GetRequiredService<IDownloadQueue>();
        await Task.WhenAll(
            config.InitializeFromDbAsync(factory),
            queue.InitializeFromDbAsync(factory)
        );
    }
}
catch (Exception ex)
{
    var logger = app.Services.GetRequiredService<ILogger<Program>>();
    logger.LogWarning(ex, "Database migration failed (non-fatal)");
}

if (app.Environment.IsDevelopment())
{
    app.UseWebAssemblyDebugging();
}
else
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}
app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(DMFT.Shared._Imports).Assembly,
        typeof(DMFT.Web.Client._Imports).Assembly);

app.Run();
```

- [ ] **Step 3: Build to verify**

Build: `dotnet build "DMFT/DMFT.Web/DMFT.Web.csproj" -c Release`
Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add DMFT/DMFT/MauiProgram.cs DMFT/DMFT.Web/Program.cs
git commit -m "fix: move DB reads after Migrate and remove circular-dep queue factory singleton"
```

---

### Task 4: Extract shared config loader to Core (DRY)

**Files:**
- Create: `DMFT.Core/Services/AppSettingsReader.cs`

**Interfaces:**
- Produces: `static class AppSettingsReader` with `static Task<YtDlpConfigValues> ReadYtDlpConfigAsync(IDbContextFactory<AppDbContext> dbFactory)` and `static Task<QueueSettingsValues> ReadQueueSettingsAsync(IDbContextFactory<AppDbContext> dbFactory)`
- Consumed by: both `YtDlpConfigProvider.InitializeFromDbAsync` and `DownloadQueue.InitializeFromDbAsync`

Currently both `YtDlpConfigProvider.InitializeFromDbAsync` implementations have identical try-catch-log blocks (6 method calls each). Extract the DB-reading part into a shared helper in Core so both providers (and the queue) share one implementation.

- [ ] **Step 1: Create `AppSettingsReader.cs` in Core**

```csharp
using DMFT.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Core.Services;

public static class AppSettingsReader
{
    public static async Task<(string? extraArgs, string? outputTemplate, string? formatString)> ReadYtDlpConfigAsync(IDbContextFactory<AppDbContext> dbFactory)
    {
        try
        {
            using var db = await dbFactory.CreateDbContextAsync();

            var extraArgs = (await db.AppSettings.FindAsync("ytdlp_extra_args"))?.Value;
            var outputTemplate = (await db.AppSettings.FindAsync("ytdlp_output_template"))?.Value;
            var formatString = (await db.AppSettings.FindAsync("ytdlp_format"))?.Value;

            return (extraArgs, outputTemplate, formatString);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppSettingsReader] Failed to read yt-dlp config: {ex.Message}");
            return (null, null, null);
        }
    }

    public static async Task<(int? maxConcurrent, int? delayBetweenMs)> ReadQueueSettingsAsync(IDbContextFactory<AppDbContext> dbFactory)
    {
        try
        {
            using var db = await dbFactory.CreateDbContextAsync();

            var conc = (await db.AppSettings.FindAsync("maxConcurrent"))?.Value;
            var delay = (await db.AppSettings.FindAsync("delayBetweenMs"))?.Value;

            int? maxConcurrent = conc != null && int.TryParse(conc, out var c) ? c : null;
            int? delayBetweenMs = delay != null && int.TryParse(delay, out var d) ? d : null;

            return (maxConcurrent, delayBetweenMs);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppSettingsReader] Failed to read queue settings: {ex.Message}");
            return (null, null);
        }
    }
}
```

- [ ] **Step 2: Simplify both `YtDlpConfigProvider.InitializeFromDbAsync` implementations**

Replace the body of `InitializeFromDbAsync` in both `YtDlpConfigProvider` (MAUI and Web) with:

```csharp
public async Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory)
{
    var (extraArgs, outputTemplate, formatString) = await AppSettingsReader.ReadYtDlpConfigAsync(dbFactory);
    if (!string.IsNullOrWhiteSpace(extraArgs))
        ExtraArguments = extraArgs;
    if (!string.IsNullOrWhiteSpace(outputTemplate))
        OutputTemplate = outputTemplate;
    if (!string.IsNullOrWhiteSpace(formatString))
        FormatString = formatString;
}
```

- [ ] **Step 3: Simplify `DownloadQueue.InitializeFromDbAsync`**

Replace the body of `InitializeFromDbAsync` in `DownloadQueue` with:

```csharp
public async Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory)
{
    var (maxConcurrent, delayBetweenMs) = await AppSettingsReader.ReadQueueSettingsAsync(dbFactory);
    if (maxConcurrent.HasValue)
        MaxConcurrent = Math.Max(1, maxConcurrent.Value);
    if (delayBetweenMs.HasValue)
        DelayBetweenMs = Math.Max(500, delayBetweenMs.Value);
}
```

- [ ] **Step 4: Build to verify**

Build: `dotnet build "DMFT/DMFT.Web/DMFT.Web.csproj" -c Release`
Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add DMFT.Core/Services/AppSettingsReader.cs DMFT.Core/Services/DownloadQueue.cs DMFT/DMFT/Services/YtDlpConfigProvider.cs DMFT/DMFT.Web/Services/YtDlpConfigProvider.cs
git commit -m "refactor: extract shared AppSettingsReader to Core, reduce duplication"
```

---

### Task 5: Add ExtraArgs validation in Settings UI

**Files:**
- Modify: `DMFT/DMFT.Shared/Pages/Settings.razor`

- [ ] **Step 1: Add validation in `SaveSettings` method**

Edit the `SaveSettings` method at line 224 in `Settings.razor`. Add a validation check at the top of the method block:

```csharp
private async Task SaveSettings()
{
    using var db = await DbFactory.CreateDbContextAsync();

    // Validate ExtraArgs
    if (!string.IsNullOrWhiteSpace(ExtraArgs))
    {
        string[] dangerous = { ";", "|", "`", "$", "&&", "||", "\n" };
        foreach (var c in dangerous)
        {
            if (ExtraArgs.Contains(c))
            {
                Toast.Show($"Extra arguments contain invalid character: {c}", ToastLevel.Error);
                return;
            }
        }
    }

    await SetAppSettingAsync(db, "theme", ThemeMode);
    await SetAppSettingAsync(db, "accentColor", AccentColor);
    // ... rest unchanged
```

- [ ] **Step 2: Build to verify**

Build: `dotnet build "DMFT/DMFT.Web/DMFT.Web.csproj" -c Release`
Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT.Shared/Pages/Settings.razor
git commit -m "feat: add input validation for ExtraArgs in Settings UI"
```

---

### Task 6: Build and verify all changes

- [ ] **Step 1: Build Web project**

```bash
dotnet build "DMFT/DMFT.Web/DMFT.Web.csproj" -c Release
```
Expected: 0 errors

- [ ] **Step 2: Build Core project**

```bash
dotnet build "DMFT.Core/DMFT.Core.csproj" -c Release
```
Expected: 0 errors

- [ ] **Step 3: Build Shared project**

```bash
dotnet build "DMFT/DMFT.Shared/DMFT.Shared.csproj" -c Release
```
Expected: 0 errors

- [ ] **Step 4: Verify final git log**

```bash
git log --oneline <start-sha>..HEAD
```
Expected: 5 commits corresponding to Tasks 1-5.

- [ ] **Step 5: Final commit if build fixes needed**

```bash
git add -A
git commit -m "fix: build fixes after pre-merge bug fixes"
```
