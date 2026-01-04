# Architecture

> **Liên quan**: [Data Models](DATA_MODELS.md) | [UX Guide](UX_GUIDE.md) | [Development](DEVELOPMENT.md)

## Overview
TikTokDownloaderService là thành phần chính điều phối giữa parser, metadata extraction, và downloader.

## Core Components

### Services/TikTok/
| Component | File | Mô tả |
|-----------|------|-------|
| **TikTokDownloaderService** | `TikTokDownloaderService.cs` | Main service orchestrating download flow |
| **TikTokLinkParser** | `TikTokLinkParser.cs` | Validates TikTok URLs and extracts videoId |
| **TikTokSoundExtractor** | `TikTokSoundExtractor.cs` | Extracts audio from TikTok videos |
| **MediaDownloader** | `MediaDownloader.cs` | Handles actual media download using yt-dlp |
| **DownloadQueue** | `DownloadQueue.cs` | Manages download queue |
| **DownloadEngineAdapter** | `DownloadEngineAdapter.cs` | Bridges with existing Download Manager flow |

### Services/Selenium/
| Component | File | Mô tả |
|-----------|------|-------|
| **SeleniumServices** | `SeleniumServices.cs` | Selenium-based scraping services |

### Model/
> Xem chi tiết: [Data Models](DATA_MODELS.md)

| Component | File | Mô tả |
|-----------|------|-------|
| **LinkInfo** | `LinkInfo.cs` | Extended with TikTok-specific fields |
| **DownloadMode** | `DownloadMode.cs` | Enum for download modes |
| **DownloadSettings** | `DownloadSettings.cs` | Download configuration |
| **YtDlpConfig** | `YtDlpConfig.cs` | yt-dlp configuration |
| **HistoryContainer** | `HistoryContainer.cs` | History data container |
| **MainContainer** | `MainContainer.cs` | Main data container |
| **StatusMessage** | `StatusMessage.cs` | Status messages |
| **ToastService** | `ToastService.cs` | Toast notification service |
| **TikTokTypes** | `TikTokTypes.cs` | TikTok-specific types |

## Data Flow

```
User Input (AddModal)
       ↓
TikTokLinkParser → Extract VideoId
       ↓
MetadataExtractor → Fetch Metadata
       ↓
DownloadEngineAdapter → Initiate Download
       ↓
MediaDownloader (yt-dlp) → Save to Disk
       ↓
StatusMessage & History Updated
       ↓
UI Shows Result (Toast + History)
```

1. User enters TikTok URL in [AddModal](UX_GUIDE.md#ui-components)
2. TikTokLinkParser validates and extracts videoId
3. MetadataExtractor fetches metadata (title, creator, duration, thumbnail)
4. DownloadEngine initiates download using chosen source
5. UI shows thumbnail, metadata, and current status
6. History lists TikTok links with storage paths

## Download Modes
> Xem chi tiết: [Data Models - DownloadMode](DATA_MODELS.md#downloadmode)

- `Video` - Download video only
- `Audio Only` - Extract audio from video
- `Audio Origin Only` - Download original audio
- `Video And Audio Origin` - Download video with original audio

## Related Documentation
- [Data Models](DATA_MODELS.md) - Chi tiết về cấu trúc dữ liệu
- [UX Guide](UX_GUIDE.md) - User flow và UI components
- [Development](DEVELOPMENT.md) - Setup và troubleshooting
