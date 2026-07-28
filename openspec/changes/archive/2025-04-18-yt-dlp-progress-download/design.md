## Context

yt-dlp supports JSON progress output via `--progress-template "%(progress)j"` flag. The current `MediaDownloader.RunYtDlpAsync()` uses `ReadToEndAsync()` which waits for complete output before processing - no real-time feedback. UI currently shows static info only.

## Goals / Non-Goals

**Goals:**
- Real-time progress display (%, bytes downloaded/total, speed, ETA) during yt-dlp download
- Current file indicator when downloading Video + Audio Origin simultaneously
- Progress wired to LinkInfo so UI can refresh via existing container state

**Non-Goals:**
- Pause/resume functionality (out of scope)
- Per-file separate progress (show aggregated or current file only)
- Background download when app minimized (MAUI handles this)

## Decisions

### D1: Progress Callback Mechanism
**Choice:** `Action<DownloadProgress>` callback on `IMediaDownloader` interface
**Alternative:** Event-based `event Action<DownloadProgress>` 
**Rationale:** Callback is simpler for one-to-one relationship between downloader and engine adapter. Event would add overhead for this use case.

### D2: Progress Properties Location
**Choice:** Add properties directly to `LinkInfo` model
**Alternative:** Separate `DownloadProgress` class stored in container
**Rationale:** LinkInfo already holds all download state. Progress is per-link, so keeping it in LinkInfo simplifies UI binding and persistence (though progress not persisted long-term).

### D3: Dual Download (Video + Audio Origin) Progress
**Choice:** Sequential file indicator update - update `CurrentFileName` as each task starts
**Rationale:** User wants to know which file is downloading. Since tasks run in parallel, we can't aggregate progress cleanly - show current active file.

### D4: Main.razor Progress Display
**Choice:** Add progress bar in "Currently Downloading" card alongside existing info
**Rationale:** Keeps all download info in one place. Simple Bootstrap progress bar + text details below.

## Risks / Trade-offs

- **[yt-dlp JSON parsing]** Some progress lines may not be valid JSON → fallback to logging raw line
- **[Speed = 0 during ETA calculation]** yt-dlp may report speed=0 initially → display "calculating..." 
- **[Parallel download progress]** Video + Audio run in parallel → only show one file's progress or switch between them