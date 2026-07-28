# Real-Time Progress via Event (Remove DB Timer) Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the 500ms DB write timer + 5s UI polling timer with a direct C# event from `DownloadEngine` to `Main.razor`, eliminating all periodic timers and unnecessary DB writes for transient progress fields.

**Architecture:** The app is single-process Blazor hybrid — `DownloadEngine` and `Main.razor` live in the same `MauiApp`. Currently progress flows: yt-dlp stdout → YtDlpService → DownloadEngine → (500ms Timer) → EF Core SQLite → (5s Timer) → Main.razor. Replace with: yt-dlp stdout → YtDlpService → DownloadEngine → C# event → Main.razor handler → `InvokeAsync(StateHasChanged)`. Three fields (`Speed`, `EtaSeconds`, `ProgressPercent`) become `[NotMapped]` since they're only meaningful during a live download.

**Tech Stack:** .NET 10 MAUI Blazor hybrid, EF Core SQLite

## Global Constraints

- All changes in the `DMFT/DMFT/` project tree (solution root = `D:\Code\DMFT`)
- `DownloadEngine` is a singleton registered in `MauiProgram.cs`
- No new NuGet packages
- Blazor `InteractiveServer` render mode (event handler must use `InvokeAsync(StateHasChanged)`)

---

## File Structure

| File | Responsibility | Action |
|------|---------------|--------|
| `DMFT/DMFT/Entities/DownloadItem.cs` | Entity model — 3 progress fields become `[NotMapped]` | Modify |
| `DMFT/DMFT/Services/Implements/DownloadService.cs` | DB persistence — remove transient fields from `MoveToHistoryAsync` | Modify |
| `DMFT/DMFT/Services/Implements/DownloadEngine.cs` | Download orchestration — add event, remove `_progressTimer` | Modify |
| `DMFT/DMFT/Services/Interfaces/IDownloadEngine.cs` | Interface — add `event Action<DownloadItem>? OnItemProgress` | Modify (rename from interface+impl file) |
| `DMFT/DMFT/Components/Pages/Main.razor` | Main page — subscribe to event, remove `_timer` + `IDisposable` | Modify |

## Task Breakdown

### Task 1: Mark transient fields [NotMapped] and clean up MoveToHistoryAsync

**Files:**
- Modify: `DMFT/DMFT/Entities/DownloadItem.cs:23-25`
- Modify: `DMFT/DMFT/Services/Implements/DownloadService.cs:63-68`

**Interfaces:**
- Consumes: `DownloadItem` entity (existing fields `Speed`, `EtaSeconds`, `ProgressPercent`)
- Produces: Modified entity with 3 `[NotMapped]` fields; `MoveToHistoryAsync` no longer copies them

- [ ] **Step 1: Add [NotMapped] to Speed, EtaSeconds, ProgressPercent in DownloadItem.cs**

```csharp
// DMFT/DMFT/Entities/DownloadItem.cs
// Add using at top if not present:
// using System.ComponentModel.DataAnnotations.Schema;

[NotMapped]
public double Speed { get; set; }         // line 23
[NotMapped]
public int EtaSeconds { get; set; }       // line 24
[NotMapped]
public int ProgressPercent { get; set; }  // line 25
```

- [ ] **Step 2: Remove transient field copies from MoveToHistoryAsync**

In `DMFT/DMFT/Services/Implements/DownloadService.cs` lines 63-68, remove the three transient field assignments:

```csharp
// Before (lines 63-68):
tracked.Status = item.Status;
tracked.DownloadedBytes = item.DownloadedBytes;
tracked.TotalBytes = item.TotalBytes;
tracked.Speed = item.Speed;
tracked.EtaSeconds = item.EtaSeconds;
tracked.ProgressPercent = item.ProgressPercent;
tracked.CurrentFileName = item.CurrentFileName;

// After:
tracked.Status = item.Status;
tracked.DownloadedBytes = item.DownloadedBytes;
tracked.TotalBytes = item.TotalBytes;
tracked.CurrentFileName = item.CurrentFileName;
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build DMFT/DMFT/DMFT/DMFT.csproj -c Release -f net10.0
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 4: Commit**

```bash
git add DMFT/DMFT/Entities/DownloadItem.cs DMFT/DMFT/Services/Implements/DownloadService.cs
git commit -m "perf: mark Speed/EtaSeconds/ProgressPercent as [NotMapped]"
```

---

### Task 2: Add event to IDownloadEngine interface, remove timer from DownloadEngine

**Files:**
- Modify: `DMFT/DMFT/Services/Implements/DownloadEngine.cs` (interface + class — same file, lines 5-9 and 11-133)

**Interfaces:**
- Consumes: `DownloadItem` entity, `DownloadProgress` from YtDlpService
- Produces: `IDownloadEngine` with new `event Action<DownloadItem>? OnItemProgress`; `DownloadEngine` without `_progressTimer`/`ProgressRefreshMs`; events fire from `HandleProgress`

- [ ] **Step 1: Add event to IDownloadEngine interface**

Replace lines 5-9:
```csharp
public interface IDownloadEngine
{
    Task StartDownloadAsync(DownloadItem item);
    Task CancelDownloadAsync(DownloadItem item);
    event Action<DownloadItem>? OnItemProgress;
}
```

- [ ] **Step 2: Remove timer field and constant from DownloadEngine class**

Remove `private Timer? _progressTimer;` (line 17) and `private const int ProgressRefreshMs = 500;` (line 18).

- [ ] **Step 3: Fire event in HandleProgress**

Replace lines 28-37:
```csharp
private void HandleProgress(DownloadProgress progress)
{
    if (_currentItem == null) return;
    _currentItem.Speed = progress.Speed;
    _currentItem.EtaSeconds = progress.EtaSeconds;
    if (progress.TotalBytes > 0)
        _currentItem.ProgressPercent = (int)((progress.DownloadedBytes * 100) / progress.TotalBytes);
    OnItemProgress?.Invoke(_currentItem);
}
```

- [ ] **Step 4: Remove timer creation and all dispose references in StartDownloadAsync**

Remove the timer block (lines 53-56):
```csharp
// DELETE these lines entirely:
_progressTimer = new Timer(async _ =>
{
    await _downloadService.UpdateDownloadAsync(item);
}, null, ProgressRefreshMs, ProgressRefreshMs);
```

Remove `_progressTimer?.Dispose(); _progressTimer = null;` from success path (lines 105-106) and error path (lines 121-122).

```csharp
// Success path — after await Task.WhenAll(tasks); line 104:
item.Status = StatusCodes.Success;
await _downloadService.MoveToHistoryAsync(item);

// Error path — in catch block:
// ... status assignment ...
await _downloadService.UpdateDownloadAsync(item);
```

Note: `_downloadService.UpdateDownloadAsync(item)` on the error path is still needed — it persists `Status` to DB.

- [ ] **Step 5: Remove timer reference from CancelDownloadAsync**

Replace lines 127-133:
```csharp
public async Task CancelDownloadAsync(DownloadItem item)
{
    await _mediaDownloader.CancelAsync();
}
```

- [ ] **Step 6: Build to verify**

```bash
dotnet build DMFT/DMFT/DMFT/DMFT.csproj -c Release -f net10.0
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 7: Commit**

```bash
git add DMFT/DMFT/Services/Implements/DownloadEngine.cs
git commit -m "feat: add OnItemProgress event, remove _progressTimer from DownloadEngine"
```

---

### Task 3: Wire Main.razor to event, remove UI timer

**Files:**
- Modify: `DMFT/DMFT/Components/Pages/Main.razor` (lines 3-4, 104, 107, 112-118, 120-124)

**Interfaces:**
- Consumes: `IDownloadEngine` (injected), `OnItemProgress` event fires on background thread
- Produces: `Main.razor` without timer, without `IDisposable`, real-time progress via event

- [ ] **Step 1: Add IDownloadEngine inject and remove IDisposable**

Replace lines 3-6:
```razor
@inject DownloadService DownloadSvc
@inject IDownloadQueue Queue
@inject IDbContextFactory<AppDbContext> DbFactory
@inject IDownloadEngine Engine
@rendermode InteractiveRenderSettings.InteractiveServer
```

Remove `@implements IDisposable` from line 4.

- [ ] **Step 2: Remove _timer field**

Remove `private Timer? _timer;` from line 107.

- [ ] **Step 3: Replace OnInitializedAsync and remove Dispose**

Replace lines 112-118:
```csharp
protected override async Task OnInitializedAsync()
{
    await LoadItems();
    Engine.OnItemProgress += item =>
    {
        var match = _items.FirstOrDefault(x => x.Id == item.Id);
        if (match != null)
        {
            match.DownloadedBytes = item.DownloadedBytes;
            match.TotalBytes = item.TotalBytes;
            match.Speed = item.Speed;
            match.EtaSeconds = item.EtaSeconds;
            match.ProgressPercent = item.ProgressPercent;
        }
        _ = InvokeAsync(StateHasChanged);
    };
}
```

Remove entire `public void Dispose() => _timer?.Dispose();` line 118.

- [ ] **Step 4: Build to verify**

```bash
dotnet build DMFT/DMFT/DMFT/DMFT.csproj -c Release -f net10.0
```
Expected: Build succeeded, 0 errors.

- [ ] **Step 5: Commit**

```bash
git add DMFT/DMFT/Components/Pages/Main.razor
git commit -m "feat: replace timer polling with direct event subscription in Main.razor"
```

---

### Task 4: Full solution build and final verification

**Files:** None — verification only.

- [ ] **Step 1: Full solution build**

```bash
dotnet build DMFT.slnx -c Release
```
Expected: Build succeeded, 0 errors (only pre-existing platform CA1416 warnings).

- [ ] **Step 2: Verify no stale references**

```powershell
# Confirm _timer and _progressTimer no longer appear anywhere
Select-String -Path "DMFT/DMFT/**/*.cs", "DMFT/DMFT/**/*.razor" -Pattern "_timer"
# Should return only Main.razor's Engine._timer event subscription (not Timer type)

Select-String -Path "DMFT/DMFT/**/*.cs" -Pattern "_progressTimer"
# Should return empty
```

- [ ] **Step 3: Final commit**

```bash
git add -A
git commit -m "perf: remove DB timer pipeline, use direct event for real-time progress"
```
