# InHistory Flag + UI Button Overhaul Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace status-based main/history filtering with an explicit `InHistory` boolean flag, update the download service queries accordingly, and add missing UI buttons (Cancel, Retry, Remove, Clear, Download All, Clear All) to Main.razor.

**Architecture:** Single `DownloadItems` table with new `InHistory` (bool) column. Main page queries `InHistory == false`; History page queries `InHistory == true`. A one-time data migration in `MauiProgram.cs` sets `InHistory = true` on existing Canceled (3) and Success (4) items. The Main page UI adds 4 new buttons per card (Cancel/Retry/Remove/Clear) and 2 batch buttons in the toolbar (Download All, Clear All).

**Tech Stack:** .NET 10 + MAUI Blazor Hybrid + EF Core + SQLite

## Global Constraints

- Target framework: `net10.0;net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0-windows10.0.19041.0`
- Entity framework: EF Core + SQLite via `IDbContextFactory<AppDbContext>`
- Status codes: `StatusCodes` static class (`New=0`, `Waiting=1`, `Downloading=2`, `Canceled=3`, `Success=4`, `Error=99`, `VideoAudioOriginError=100`, `VideoError=101`, `AudioOriginError=102`, `AudioOnlyError=103`)
- DI: all services registered as `Singleton` in `MauiProgram.cs`
- Toast: custom `ToastService` (not MAUI CommunityToolkit)
- Progress: real-time via `IDownloadEngine.OnItemProgress` event (not DB timer)

---
### Task 1: Add `InHistory` Column + EF Migration + Startup Data Migration

**Files:**
- Modify: `DMFT/DMFT/Entities/DownloadItem.cs` (add property)
- Modify: `DMFT/DMFT/MauiProgram.cs` (add startup data migration)
- Create: EF migration via `dotnet ef migrations add`

**Interfaces:**
- Consumes: `DownloadItem.Id`, `DownloadItem.Status`, `AppDbContext.Database.Migrate()`
- Produces: `DownloadItem.InHistory` (bool property), migrated database with existing items flagged

- [ ] **Step 1: Add `InHistory` property to `DownloadItem.cs`**

Add after `CurrentFileName` (line 29):

```csharp
    public bool InHistory { get; set; }
````

The file at `DMFT/DMFT/Entities/DownloadItem.cs` should now have this block around lines 28-30:

```csharp
    [NotMapped]
    public int ProgressPercent { get; set; }
    public string CurrentFileName { get; set; } = string.Empty;
    public bool InHistory { get; set; }
```

- [ ] **Step 2: Create EF migration**

Run from solution root:

```bash
dotnet ef migrations add AddInHistory --project DMFT/DMFT/DMFT/DMFT.csproj
```

Expected output: `Done. To undo this action, use 'ef migrations remove'`

Verify the generated migration file exists in `DMFT/DMFT/Data/Migrations/` and contains:

```csharp
protected override void Up(MigrationBuilder migrationBuilder)
{
    migrationBuilder.AddColumn<bool>(
        name: "InHistory",
        table: "DownloadItems",
        type: "INTEGER",
        nullable: false,
        defaultValue: false);
}
```

- [ ] **Step 3: Add startup data migration in `MauiProgram.cs`**

Inside the existing database startup block (lines 86-99), after `context.Database.Migrate()` and before the settings init, add a one-time data fixup for items with old status-based history:

```csharp
                using (var context = factory.CreateDbContext())
                {
                    context.Database.Migrate();
                }

                // === Data migration: set InHistory for legacy Canceled/Success items ===
                using (var migrateCtx = factory.CreateDbContext())
                {
                    var legacyHistorical = migrateCtx.DownloadItems
                        .Where(x => !x.InHistory && (x.Status == 3 || x.Status == 4))
                        .ToList();
                    if (legacyHistorical.Count > 0)
                    {
                        foreach (var item in legacyHistorical)
                            item.InHistory = true;
                        migrateCtx.SaveChanges();
                    }
                }
```

Place this block between the `Migrate()` call and the existing `settings.InitAsync()` call. The full block (lines 84-103) will look like:

```csharp
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

                // Data migration: set InHistory for legacy Canceled/Success items
                using (var migrateCtx = factory.CreateDbContext())
                {
                    var legacyHistorical = migrateCtx.DownloadItems
                        .Where(x => !x.InHistory && (x.Status == 3 || x.Status == 4))
                        .ToList();
                    if (legacyHistorical.Count > 0)
                    {
                        foreach (var item in legacyHistorical)
                            item.InHistory = true;
                        migrateCtx.SaveChanges();
                    }
                }

                var settings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();
                settings.InitAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database migration failed: {ex.Message}");
        }
```

Note: we create a separate `migrateCtx` because `context` (from the Migrate() call) might have a different lifetime or tracking state.

- [ ] **Step 4: Build to verify**

```bash
dotnet build DMFT.slnx -c Release -f net10.0
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add DMFT/DMFT/DMFT/Entities/DownloadItem.cs DMFT/DMFT/DMFT/MauiProgram.cs
git add DMFT/DMFT/DMFT/Data/Migrations/
git commit -m "feat: add InHistory flag with EF migration and startup data migration"
```

---
### Task 2: Update `DownloadService` Filters and Methods

**Files:**
- Modify: `DMFT/DMFT/Services/Implements/DownloadService.cs`

**Interfaces:**
- Consumes: `DownloadItem.InHistory`, `StatusCodes` constants
- Produces: `GetMainLinksAsync()` — returns `InHistory == false`, `GetHistoryAsync()` — returns `InHistory == true`, `MoveToHistoryAsync(item)` — sets `InHistory = true`, `ClearDownloadsAsync(filter?)` — default filter uses `Status == 0 && !InHistory`

- [ ] **Step 1: Change `GetMainLinksAsync` filter**

Current (line 22): `.Where(x => x.Status < 4)`
Change to:

```csharp
            .Where(x => !x.InHistory)
```

Result block (lines 18-25):

```csharp
    public async Task<List<DownloadItem>> GetMainLinksAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.DownloadItems
            .Where(x => !x.InHistory)
            .OrderBy(x => x.Time)
            .ToListAsync();
    }
```

- [ ] **Step 2: Change `GetHistoryAsync` filter**

Current (line 31): `.Where(x => x.Status == 4 || x.Status == 3 || x.Status >= 99)`
Change to:

```csharp
            .Where(x => x.InHistory)
```

Result block (lines 27-34):

```csharp
    public async Task<List<DownloadItem>> GetHistoryAsync()
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        return await db.DownloadItems
            .Where(x => x.InHistory)
            .OrderByDescending(x => x.Time)
            .ToListAsync();
    }
```

- [ ] **Step 3: Update `MoveToHistoryAsync` to set `InHistory = true`**

Add `tracked.InHistory = true;` after `tracked.CurrentFileName = item.CurrentFileName;` (line 74):

```csharp
    public async Task MoveToHistoryAsync(DownloadItem item)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tracked = await db.DownloadItems.FindAsync(item.Id);
        if (tracked != null)
        {
            tracked.Status = item.Status;
            tracked.DownloadedBytes = item.DownloadedBytes;
            tracked.TotalBytes = item.TotalBytes;
            tracked.CurrentFileName = item.CurrentFileName;
            tracked.InHistory = true;
            await db.SaveChangesAsync();
        }
    }
```

- [ ] **Step 4: Add `CancelDownloadAsync` method**

This method sets an item's status to Canceled and moves it to history. Add after `MoveToHistoryAsync` (after line 77):

```csharp
    public async Task CancelDownloadAsync(DownloadItem item)
    {
        using var db = await _dbFactory.CreateDbContextAsync();
        var tracked = await db.DownloadItems.FindAsync(item.Id);
        if (tracked != null)
        {
            tracked.Status = StatusCodes.Canceled;
            tracked.InHistory = true;
            await db.SaveChangesAsync();
        }
    }
```

- [ ] **Step 5: Update `ClearDownloadsAsync` default filter**

The method signature stays the same but the callers that pass no filter will now target only `Status == 0 && !InHistory` items. No code change needed in this method — the filter is passed by the caller. In Task 3, the "Clear All" handler will pass the correct filter.

- [ ] **Step 6: Build to verify**

```bash
dotnet build DMFT.slnx -c Release -f net10.0
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 7: Commit**

```bash
git add DMFT/DMFT/DMFT/Services/Implements/DownloadService.cs
git commit -m "feat: update DownloadService filters to use InHistory, add CancelDownloadAsync"
```

---
### Task 3: Update Main.razor UI — Toolbar + Per-Item Buttons

**Files:**
- Modify: `DMFT/DMFT/Components/Pages/Main.razor`

**Interfaces:**
- Consumes: `DownloadService.GetMainLinksAsync()`, `DownloadService.UpdateDownloadAsync(item)`, `DownloadService.UpdateDownloadAllAsync(items)`, `DownloadService.DeleteDownloadAsync(id)`, `DownloadService.ClearDownloadsAsync(filter)`, `DownloadService.CancelDownloadAsync(item)`, `DownloadService.MoveToHistoryAsync(item)`, `IDownloadEngine.CancelDownloadAsync(item)`, `IDownloadQueue.EnqueueDownloadAsync(item)`, `StatusCodes` constants
- Produces: updated Main.razor with 2 toolbar buttons and revised per-card buttons

- [ ] **Step 1: Add "Download All" + "Clear All" to toolbar**

Insert these two buttons after the "Apply to All" button in the "Set All" toolbar (after line 44):

```razor
        <button class="btn btn-sm btn-primary"
                @onclick="DownloadAllAsync">Download All</button>
        <button class="btn btn-sm btn-outline-danger"
                @onclick="ClearAllAsync">Clear All</button>
```

The full toolbar block (lines 29-46) will become:

```razor
    <div class="mb-3 flex items-center gap-3 flex-wrap">
        <span class="text-sm font-bold whitespace-nowrap text-surface">Set All:</span>
        <label class="flex items-center gap-1.5 text-sm cursor-pointer select-none">
            <input type="checkbox" class="w-4 h-4 accent-primary" @bind="_setAllVideo" />
            Video
        </label>
        <label class="flex items-center gap-1.5 text-sm cursor-pointer select-none">
            <input type="checkbox" class="w-4 h-4 accent-primary" @bind="_setAllAudio" />
            Audio
        </label>
        <label class="flex items-center gap-1.5 text-sm cursor-pointer select-none">
            <input type="checkbox" class="w-4 h-4 accent-primary" @bind="_setAllOriginAudio" />
            Origin Audio
        </label>
        <button class="btn btn-outline-primary"
                @onclick="ApplyModeToAll">Apply to All</button>
        <button class="btn btn-sm btn-primary"
                @onclick="DownloadAllAsync">Download All</button>
        <button class="btn btn-sm btn-outline-danger"
                @onclick="ClearAllAsync">Clear All</button>
    </div>
```

- [ ] **Step 2: Replace the per-card button block**

Current block (lines 81-90):

```razor
                <div class="flex gap-2 mt-1">
                    <button class="btn btn-sm btn-primary"
                            @onclick="() => DownloadAsync(item)">Download</button>
                    <button class="btn btn-sm btn-danger"
                            @onclick="() => RemoveItem(item)">Remove</button>
                    @if (!string.IsNullOrEmpty(item.CurrentFileName))
                    {
                        <span class="px-3 py-1.5 rounded text-xs bg-primary-container text-primary">@item.CurrentFileName</span>
                    }
                </div>
```

Replace with:

```razor
                <div class="flex gap-2 mt-1">
                    @if (item.Status == StatusCodes.Downloading)
                    {
                        <button class="btn btn-sm btn-warning"
                                @onclick="() => CancelItemAsync(item)">Cancel</button>
                    }
                    else if (item.Status >= 99)
                    {
                        <button class="btn btn-sm btn-primary"
                                @onclick="() => DownloadAsync(item)">Retry</button>
                        <button class="btn btn-sm btn-secondary"
                                @onclick="() => RemoveItemAsync(item)">Remove</button>
                    }
                    else if (item.Status == StatusCodes.New)
                    {
                        <button class="btn btn-sm btn-primary"
                                @onclick="() => DownloadAsync(item)">Download</button>
                    }
                    <button class="btn btn-sm btn-outline-danger"
                            @onclick="() => ClearItemAsync(item)">Clear</button>
                    @if (!string.IsNullOrEmpty(item.CurrentFileName))
                    {
                        <span class="px-3 py-1.5 rounded text-xs bg-primary-container text-primary">@item.CurrentFileName</span>
                    }
                </div>
```

- [ ] **Step 3: Add new code-behind methods**

Replace the `@code` block (lines 98-208) with the updated version. The full new `@code` block:

```razor
@code {

    private List<DownloadItem> _items = new();
    private AddModal? addModal;
    private LoadingModal? loadingModal;
    private bool _setAllVideo = true;
    private bool _setAllAudio;
    private bool _setAllOriginAudio;

    protected override async Task OnInitializedAsync()
    {
        Engine.OnItemProgress += item => InvokeAsync(() =>
        {
            var existing = _items.FirstOrDefault(x => x.Id == item.Id);
            if (existing != null)
            {
                existing.Speed = item.Speed;
                existing.EtaSeconds = item.EtaSeconds;
                existing.ProgressPercent = item.ProgressPercent;
                StateHasChanged();
            }
        });
        await LoadItems();
    }

    private async Task LoadItems()
    {
        _items = (await DownloadSvc.GetMainLinksAsync()).ToList();
        StateHasChanged();
    }

    private void ShowAddModal() => addModal?.Show();

    private async Task AddUrls(string content)
    {
        if (string.IsNullOrWhiteSpace(content)) return;
        var urls = content.Split('\n', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
        var parser = new VideoLinkParser();
        foreach (var url in urls)
        {
            parser.TryParseVideoId(url, out var videoId);
            var item = new DownloadItem
            {
                Id = Guid.NewGuid(),
                Url = url,
                OriginalUrl = url,
                VideoId = videoId ?? string.Empty,
                Platform = parser.GetPlatform(url).ToString(),
                Status = StatusCodes.New,
                DownloadMode = (int)DownloadMode.Video,
                ProgressPercent = 0,
                Time = DateTime.UtcNow
            };
            await DownloadSvc.AddDownloadAsync(item);
        }
        await LoadItems();
        Toast.Show($"Added {urls.Length} URL(s)", ToastLevel.Success);
    }

    private async Task ApplyModeToAll()
    {
        int mode = 0;
        if (_setAllVideo) mode |= (int)DownloadMode.Video;
        if (_setAllAudio) mode |= (int)DownloadMode.Audio;
        if (_setAllOriginAudio) mode |= (int)DownloadMode.OriginAudio;
        foreach (var item in _items)
            item.DownloadMode = mode;
        await DownloadSvc.UpdateDownloadAllAsync(_items);
        Toast.Show($"Applied mode to all {_items.Count} item(s)", ToastLevel.Info);
    }

    private async Task DownloadAsync(DownloadItem item)
    {
        loadingModal?.Show();
        try
        {
            await Queue.EnqueueDownloadAsync(item);
            Toast.Show($"Downloading: {item.VideoId}", ToastLevel.Success);
        }
        catch (Exception ex)
        {
            Toast.Show($"Error: {ex.Message}", ToastLevel.Error);
        }
        finally
        {
            loadingModal?.Hide();
            await LoadItems();
        }
    }

    private async Task DownloadAllAsync()
    {
        var newItems = _items.Where(x => x.Status == StatusCodes.New).ToList();
        if (newItems.Count == 0)
        {
            Toast.Show("No new items to download", ToastLevel.Info);
            return;
        }
        foreach (var item in newItems)
            await Queue.EnqueueDownloadAsync(item);
        Toast.Show($"Added {newItems.Count} item(s) to queue", ToastLevel.Success);
        await LoadItems();
    }

    private async Task ClearAllAsync()
    {
        await DownloadSvc.ClearDownloadsAsync(x => x.Status == StatusCodes.New && !x.InHistory);
        Toast.Show("Cleared all new items", ToastLevel.Info);
        await LoadItems();
    }

    private async Task CancelItemAsync(DownloadItem item)
    {
        await Engine.CancelDownloadAsync(item);
        await DownloadSvc.CancelDownloadAsync(item);
        Toast.Show($"Canceled: {item.VideoId}", ToastLevel.Info);
        await LoadItems();
    }

    private async Task RemoveItemAsync(DownloadItem item)
    {
        await DownloadSvc.MoveToHistoryAsync(item);
        Toast.Show($"Moved to history: {item.VideoId}", ToastLevel.Info);
        await LoadItems();
    }

    private async Task ClearItemAsync(DownloadItem item)
    {
        await DownloadSvc.DeleteDownloadAsync(item.Id);
        Toast.Show($"Cleared: {item.VideoId}", ToastLevel.Info);
        await LoadItems();
    }

    private static string GetStatusLabel(int status) => status switch
    {
        0 => "New",
        1 => "Waiting",
        2 => "Downloading",
        3 => "Canceled",
        4 => "Completed",
        99 => "Error",
        100 => "Video + Audio Origin Error",
        101 => "Video Error",
        102 => "Audio Origin Error",
        103 => "Audio Only Error",
        _ => "Unknown"
    };
}
```

Key changes from old code:
- Removed old `RemoveItem` method (which called `DeleteDownloadAsync`)
- Added `DownloadAllAsync` — loops New items and enqueues each
- Added `ClearAllAsync` — calls `ClearDownloadsAsync` with filter for Status==New && !InHistory
- Added `CancelItemAsync` — calls `Engine.CancelDownloadAsync()` then `DownloadSvc.CancelDownloadAsync()`
- Added `RemoveItemAsync` — calls `DownloadSvc.MoveToHistoryAsync()` (sets InHistory=true)
- Added `ClearItemAsync` — calls `DownloadSvc.DeleteDownloadAsync()` (permanent delete)

- [ ] **Step 4: Build to verify**

```bash
dotnet build DMFT.slnx -c Release -f net10.0
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 5: Commit**

```bash
git add DMFT/DMFT/DMFT/Components/Pages/Main.razor
git commit -m "feat: add Cancel/Retry/Remove/Clear/DownloadAll/ClearAll buttons to Main.razor"
```

---
### Task 4: Update History.razor for InHistory

**Files:**
- Modify: `DMFT/DMFT/Components/Pages/History.razor`

**Interfaces:**
- Consumes: `DownloadService.GetHistoryAsync()`, `DownloadService.AddDownloadAsync(item)`, `DownloadService.DeleteDownloadAsync(id)`, `DownloadItem.InHistory`
- Produces: history page that correctly sets `InHistory = false` on retry

- [ ] **Step 1: Update `RetryAsync` to set `InHistory = false`**

Current `RetryAsync` (lines 64-81) creates a new copy of the item but doesn't set `InHistory`. Since `bool` defaults to `false`, it works — but make it explicit. Add one line:

```csharp
    private async Task RetryAsync(DownloadItem item)
    {
        try
        {
            item.Id = Guid.NewGuid();
            item.Status = StatusCodes.New;
            item.InHistory = false;
            item.ProgressPercent = 0;
            item.Time = DateTime.UtcNow;
            item.CurrentFileName = string.Empty;
            await DownloadSvc.AddDownloadAsync(item);
            await Queue.EnqueueDownloadAsync(item);
            Toast.Show($"Retrying: {item.VideoId}", ToastLevel.Info);
        }
        catch (Exception ex)
        {
            Toast.Show($"Error: {ex.Message}", ToastLevel.Error);
        }
    }
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build DMFT.slnx -c Release -f net10.0
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/DMFT/Components/Pages/History.razor
git commit -m "fix: set InHistory=false on history retry"
```

---
### Task 5: Full Solution Build + Final Verification

**Files:** none (verification task)

- [ ] **Step 1: Full solution build**

```bash
dotnet build DMFT.slnx -c Release -f net10.0
```

Expected: `Build succeeded. 0 Error(s)`

- [ ] **Step 2: Verify no stale references to old status-based history filter**

```bash
rg "Status < 4" --include "*.cs" DMFT/DMFT/DMFT/
rg "Status == 4 \|\| Status == 3 \|\| Status >= 99" --include "*.cs" DMFT/DMFT/DMFT/
```

Expected: no matches (both patterns replaced with `!InHistory` and `InHistory` respectively)

- [ ] **Step 3: Verify the `btn-warning` CSS class exists**

The new "Cancel" button uses `btn btn-sm btn-warning`. Check if the Tailwind CSS build includes a `btn-warning` style. Look in the vite-project source for existing btn styles:

```bash
rg "btn-warning" DMFT/DMFT/Components/vite-project/src/ DMFT/DMFT/Components/wwwroot/build/
```

If no match, add the style by editing the theme CSS at `DMFT/DMFT/Components/vite-project/src/css/theme.css`:

```css
.btn-warning {
    background-color: #f59e0b;
    color: #fff;
}
.btn-warning:hover {
    background-color: #d97706;
}
```

Then rebuild frontend:

```bash
cd DMFT/DMFT/Components/vite-project && npm run build
```

- [ ] **Step 4: Final commit**

```bash
git add -A
git commit -m "feat: complete InHistory flag and UI button overhaul"
```
