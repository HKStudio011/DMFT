# Download Mode Checkboxes Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Convert download mode from a mutually-exclusive `<select>` dropdown to independent checkboxes (Video, Audio, Origin Audio) using `[Flags]` enum, and hide Origin Audio for YouTube non-short.

**Architecture:** Change `DownloadMode` from static int constants to `[Flags]` enum with bit values 1/2/4, allowing any combination. Add `[NotMapped]` bool helper properties on `DownloadItem` for checkbox binding. Rewrite `DownloadEngine` from `switch` to per-flag task collection. Add EF migration to convert old DB int values (0-3) to new bitmask values (1/2/4/5). Update Main.razor to render checkboxes and hide Origin Audio when `item.Platform == "YouTube"`.

**Tech Stack:** .NET 10 / MAUI Blazor / EF Core SQLite

## Global Constraints

- `DownloadMode` enum file is `DMFT.Core/Services/DownloadMode.cs`
- `DownloadItem` entity is `DMFT.Core/Entities/DownloadItem.cs` — DB column is `INTEGER`
- `DownloadEngine` is `DMFT.Core/Services/DownloadEngine.cs`
- UI is `DMFT/DMFT.Shared/Pages/Main.razor`
- Migration files live in `DMFT.Core/Data/Migrations/`
- YouTube non-short detection: `Platform == "YouTube"` (set by `VideoLinkParser.GetPlatform` when URL contains `youtube.com/watch`)
- `IMediaDownloader` interface in `DMFT.Core/Services/YtDlpService.cs:15`: `DownloadAsync(string videoUrl, string outputPath, bool noWatermark)` for video, `DownloadAudioAsync(string videoUrl, string outputPath)` for audio
- Follow existing code style: no XML doc comments, file-scoped namespaces, implicit usings
- No changes needed in DMFT.Old (unreferenced dead code)
- No changes needed in History.razor (doesn't display DownloadMode)

---

### Task 1: Convert DownloadMode to [Flags] enum

**Files:**
- Modify: `DMFT.Core/Services/DownloadMode.cs`

**Interfaces:**
- Produces: `[Flags] enum DownloadMode { None=0, Video=1, Audio=2, OriginAudio=4 }`
- Consumed by: `DownloadItem.DownloadMode` (property type stays `int`, cast at usage), `DownloadEngine` (flag checks)

- [ ] **Step 1: Replace DownloadMode.cs**

```csharp
namespace DMFT.Core.Services;

[Flags]
public enum DownloadMode
{
    None = 0,
    Video = 1,
    Audio = 2,
    OriginAudio = 4,
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build "DMFT.Core/DMFT.Core.csproj" -c Release
```

Expected: 0 errors (only pre-existing NuGet warnings)

- [ ] **Step 3: Commit**

```bash
git add DMFT.Core/Services/DownloadMode.cs
git commit -m "feat: convert DownloadMode to [Flags] enum with bit values"
```

---

### Task 2: Add checkbox helper properties to DownloadItem

**Files:**
- Modify: `DMFT.Core/Entities/DownloadItem.cs`

**Interfaces:**
- Consumes: `DownloadMode` enum (Video=1/Audio=2/OriginAudio=4)
- Produces: `DownloadItem.DownloadVideo` (bool), `DownloadItem.DownloadAudio` (bool), `DownloadItem.DownloadOriginAudio` (bool) — `[NotMapped]` properties for checkbox binding

- [ ] **Step 1: Replace DownloadItem.cs**

The entity already has `public int DownloadMode { get; set; }`. Keep it as `int` for EF compatibility but add `[NotMapped]` helper properties:

```csharp
using System.ComponentModel.DataAnnotations.Schema;
using DMFT.Core.Services;

namespace DMFT.Core.Entities;

public class DownloadItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public string Platform { get; set; } = "Unknown";
    public int Status { get; set; }
    public DateTime Time { get; set; } = DateTime.UtcNow;
    public string VideoId { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string TitleDescription { get; set; } = string.Empty;
    public string OriginalSoundUrl { get; set; } = string.Empty;
    public string OriginalSoundName { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;
    public int DownloadMode { get; set; }
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
    public double Speed { get; set; }
    public int EtaSeconds { get; set; }
    public int ProgressPercent { get; set; }
    public string CurrentFileName { get; set; } = string.Empty;

    [NotMapped]
    public bool DownloadVideo
    {
        get => (DownloadMode & (int)Services.DownloadMode.Video) != 0;
        set => SetFlag((int)Services.DownloadMode.Video, value);
    }

    [NotMapped]
    public bool DownloadAudio
    {
        get => (DownloadMode & (int)Services.DownloadMode.Audio) != 0;
        set => SetFlag((int)Services.DownloadMode.Audio, value);
    }

    [NotMapped]
    public bool DownloadOriginAudio
    {
        get => (DownloadMode & (int)Services.DownloadMode.OriginAudio) != 0;
        set => SetFlag((int)Services.DownloadMode.OriginAudio, value);
    }

    private void SetFlag(int bit, bool on)
    {
        DownloadMode = on ? DownloadMode | bit : DownloadMode & ~bit;
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build "DMFT.Core/DMFT.Core.csproj" -c Release
```

Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add DMFT.Core/Entities/DownloadItem.cs
git commit -m "feat: add [NotMapped] bool properties for checkbox binding"
```

---

### Task 3: Rewrite DownloadEngine for flag-based mode

**Files:**
- Modify: `DMFT.Core/Services/DownloadEngine.cs`

**Interfaces:**
- Consumes: `DownloadMode` [Flags] enum, `DownloadItem` with int DownloadMode, `IMediaDownloader` (DownloadAsync/DownloadAudioAsync), `StatusCodes`
- Produces: flag-checked download logic that runs all selected options

- [ ] **Step 1: Replace StartDownloadAsync method**

Keep the class structure, interface, constructor, and CancelDownloadAsync unchanged. Replace only the `StartDownloadAsync` body. The key changes:
- Check flags with `HasFlag` instead of `switch`
- Collect tasks in `List<Task>` and run with `Task.WhenAll`
- Produce deterministic output file names for each task
- Simplify error status assignment

```csharp
using DMFT.Core.Entities;

namespace DMFT.Core.Services;

public interface IDownloadEngine
{
    Task StartDownloadAsync(DownloadItem item);
    Task CancelDownloadAsync(DownloadItem item);
}

public class DownloadEngine : IDownloadEngine
{
    private readonly IMediaDownloader _mediaDownloader;
    private readonly DownloadService _downloadService;
    private readonly ITikTokSoundExtractor _soundExtractor;
    private DownloadItem? _currentItem;
    private Timer? _progressTimer;
    private const int ProgressRefreshMs = 500;

    public DownloadEngine(IMediaDownloader mediaDownloader, DownloadService downloadService, ITikTokSoundExtractor soundExtractor)
    {
        _mediaDownloader = mediaDownloader;
        _downloadService = downloadService;
        _soundExtractor = soundExtractor;
        _mediaDownloader.OnProgress += HandleProgress;
    }

    private void HandleProgress(DownloadProgress progress)
    {
        if (_currentItem == null) return;
        _currentItem.DownloadedBytes = progress.DownloadedBytes;
        _currentItem.TotalBytes = progress.TotalBytes;
        _currentItem.Speed = progress.Speed;
        _currentItem.EtaSeconds = progress.EtaSeconds;
        if (progress.TotalBytes > 0)
            _currentItem.ProgressPercent = (int)((progress.DownloadedBytes * 100) / progress.TotalBytes);
    }

    public async Task StartDownloadAsync(DownloadItem item)
    {
        if (item == null) return;
        _currentItem = item;
        item.Status = StatusCodes.Downloading;
        item.DownloadedBytes = 0;
        item.TotalBytes = 0;
        item.Speed = 0;
        item.EtaSeconds = 0;
        item.ProgressPercent = 0;
        await _downloadService.UpdateDownloadAsync(item);

        var mode = (DownloadMode)item.DownloadMode;

        _progressTimer = new Timer(async _ =>
        {
            await _downloadService.UpdateDownloadAsync(item);
        }, null, ProgressRefreshMs, ProgressRefreshMs);

        try
        {
            string videoDest = Path.Combine(item.SaveLocation, $"{item.VideoId}_video.mp4");
            string audioDest = Path.Combine(item.SaveLocation, $"{item.VideoId}_audio.mp3");
            string originDest = Path.Combine(item.SaveLocation, $"{item.VideoId}_origin.mp3");

            if (mode.HasFlag(DownloadMode.OriginAudio))
            {
                var (soundName, soundUrl) = await _soundExtractor.GetOriginalSoundAsync(item.Url);
                if (!string.IsNullOrWhiteSpace(soundUrl))
                {
                    item.OriginalSoundName = soundName ?? "";
                    item.OriginalSoundUrl = soundUrl;
                    item.OriginalUrl = item.Url;
                    await _downloadService.UpdateDownloadAsync(item);
                }
            }

            var tasks = new List<Task>();

            if (mode.HasFlag(DownloadMode.Video))
            {
                item.CurrentFileName = Path.GetFileName(videoDest);
                tasks.Add(_mediaDownloader.DownloadAsync(item.Url, videoDest, noWatermark: true));
            }

            if (mode.HasFlag(DownloadMode.Audio))
            {
                item.CurrentFileName = Path.GetFileName(audioDest);
                tasks.Add(_mediaDownloader.DownloadAudioAsync(item.Url, audioDest));
            }

            if (mode.HasFlag(DownloadMode.OriginAudio) && !string.IsNullOrWhiteSpace(item.OriginalSoundUrl))
            {
                item.CurrentFileName = Path.GetFileName(originDest);
                tasks.Add(_mediaDownloader.DownloadAudioAsync(item.OriginalSoundUrl, originDest));
            }

            if (tasks.Count == 0)
                throw new Exception("No download tasks selected");

            await Task.WhenAll(tasks);

            item.Status = StatusCodes.Success;
            _progressTimer?.Dispose();
            _progressTimer = null;
            await _downloadService.MoveToHistoryAsync(item);
        }
        catch (Exception)
        {
            if (mode.HasFlag(DownloadMode.Video))
                item.Status = StatusCodes.VideoError;
            else if (mode.HasFlag(DownloadMode.Audio))
                item.Status = StatusCodes.AudioOnlyError;
            else if (mode.HasFlag(DownloadMode.OriginAudio))
                item.Status = StatusCodes.AudioOriginError;
            else
                item.Status = StatusCodes.Error;
            _progressTimer?.Dispose();
            _progressTimer = null;
            await _downloadService.UpdateDownloadAsync(item);
        }
    }

    public async Task CancelDownloadAsync(DownloadItem item)
    {
        _progressTimer?.Dispose();
        _progressTimer = null;
        await _mediaDownloader.CancelAsync();
    }
}
```

- [ ] **Step 2: Build to verify**

```bash
dotnet build "DMFT/DMFT.Web/DMFT.Web.csproj" -c Release
```

Expected: 0 errors

- [ ] **Step 3: Commit**

```bash
git add DMFT.Core/Services/DownloadEngine.cs
git commit -m "feat: rewrite engine for flag-based download mode, run multiple tasks per item"
```

---

### Task 4: Update Main.razor with checkboxes and platform-aware visibility

**Files:**
- Modify: `DMFT/DMFT.Shared/Pages/Main.razor`

**Interfaces:**
- Consumes: `DownloadItem.DownloadVideo`, `DownloadItem.DownloadAudio`, `DownloadItem.DownloadOriginAudio` ([NotMapped] bools)
- Consumes: `DownloadMode` flags enum
- Consumes: `item.Platform` (string: "YouTube" → hide Origin Audio)

- [ ] **Step 1: Replace the "Set All Mode" section (lines 31-42)**

Old: `<select>` with 4 options + "Apply to All" button.

New: 3 checkboxes + "Apply to All" button. Use local bool fields `_setAllVideo`, `_setAllAudio`, `_setAllOriginAudio` instead of `_selectedModeForAll`.

Replace lines 31-42:

```razor
    <div class="mb-3 flex items-center gap-3 flex-wrap">
        <span class="text-sm font-bold whitespace-nowrap text-on-surface">Set All:</span>
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
        <button class="px-3 py-1 rounded text-xs bg-primary-container text-on-primary-container hover:brightness-110 border-0 cursor-pointer"
                @onclick="ApplyModeToAll">Apply to All</button>
    </div>
```

- [ ] **Step 2: Replace the per-item mode section (lines 57-66)**

Old: `<select>` with 4 options per item.

New: 3 checkboxes per item, hiding Origin Audio when `item.Platform == "YouTube"`.

Replace lines 57-66:

```razor
                <div class="flex items-center gap-3 flex-wrap">
                    <span class="text-xs text-on-surface-variant">Mode:</span>
                    <label class="flex items-center gap-1 text-xs cursor-pointer select-none">
                        <input type="checkbox" class="w-3.5 h-3.5 accent-primary" @bind="item.DownloadVideo" />
                        Video
                    </label>
                    <label class="flex items-center gap-1 text-xs cursor-pointer select-none">
                        <input type="checkbox" class="w-3.5 h-3.5 accent-primary" @bind="item.DownloadAudio" />
                        Audio
                    </label>
                    @if (item.Platform != "YouTube")
                    {
                        <label class="flex items-center gap-1 text-xs cursor-pointer select-none">
                            <input type="checkbox" class="w-3.5 h-3.5 accent-primary" @bind="item.DownloadOriginAudio" />
                            Origin Audio
                        </label>
                    }
                </div>
```

- [ ] **Step 3: Update the code block**

Replace `_selectedModeForAll` field and `ApplyModeToAll` method in the `@code` block:

Old (lines 96, 140-146):
```csharp
private int _selectedModeForAll = DownloadMode.Video;
...
private void ApplyModeToAll()
{
    foreach (var item in _items)
    {
        item.DownloadMode = _selectedModeForAll;
    }
    Toast.Show($"Applied mode to all {_items.Count} item(s)", ToastLevel.Info);
}
```

New:
```csharp
private bool _setAllVideo = true;
private bool _setAllAudio;
private bool _setAllOriginAudio;
...
private void ApplyModeToAll()
{
    int mode = 0;
    if (_setAllVideo) mode |= (int)DownloadMode.Video;
    if (_setAllAudio) mode |= (int)DownloadMode.Audio;
    if (_setAllOriginAudio) mode |= (int)DownloadMode.OriginAudio;
    foreach (var item in _items)
    {
        item.DownloadMode = mode;
    }
    Toast.Show($"Applied mode to all {_items.Count} item(s)", ToastLevel.Info);
}
```

Also update the `AddUrls` method — on line 130 the default mode is `DownloadMode.Video`. With the new enum, cast:
```csharp
DownloadMode = (int)DownloadMode.Video,
```

- [ ] **Step 4: Build to verify**

```bash
dotnet build "DMFT/DMFT.Web/DMFT.Web.csproj" -c Release
```

Expected: 0 errors

- [ ] **Step 5: Commit**

```bash
git add "DMFT/DMFT.Shared/Pages/Main.razor"
git commit -m "feat: replace mode select with checkboxes, hide Origin Audio for YouTube"
```

---

### Task 5: Add EF migration to convert old DB values

**Files:**
- Create: `DMFT.Core/Data/Migrations/20260628_ConvertDownloadMode.Designer.cs`
- Create: `DMFT.Core/Data/Migrations/20260628_ConvertDownloadMode.cs`
- Modify: `DMFT.Core/Data/Migrations/AppDbContextModelSnapshot.cs` (auto-updated by EF tools)

**Interfaces:**
- Consumes: existing `DownloadItems.DownloadMode` column with old int values (0/1/2/3)
- Produces: updated column with new bitmask values (1/2/4/5)

**Old→New mapping:**
| Old | Old meaning | New | New meaning |
|---|---|---|---|
| 0 | Video | 1 | Video |
| 1 | AudioOnly | 2 | Audio |
| 2 | AudioOriginOnly | 4 | OriginAudio |
| 3 | VideoAndAudioOrigin | 5 | Video \| OriginAudio |

- [ ] **Step 1: Create migration using EF tool**

Run the migration scaffold. This generates an empty migration (no schema change since column type stays INTEGER):

```bash
dotnet ef migrations add ConvertDownloadMode --project DMFT.Core --startup-project DMFT/DMFT.Web
```

If `dotnet ef` is not installed, run first:
```bash
dotnet tool install --global dotnet-ef
```

- [ ] **Step 2: Add SQL UPDATE statements to the migration Up() method**

Edit the generated `20260628_ConvertDownloadMode.cs` to add SQL that converts old values:

```csharp
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace DMFT.Core.Data.Migrations
{
    public partial class ConvertDownloadMode : Migration
    {
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Old: 0=Video, 1=AudioOnly, 2=AudioOriginOnly, 3=VideoAndAudioOrigin
            // New: 1=Video, 2=Audio, 4=OriginAudio, 5=Video|OriginAudio
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 1 WHERE DownloadMode = 0");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 2 WHERE DownloadMode = 1");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 4 WHERE DownloadMode = 2");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 5 WHERE DownloadMode = 3");
        }

        protected override void Down(MigrationBuilder migrationBuilder)
        {
            // Reverse: map new values back to old
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 0 WHERE DownloadMode = 1");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 1 WHERE DownloadMode = 2");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 2 WHERE DownloadMode = 4");
            migrationBuilder.Sql("UPDATE DownloadItems SET DownloadMode = 3 WHERE DownloadMode = 5");
        }
    }
}
```

- [ ] **Step 3: Build to verify**

```bash
dotnet build "DMFT/DMFT.Web/DMFT.Web.csproj" -c Release
```

Expected: 0 errors

- [ ] **Step 4: Commit**

```bash
git add DMFT.Core/Data/Migrations/
git commit -m "feat: add migration to convert old DownloadMode values to new bitmask"
```

---

### Task 6: Final build and verification

- [ ] **Step 1: Build all projects**

```bash
dotnet build "DMFT/DMFT.Web/DMFT.Web.csproj" -c Release
dotnet build "DMFT.Core/DMFT.Core.csproj" -c Release
dotnet build "DMFT/DMFT.Shared/DMFT.Shared.csproj" -c Release
```

Expected: 0 errors each

- [ ] **Step 2: Verify commit history**

```bash
git log --oneline -10
```

Expected: 6 new commits for Tasks 1-5, starting with the DownloadMode enum change.

- [ ] **Step 3: Verify no remaining references to old constants**

```bash
rg "DownloadMode\.(Video|AudioOnly|AudioOriginOnly|VideoAndAudioOrigin)" DMFT.Core/ DMFT/DMFT.Shared/ DMFT/DMFT.Web/ DMFT/DMFT/ --type cs
```

Expected: 0 matches (all old constants replaced)

- [ ] **Step 4: Final commit if build fixes needed**

```bash
git add -A
git commit -m "fix: post-migration corrections"
```
