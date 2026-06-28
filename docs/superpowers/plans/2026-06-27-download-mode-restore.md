# Multi-Mode Download Restore Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restore the 4 download modes (Video, AudioOnly, AudioOriginOnly, VideoAndAudioOrigin) that were lost in the v2 `DownloadEngine` rewrite.

**Architecture:** The v2 `DownloadEngine` currently downloads video only, ignoring `DownloadMode`. We add a constants file for mode values, extend the engine with a switch on mode (using `IMediaDownloader.DownloadAudioAsync` and the existing `ITikTokSoundExtractor` for original sound), add a mode selector dropdown to the Blazor UI, and wire the extractor into DI.

**Tech Stack:** .NET 10 / MAUI Blazor / Playwright (for TikTok sound extraction) / yt-dlp

## Global Constraints

- All new code goes in `DMFT.Core` or `DMFT.Shared` — the old `DMFT.Old` project is a reference only, not to be modified.
- `DownloadMode` is stored as `int` on `DownloadItem` — no EF migration needed.
- Use existing `IMediaDownloader` interface (`DownloadAsync` for video, `DownloadAudioAsync` for audio).
- `ITikTokSoundExtractor` interface already exists in `DMFT.Core/Services/TikTokSoundExtractor.cs`.
- `StatusCodes` in `DMFT.Core/Services/StatusCodes.cs` already has error codes for all 4 modes.
- Follow existing code style: no XML doc comments, file-scoped namespaces, implicit usings.

---

### Task 1: Create `DownloadMode` constants

**Files:**
- Create: `DMFT.Core/Services/DownloadMode.cs`

**Interfaces:**
- Produces: `DownloadMode.Video` (0), `DownloadMode.AudioOnly` (1), `DownloadMode.AudioOriginOnly` (2), `DownloadMode.VideoAndAudioOrigin` (3) — all `public const int`

- [ ] **Step 1: Create the constants file**

```csharp
namespace DMFT.Core.Services;

public static class DownloadMode
{
    public const int Video = 0;
    public const int AudioOnly = 1;
    public const int AudioOriginOnly = 2;
    public const int VideoAndAudioOrigin = 3;
}
```

- [ ] **Step 2: Commit**

```bash
git add DMFT.Core/Services/DownloadMode.cs
git commit -m "feat: add DownloadMode constants"
```

---

### Task 2: Update `StatusCodes` error mapping in `DownloadEngine`

**Files:**
- Modify: `DMFT.Core/Services/DownloadEngine.cs`

**Interfaces:**
- Consumes: `DownloadMode` constants (from Task 1), `StatusCodes`, `ITikTokSoundExtractor`, `IMediaDownloader`
- Produces: Updated `StartDownloadAsync` with 4-mode branching

- [ ] **Step 1: Update `DownloadEngine` to branch on `DownloadMode`**

Replace the entire `StartDownloadAsync` method body. The constructor gains an `ITikTokSoundExtractor` parameter.

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

        _progressTimer = new Timer(async _ =>
        {
            await _downloadService.UpdateDownloadAsync(item);
        }, null, ProgressRefreshMs, ProgressRefreshMs);

        try
        {
            string videoDest = Path.Combine(item.SaveLocation, $"{item.VideoId}_video.mp4");
            string audioDest = Path.Combine(item.SaveLocation, $"{item.VideoId}_audio.mp3");

            if (item.DownloadMode == DownloadMode.VideoAndAudioOrigin || item.DownloadMode == DownloadMode.AudioOriginOnly)
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

            Task videoTask = null;
            Task audioTask = null;

            switch (item.DownloadMode)
            {
                case DownloadMode.VideoAndAudioOrigin:
                    item.CurrentFileName = Path.GetFileName(videoDest);
                    videoTask = _mediaDownloader.DownloadAsync(item.Url, videoDest, noWatermark: true);
                    if (!string.IsNullOrWhiteSpace(item.OriginalSoundUrl))
                    {
                        item.CurrentFileName = Path.GetFileName(audioDest);
                        audioTask = _mediaDownloader.DownloadAudioAsync(item.OriginalSoundUrl, audioDest);
                    }
                    if (videoTask != null && audioTask != null)
                        await Task.WhenAll(videoTask, audioTask);
                    else
                        throw new Exception("Missing download tasks");
                    break;

                case DownloadMode.Video:
                    item.CurrentFileName = Path.GetFileName(videoDest);
                    videoTask = _mediaDownloader.DownloadAsync(item.Url, videoDest, noWatermark: true);
                    if (videoTask != null)
                        await videoTask;
                    else
                        throw new Exception("Video download task missing");
                    break;

                case DownloadMode.AudioOriginOnly:
                    if (!string.IsNullOrWhiteSpace(item.OriginalSoundUrl))
                    {
                        item.CurrentFileName = Path.GetFileName(audioDest);
                        audioTask = _mediaDownloader.DownloadAudioAsync(item.OriginalSoundUrl, audioDest);
                        if (audioTask != null)
                            await audioTask;
                        else
                            throw new Exception("Audio origin download failed");
                    }
                    else
                        throw new Exception("No audio URL");
                    break;

                case DownloadMode.AudioOnly:
                    if (!string.IsNullOrWhiteSpace(item.Url))
                    {
                        item.CurrentFileName = Path.GetFileName(audioDest);
                        audioTask = _mediaDownloader.DownloadAudioAsync(item.Url, audioDest);
                        if (audioTask != null)
                            await audioTask;
                        else
                            throw new Exception("Audio only failed");
                    }
                    else
                        throw new Exception("Video URL missing for audio only");
                    break;
            }

            item.Status = StatusCodes.Success;
            _progressTimer?.Dispose();
            _progressTimer = null;
            await _downloadService.MoveToHistoryAsync(item);
        }
        catch (Exception)
        {
            item.Status = item.DownloadMode switch
            {
                DownloadMode.VideoAndAudioOrigin => StatusCodes.VideoAudioOriginError,
                DownloadMode.Video => StatusCodes.VideoError,
                DownloadMode.AudioOriginOnly => StatusCodes.AudioOriginError,
                DownloadMode.AudioOnly => StatusCodes.AudioOnlyError,
                _ => StatusCodes.Error
            };
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

- [ ] **Step 2: Commit**

```bash
git add DMFT.Core/Services/DownloadEngine.cs
git commit -m "feat: add multi-mode download branching to DownloadEngine"
```

---

### Task 3: Register `ITikTokSoundExtractor` in DI

**Files:**
- Modify: `DMFT/DMFT/MauiProgram.cs`
- Modify: `DMFT/DMFT.Web/Program.cs`

- [ ] **Step 1: Add registration to MauiProgram.cs**

Add after line 43 (`services.AddSingleton<IDownloadEngine, DownloadEngine>()`):

```csharp
builder.Services.AddSingleton<ITikTokSoundExtractor, TikTokSoundExtractor>();
```

- [ ] **Step 2: Add registration to Web Program.cs**

Add after line 34 (`services.AddSingleton<IDownloadEngine, DownloadEngine>()`):

```csharp
builder.Services.AddSingleton<ITikTokSoundExtractor, TikTokSoundExtractor>();
```

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/MauiProgram.cs DMFT/DMFT.Web/Program.cs
git commit -m "feat: register ITikTokSoundExtractor in DI"
```

---

### Task 4: Add mode selection UI to `Main.razor`

**Files:**
- Modify: `DMFT/DMFT.Shared/Pages/Main.razor`

- [ ] **Step 1: Add "Set All Mode" dropdown + per-item mode dropdown**

Replace the entire file content:

```razor
@page "/"
@using DMFT.Core.Services
@using DMFT.Core.Entities
@using DMFT.Shared.Components
@inject DownloadService DownloadSvc
@inject DownloadQueue Queue
@inject ToastService Toast
@implements IDisposable

<PageTitle>DMFT - Download</PageTitle>

<div class="flex items-center justify-between mb-6">
    <h1 class="text-2xl font-bold text-on-surface m-0">Downloads</h1>
    <button class="px-4 py-2 rounded bg-primary text-on-primary hover:brightness-110 border-0 cursor-pointer text-sm font-medium flex items-center gap-2"
            @onclick="ShowAddModal">
        <span>+</span> Add URLs
    </button>
</div>

<AddModal @ref="addModal" OnAdd="AddUrls" />

@if (_items.Count == 0)
{
    <div class="text-center py-16 text-on-surface-dim">
        <div class="text-5xl mb-4 opacity-50">&#9654;</div>
        <p class="text-lg">No downloads yet. Click <strong>Add URLs</strong> to get started.</p>
    </div>
}
else
{
    <div class="mb-3 flex items-center gap-2">
        <span class="text-sm font-bold whitespace-nowrap text-on-surface">Set All Mode:</span>
        <select class="form-select form-select-sm px-2 py-1 rounded border border-outline-variant bg-surface text-on-surface text-sm"
                @bind="_selectedModeForAll">
            <option value="@DownloadMode.Video">Video</option>
            <option value="@DownloadMode.AudioOnly">Audio Only</option>
            <option value="@DownloadMode.AudioOriginOnly">Audio Origin</option>
            <option value="@DownloadMode.VideoAndAudioOrigin">Video + Audio Origin</option>
        </select>
        <button class="px-3 py-1 rounded text-xs bg-primary-container text-on-primary-container hover:brightness-110 border-0 cursor-pointer"
                @onclick="ApplyModeToAll">Apply to All</button>
    </div>

    <div class="space-y-3">
        @foreach (var item in _items.OrderByDescending(x => x.Time))
        {
            <div class="bg-surface rounded-lg p-4 flex flex-col gap-2 border border-outline-variant">
                <div class="flex items-center gap-3">
                    <span class="px-2 py-0.5 rounded text-xs font-medium text-on-primary bg-primary">@item.Platform</span>
                    <span class="flex-1 text-sm text-on-surface truncate">@item.Url</span>
                    <span class="text-xs text-on-surface-variant">@GetStatusLabel(item.Status)</span>
                    @if (string.IsNullOrEmpty(item.CurrentFileName))
                    {
                        <span class="text-xs px-2 py-0.5 rounded bg-error text-on-error">Missing</span>
                    }
                </div>
                <div class="flex items-center gap-2">
                    <span class="text-xs text-on-surface-variant">Mode:</span>
                    <select class="form-select form-select-sm px-2 py-0.5 rounded border border-outline-variant bg-surface text-on-surface text-xs"
                            @bind="item.DownloadMode">
                        <option value="@DownloadMode.Video">Video</option>
                        <option value="@DownloadMode.AudioOnly">Audio Only</option>
                        <option value="@DownloadMode.AudioOriginOnly">Audio Origin</option>
                        <option value="@DownloadMode.VideoAndAudioOrigin">Video + Audio Origin</option>
                    </select>
                </div>
                @if (item.ProgressPercent > 0 && item.ProgressPercent < 100)
                {
                    <div class="w-full h-2 bg-surface-variant rounded-full overflow-hidden">
                        <div class="h-full bg-primary rounded-full transition-all duration-300" style="width: @item.ProgressPercent%"></div>
                    </div>
                    <span class="text-xs text-on-surface-dim">@item.ProgressPercent%</span>
                }
                <div class="flex gap-2 mt-1">
                    <button class="px-3 py-1.5 rounded text-xs bg-primary text-on-primary hover:brightness-110 border-0 cursor-pointer"
                            @onclick="() => DownloadAsync(item)">Download</button>
                    <button class="px-3 py-1.5 rounded text-xs bg-error text-on-error hover:brightness-110 border-0 cursor-pointer"
                            @onclick="() => RemoveItem(item)">Remove</button>
                    @if (!string.IsNullOrEmpty(item.CurrentFileName))
                    {
                        <span class="px-3 py-1.5 rounded text-xs bg-primary-container text-on-primary-container">@item.CurrentFileName</span>
                    }
                </div>
            </div>
        }
    </div>
}

<LoadingModal @ref="loadingModal" />

@code {
    private List<DownloadItem> _items = new();
    private AddModal? addModal;
    private LoadingModal? loadingModal;
    private Timer? _timer;
    private int _selectedModeForAll = DownloadMode.Video;

    protected override async Task OnInitializedAsync()
    {
        await LoadItems();
        _timer = new Timer(async _ => await InvokeAsync(LoadItems), null, 5000, 5000);
    }

    public void Dispose() => _timer?.Dispose();

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
                DownloadMode = DownloadMode.Video,
                ProgressPercent = 0,
                Time = DateTime.UtcNow
            };
            await DownloadSvc.AddDownloadAsync(item);
        }
        await LoadItems();
        Toast.Show($"Added {urls.Length} URL(s)", ToastLevel.Success);
    }

    private void ApplyModeToAll()
    {
        foreach (var item in _items)
        {
            item.DownloadMode = _selectedModeForAll;
        }
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

    private async Task RemoveItem(DownloadItem item)
    {
        await DownloadSvc.DeleteDownloadAsync(item.Id);
        Toast.Show($"Removed: {item.VideoId}", ToastLevel.Info);
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

- [ ] **Step 2: Commit**

```bash
git add DMFT/DMFT.Shared/Pages/Main.razor
git commit -m "feat: add download mode selection UI"
```

---

### Task 5: Ensure Playwright browsers are installed and document in AGENTS.md

**Files:**
- Modify: `AGENTS.md`

- [ ] **Step 1: Update AGENTS.md with Playwright setup step**

Add a bullet under the existing `## Dependencies` section:

```markdown
- **Playwright browsers**: Required for TikTok sound extraction. After restore/build, run `pwsh bin\Debug\net10.0\playwright.ps1 install chromium`
```

Also add a build-and-install command alias:

```diff
 ## Key Commands
 
 ```bash
 # Restore & build
 dotnet restore DMFT/DMFT.csproj
 dotnet build DMFT/DMFT.csproj -c Release
 
+# Build + install playwright browsers
+dotnet build DMFT/DMFT.csproj -c Release && pwsh DMFT/bin/Debug/net10.0/playwright.ps1 install chromium
+
```

- [ ] **Step 2: Commit**

```bash
git add AGENTS.md
git commit -m "docs: add Playwright browser install instructions"
```

---

### Task 6: Build and verify

- [ ] **Step 1: Build the project**

```bash
dotnet build DMFT/DMFT.csproj -c Release
```

Expected: Build succeeds with no errors.

- [ ] **Step 2: Run the project**

```bash
dotnet run --project DMFT/DMFT.csproj -c Release
```

Or for the web version:

```bash
dotnet run --project DMFT/DMFT.Web/DMFT.Web.csproj -c Release
```

Verify the app starts without exceptions at DI resolution time.

- [ ] **Step 3: Final commit if any build fixes needed**

```bash
git add -A
git commit -m "fix: build fixes after multi-mode implementation"
```
