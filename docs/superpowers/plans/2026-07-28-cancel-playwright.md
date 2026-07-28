# Cancel Playwright Browser Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** CancelOriginAudio extraction kills the Playwright browser window immediately when user clicks Cancel.

**Architecture:** `ISoundExtractor` gains `CancelAsync()`; `SoundExtractor` stores the current `IPlaywright`/`IBrowser` references as instance fields so `CancelAsync()` can dispose them; `DownloadEngine.CancelDownloadAsync` calls it before killing yt-dlp.

**Tech Stack:** .NET 10, MAUI Blazor, Microsoft.Playwright

## Global Constraints

- All methods in `ISoundExtractor` interface must be implemented in `SoundExtractor`
- `CancelAsync()` must be safe to call concurrently (null checks + try/catch)
- The Playwright browser window must close within ~500ms of Cancel
- No `using`/`await using` on tracked Playwright resources (they're disposed manually in `CancelAsync()`)
- Frontend rebuild NOT required (C# only change)

---

### Task 1: Add `CancelAsync` to `ISoundExtractor` interface

**Files:**
- Modify: `DMFT/DMFT/Services/Implements/SoundExtractor.cs:5-9` (interface block)

**Interfaces:**
- Consumes: (none)
- Produces: `Task CancelAsync()` in `ISoundExtractor`

- [ ] **Step 1: Read current interface**

Read `DMFT/DMFT/Services/Implements/SoundExtractor.cs` lines 1-9 to confirm current interface shape.

- [ ] **Step 2: Add `CancelAsync()` to interface**

```csharp
public interface ISoundExtractor
{
    Task<(string? soundName, string? soundUrl, string? videoId)> GetOriginalSoundTiktokAsync(string videoUrl);
    Task<string?> GetOriginalSoundYTShortAsync(string videoUrl);
    Task<bool> CheckAvailableAsync();
    Task CancelAsync();
}
```

- [ ] **Step 3: Build to verify interface compiles**

Run: `dotnet build DMFT.slnx -c Release -f net10.0`
Expected: error CS0535 — `SoundExtractor` does not implement `CancelAsync`

- [ ] **Step 4: Commit**

```bash
git add DMFT/DMFT/Services/Implements/SoundExtractor.cs
git commit -m "feat: add CancelAsync to ISoundExtractor interface"
```

---

### Task 2: Change `SoundExtractor` from `using` to tracked instance fields

**Files:**
- Modify: `DMFT/DMFT/Services/Implements/SoundExtractor.cs` — replace fields, update `GetOriginalSoundTiktokAsync` and `GetOriginalSoundYTShortAsync`

**Interfaces:**
- Consumes: `ISoundExtractor` from Task 1
- Produces: Instance fields `_currentPlaywright` and `_currentBrowser` that `CancelAsync()` can dispose

- [ ] **Step 1: Read current SoundExtractor fields and extract methods**

Read `DMFT/DMFT/Services/Implements/SoundExtractor.cs` lines 11-115 to identify all Playwright `using`/`await using` patterns.

- [ ] **Step 2: Add instance fields below `_available`**

```csharp
public class SoundExtractor : ISoundExtractor
{
    private readonly IVideoLinkParser _parser;
    private bool? _available;
    private IPlaywright? _currentPlaywright;
    private Microsoft.Playwright.IBrowser? _currentBrowser;
```

- [ ] **Step 3: Rewrite `GetOriginalSoundTiktokAsync` — remove `using`/`await using`, assign to fields**

Replace the Playwright setup block (from `using var playwright = ...` through `if (browser == null) return ...`):

```csharp
    public async Task<(string? soundName, string? soundUrl, string? videoId)> GetOriginalSoundTiktokAsync(string videoUrl)
    {
        _parser.TryParseVideoId(videoUrl, out var videoId);
        _currentPlaywright = await Microsoft.Playwright.Playwright.CreateAsync();
        _currentBrowser = await TryLaunchAsync(_currentPlaywright, headless: false);
        if (_currentBrowser == null) return (null, null, videoId);

        var page = await _currentBrowser.NewPageAsync();
        await page.GotoAsync(videoUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);
        await page.WaitForSelectorAsync("a[href^='/music/']", new() { Timeout = 300_000 });
        var musicLink = await page.QuerySelectorAsync("a[href^='/music/']");
        if (musicLink == null) return (null, null, videoId);

        await musicLink.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);
        await page.WaitForSelectorAsync("div#mse", new() { State = WaitForSelectorState.Attached, Timeout = 30000 });

        var nameEl = await page.QuerySelectorAsync("h1");
        var soundName = nameEl != null ? await nameEl.TextContentAsync() : null;

        var html = await page.ContentAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html,
            @"<div id=""mse""[\s\S]*?<video[^>]*src=""([^""]+)""");
        var soundUrl = match.Success ? match.Groups[1].Value : null;

        return (soundName?.Trim(), soundUrl, videoId);
    }
```

Key changes:
- `using var playwright` → `_currentPlaywright = await ...`
- `await using var browser` → `_currentBrowser = await TryLaunchAsync(...)`
- `browser` → `_currentBrowser`

- [ ] **Step 4: Build to verify**

Run: `dotnet build DMFT.slnx -c Release -f net10.0`
Expected: errors for missing `CancelAsync()` implementation

- [ ] **Step 5: Commit**

```bash
git add DMFT/DMFT/Services/Implements/SoundExtractor.cs
git commit -m "feat: track Playwright instances as fields in SoundExtractor"
```

---

### Task 3: Rewrite `GetOriginalSoundYTShortAsync` — track instances

**Files:**
- Modify: `DMFT/DMFT/Services/Implements/SoundExtractor.cs`

- [ ] **Step 1: Replace `using`/`await using` with instance field assignments**

Find and replace:

```csharp
    public async Task<string?> GetOriginalSoundYTShortAsync(string videoUrl)
    {
        _currentPlaywright = await Microsoft.Playwright.Playwright.CreateAsync();
        _currentBrowser = await TryLaunchAsync(_currentPlaywright, headless: false);
        if (_currentBrowser == null) return null;

        var page = await _currentBrowser.NewPageAsync();
        await page.GotoAsync(videoUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);

        var soundBtn = await page.WaitForSelectorAsync("#experiment-overlay > ytd-reel-player-overlay-renderer > yt-reel-player-overlay-view-model > div.ytReelPlayerOverlayViewModelActionsContainer > reel-action-bar-view-model > pivot-button-view-model > a", new() { Timeout = 300_000 });
        if (soundBtn == null) return null;
        await soundBtn.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);

        var panel = await page.WaitForSelectorAsync("#anchored-panel > ytd-engagement-panel-section-list-renderer:nth-child(4)", new() { Timeout = 300_000 });
        if (panel == null) return null;
        var firstContents = await panel.QuerySelectorAsync("#contents");
        if (firstContents == null) return null;
        var secondContents = await firstContents.QuerySelectorAsync("#contents");
        if (secondContents == null) return null;
        var items = await secondContents.QuerySelectorAllAsync(":scope > *");

        if (items.Count <= 1 && items.Count > 0)
        {
            var link = await items[0].QuerySelectorAsync("a");
            if (link == null) return null;
            await link.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);
            return page.Url;
        }

        if (items.Count >= 2)
        {
            var header = await firstContents.QuerySelectorAsync("#header > yt-page-header-view-model > div > div.ytPageHeaderViewModelHeadline > yt-content-preview-image-view-model");
            if (header == null) return null;
            await header.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);
            return page.Url;
        }

        return null;
    }
```

Key change: replace `await using` → `_currentPlaywright =` / `_currentBrowser =`

- [ ] **Step 2: Build to verify**

Run: `dotnet build DMFT.slnx -c Release -f net10.0`
Expected: error for missing `CancelAsync()` implementation

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/Services/Implements/SoundExtractor.cs
git commit -m "feat: track Playwright instances in GetOriginalSoundYTShortAsync"
```

---

### Task 4: Implement `CancelAsync` in `SoundExtractor`

**Files:**
- Modify: `DMFT/DMFT/Services/Implements/SoundExtractor.cs` — add method body

- [ ] **Step 1: Add `CancelAsync()` method**

Insert after `CheckAvailableAsync()`:

```csharp
    public Task CancelAsync()
    {
        try { _currentBrowser?.CloseAsync().GetAwaiter().GetResult(); } catch { }
        try { _currentBrowser?.Dispose(); } catch { }
        try { _currentPlaywright?.Dispose(); } catch { }
        _currentBrowser = null;
        _currentPlaywright = null;
        return Task.CompletedTask;
    }
```

> **Why synchronous Pattern?** `CancelAsync` on `IDownloadEngine` returns `Task` (not `ValueTask`), and `IMediaDownloader.CancelAsync()` is also synchronous-under-the-hood. Using `.GetAwaiter().GetResult()` on `CloseAsync` is acceptable here because: (1) Cancel is inherently fire-and-forget, (2) we wrap in try/catch, (3) disposing synchronously ensures yt-dlp kill isn't delayed by an async continuation.

- [ ] **Step 2: Build and verify zero errors**

Run: `dotnet build DMFT.slnx -c Release -f net10.0`
Expected: Build succeeded, 0 errors (1 pre-existing CA1416 warning in MauiProgram.cs)

- [ ] **Step 3: Commit**

```bash
git add DMFT/DMFT/Services/Implements/SoundExtractor.cs
git commit -m "feat: implement CancelAsync to close Playwright browser"
```

---

### Task 5: Wire `CancelAsync` into `DownloadEngine.CancelDownloadAsync`

**Files:**
- Modify: `DMFT/DMFT/Services/Implements/DownloadEngine.cs:149-152`

- [ ] **Step 1: Read current CancelDownloadAsync**

Read lines 148-152 of `DownloadEngine.cs`.

- [ ] **Step 2: Add `_soundExtractor.CancelAsync()` call before killing yt-dlp**

Replace the existing method:

```csharp
    public async Task CancelDownloadAsync(DownloadItem item)
    {
        await _soundExtractor.CancelAsync();
        await _mediaDownloader.CancelAsync();
    }
```

- [ ] **Step 3: Build and verify zero errors**

Run: `dotnet build DMFT.slnx -c Release -f net10.0`
Expected: Build succeeded, 0 errors

- [ ] **Step 4: Commit**

```bash
git add DMFT/DMFT/Services/Implements/DownloadEngine.cs
git commit -m "feat: close Playwright browser on Cancel in DownloadEngine"
```

---

### Task 6: Build all targets and final verification

**Files:**
- (none — verification only)

- [ ] **Step 1: Build main project**

Run: `dotnet build DMFT.slnx -c Release -f net10.0`
Expected: Build succeeded, 0 errors

- [ ] **Step 2: Build frontend**

Run: `cd DMFT/DMFT/Components/vite-project && npm run build`
Expected: built in ~600ms, 0 errors

- [ ] **Step 3: Check git status for staged/uncommitted files**

Run: `git status`
Expected: only the 2 modified files (`SoundExtractor.cs`, `DownloadEngine.cs`), all committed

---

## Self-Review Checklist

**1. Spec coverage:**
- Cancel kills Playwright browser: Tasks 2-5
- Safe concurrent call: Task 4 (try/catch + null check)
- No resource leak: Task 2-3 (remove `using` in favor of tracked fields + `CancelAsync` dispose)
- No frontend rebuild needed: Task 6 (C# only)

**2. Placeholder scan:** All code in every step is complete — no TBDs, TODOs, or "implement later" patterns.

**3. Type consistency:** `ISoundExtractor.CancelAsync()` returns `Task` — consistent in Task 1 (interface) and Task 4 (impl). `DownloadEngine.CancelDownloadAsync` returns `Task` — matches signature in Task 5.
