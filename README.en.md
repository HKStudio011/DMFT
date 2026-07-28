<p align="center">
  <img src="DMFT/DMFT/Resources/Images/dotnet_bot.svg" alt="DMFT" width="120" />
</p>

# DMFT — Download Manager For TikTok & YouTube

> **Note**: Detailed project documentation is in the [docs/](docs/README.md) folder.

## 📖 About

**DMFT** (v2) is a cross-platform **MAUI Blazor Hybrid** application for downloading videos from **TikTok**, **YouTube**, and **YouTube Shorts** using **yt-dlp**.

It features a modern web-based UI (Tailwind CSS v4), download queue, history management, toast notifications, and Origin Audio extraction via Playwright.

### Architecture (v2)

| Layer | Technology |
|-------|-----------|
| UI | Blazor Hybrid + Vite 7 + Tailwind CSS v4 |
| Backend | .NET 10 / MAUI |
| Database | SQLite via EF Core (Code First + Migrations) |
| Download engine | yt-dlp (process wrapper) |
| Origin Audio | Playwright (.NET) browser automation |

## ✨ Key Features

- Multi-platform support: TikTok, YouTube, YouTube Shorts
- Multiple download modes (`[Flags]` enum):
  - **Video** (1) — download video with watermark
  - **Audio** (2) — audio-only extraction
  - **Origin Audio** (4) — original sound extraction (TikTok/YT Shorts)
  - Combinations: Video+Audio, Video+Origin Audio, all three
- Sequential download queue with auto-processing
- Origin Audio extraction via Playwright (auto-detect browser)
- Light/Dark theme + 5 accent colors
- SQLite persistence (`dmft.db` in AppData directory)
- Batch mode application (Apply to All)
- Cancel with full resource cleanup (close Playwright, kill yt-dlp)

## ⚙️ System Requirements

- .NET **10.0 SDK** + MAUI workload
- **yt-dlp** — auto-resolved: AppData → `./yt-dlp/` → PATH
- **Playwright browsers** (for Origin Audio):
  ```bash
  pwsh bin/Debug/net10.0/playwright.ps1 install chromium
  ```
- Node.js 20+ (for frontend build)

## 🚀 Setup & Run

### Restore & build

```bash
dotnet restore DMFT.slnx
dotnet build DMFT.slnx -c Release
```

### Frontend (Vite + Tailwind)

```bash
cd DMFT/DMFT/Components/vite-project
npm install && npm run build
```

### Run

```bash
dotnet run --project DMFT/DMDT -c Release
```

## 🖱️ Usage Guide

- **Add URL**: Click **Add**, paste TikTok/YouTube/YouTube Shorts URL
- **Select mode**: Video, Audio, Origin Audio (per-item or Apply to All)
- **Download**: Click **Download** on an item or **Download All**
- **Monitor**: Progress bar, speed, ETA on each item
- **History**: **History** page — successfully downloaded items
- **Cancel**: Click **Cancel** — auto-cleans Playwright browser + kills yt-dlp
- **Settings**: **Settings** page — default download path, inter-download delay

## 🧪 Testing

```bash
dotnet test DMFT.Test.App/DMFT.Test.App.csproj
```

> **Note**: The test project (xUnit v3 + Moq) is currently an empty scaffold — no tests written yet.

## 🗂️ Project Structure

```
DMFT.slnx
├── DMFT/DMFT/                          # Main app (MAUI Blazor)
│   ├── Entities/                        # EF Core models
│   │   ├── DownloadItem.cs              # Download item
│   │   ├── DownloadSetting.cs           # Download settings
│   │   └── AppSetting.cs                # App settings
│   ├── Data/
│   │   └── AppDbContext.cs              # DbContext (SQLite)
│   ├── Services/
│   │   ├── Interfaces/                  # Interface contracts
│   │   └── Implements/                  # Implementations
│   │       ├── DownloadService.cs       # CRUD + history
│   │       ├── DownloadQueue.cs         # Sequential queue
│   │       ├── DownloadEngine.cs        # Download orchestrator
│   │       ├── YtDlpService.cs          # yt-dlp wrapper
│   │       ├── YtDlpConfigProvider.cs   # Runtime config
│   │       ├── SoundExtractor.cs        # Playwright extraction
│   │       ├── VideoLinkParser.cs        # URL parser
│   │       └── AppSettingsService.cs    # Settings cache
│   ├── Components/
│   │   ├── Pages/                       # Blazor pages
│   │   ├── Components/                  # Shared components
│   │   └── vite-project/               # Frontend build
│   ├── MauiProgram.cs                   # DI wiring
│   └── wwwroot/                         # Static assets
├── DMFT.Test.App/                       # Unit tests (scaffold)
└── docs/                                # Documentation
```

## 🔧 Advanced Configuration

- **Default save path**: Configured in **Settings**, fallback `%USERPROFILE%\Music`
- **Inter-download delay**: Default 2000ms, configurable in Settings
- **DownloadMode flags**:
  - `Video = 1`
  - `Audio = 2`
  - `OriginAudio = 4`
- **Origin Audio** is only supported on TikTok and YouTube Shorts (requires Playwright browser)

## 🛠 Troubleshooting

| Issue | Cause | Fix |
|-------|-------|-----|
| yt-dlp not found | yt-dlp not installed | Place `yt-dlp.exe` in `%AppData%/DMFT/yt-dlp/` or `./yt-dlp/` |
| Origin Audio fails | Playwright browser missing | Run `playwright.ps1 install chromium` |
| EF Core migration error | Corrupted old DB | Delete `dmft.db` in AppData, restart (auto-migrates) |
| Frontend not updating | CSS/JS not rebuilt | Run `npm run build` in `vite-project/` |

## 📦 Tech Stack

- [.NET 10 / MAUI](https://dotnet.microsoft.com/) — Cross-platform framework
- [Blazor Hybrid](https://learn.microsoft.com/aspnet/core/blazor/hybrid/) — UI
- [EF Core](https://learn.microsoft.com/ef/core/) + SQLite — Persistence
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) — Download engine
- [Playwright .NET](https://playwright.dev/dotnet/) — Browser automation
- [Vite 7](https://vitejs.dev/) — Frontend tooling
- [Tailwind CSS v4](https://tailwindcss.com/) — Styling
