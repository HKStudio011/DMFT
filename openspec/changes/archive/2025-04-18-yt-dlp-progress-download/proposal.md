## Why

Currently, yt-dlp downloads have no real-time progress feedback - users see only static thumbnail/info while downloading. This creates poor UX especially for large files where users can't tell if the download is progressing, stuck, or failed.

## What Changes

- Add progress callback mechanism to `MediaDownloader` using yt-dlp's `--progress-template "%(progress)j"` JSON output
- Add progress properties to `LinkInfo` model (downloaded_bytes, total_bytes, speed, eta, progress_percent, current_filename)
- Update `DownloadEngineAdapter` to wire progress updates to link and trigger UI refresh
- Add progress bar with detailed info (%, downloaded/total, speed/sec, ETA) in Main.razor "Currently Downloading" section
- Add current file indicator when downloading Video + Audio Origin simultaneously

## Capabilities

### New Capabilities
- `yt-dlp-progress`: Download progress tracking via yt-dlp JSON output - parse and display real-time progress (bytes, %, speed, ETA) in UI

### Modified Capabilities
- None (existing specs cover download modes, no requirement changes)

## Impact

- `DMFT/Services/TikTok/MediaDownloader.cs` - add progress streaming via event callback
- `DMFT/Model/LinkInfo.cs` - add progress properties
- `DMFT/Services/TikTok/DownloadEngineAdapter.cs` - wire progress updates
- `DMFT/Components/Pages/Main.razor` - add progress bar UI