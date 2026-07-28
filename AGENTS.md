# AGENTS.md

This is the **v2** branch — a near-total rewrite from v1. Treat v1 knowledge (JSON persistence, `MainContainer`, `DMFT.Tests/`) as obsolete.

## Key Commands

```bash
# Restore & build
dotnet restore DMFT.slnx
dotnet build DMFT.slnx -c Release

# Frontend (Vite + Tailwind v4) — rebuild after .razor/.css/.ts changes
cd DMFT/DMFT/Components/vite-project && npm run build

# Watch frontend rebuild on change
npm run watch

# EF Core migration
dotnet ef migrations add <Name> --project DMFT/DMFT --context AppDbContext
```

## Project Structure

- **Solution**: `DMFT.slnx` (2 projects)
- **Main app**: `DMFT/DMFT/` — MAUI Blazor hybrid, targets `net10.0;net10.0-android;net10.0-ios;net10.0-maccatalyst;net10.0-windows10.0.19041.0`
- **Test project**: `DMFT.Test.App/` — xUnit v3 + Moq. Currently **no test files exist** (empty scaffold).
- **Frontend**: `DMFT/DMFT/Components/vite-project/` — Vite 7 + Tailwind CSS v4, builds to `wwwroot/build/`

## Architecture

| Layer | Location | Notes |
|-------|----------|-------|
| **Entities** | `DMFT/DMFT/Entities/` | `DownloadItem`, `DownloadSetting`, `AppSetting` — EF Core models |
| **DB** | `DMFT/DMFT/Data/` | SQLite via EF Core. Migrations auto-applied on startup |
| **Services (interfaces)** | `DMFT/DMFT/Services/Interfaces/` | `IAppSettingsService`, `IStoragePathProvider`, `IYtDlpConfigProvider` |
| **Services (impl)** | `DMFT/DMFT/Services/Implements/` | `DownloadService`, `DownloadEngine`, `DownloadQueue`, `YtDlpService`, `VideoLinkParser`, `TikTokSoundExtractor`, `AppSettingsService`, `AppUpdateService` |
| **Pages** | `DMFT/DMFT/Components/Pages/` | `Main.razor` (/), `History.razor` (/history), `Settings.razor` (/settings), `NotFound.razor` |
| **Shared components** | `DMFT/DMFT/Components/Components/` | `ModalBase`, `AddModal`, `LoadingModal`, `ToastContainer` |
| **Theme** | `vite-project/src/css/theme.css` | CSS custom properties with light/dark + 5 accent colors |

## Key Conventions (v2-specific)

- **Persistence**: EF Core + SQLite (`dmft.db` in app data), **not JSON**. `AppDbContext` has 3 DbSets: `DownloadItems`, `DownloadSettings`, `AppSettings`.
- **`DownloadMode`**: `[Flags]` enum — `Video=1`, `Audio=2`, `OriginAudio=4`. `DownloadItem` exposes `DownloadVideo`, `DownloadAudio`, `DownloadOriginAudio` boolean properties.
- **`StatusCodes`**: Static int constants (`New=0`, `Waiting=1`, `Downloading=2`, `Canceled=3`, `Success=4`, `Error=99`, etc.) — replaces v1's `StatusMessage` enum.
- **App settings**: Cached in-memory via `IAppSettingsService`, backed by `AppSettings` table. Loaded at startup after migrations.
- **Render mode**: Blazor Hybrid — all pages use `InteractiveRenderSettings.InteractiveServer`. On MAUI, `ConfigureBlazorHybridRenderModes()` nulls out WASM/Auto modes (they don't apply).
- **Toast**: Custom `ToastService` in `Services/Implements/`, not MAUI CommunityToolkit.
- **DI**: `MauiProgram.cs` wires everything — requires `IStoragePathProvider` registered before `YtDlpConfigProvider`, which depends on both.

## Frontend Build Pipeline

- CSS is Tailwind v4 (`@import "tailwindcss"`) — no PostCSS config file needed
- TS entry: `vite-project/src/ts/main.ts` (jQuery + theme switcher)
- Build output: `wwwroot/build/assets/main.js` + `wwwroot/build/assets/styles.css`
- **Always rebuild frontend** after changing `.razor` files (Tailwind scans them for class usage) or CSS/TS

## Dependencies

- .NET 10 SDK + MAUI workload
- **yt-dlp**: Resolved by `YtDlpConfigProvider` — looks in AppData first, then `AppContext.BaseDirectory/yt-dlp/`, falls back to PATH
- **Playwright browsers**: Required for TikTok sound extraction. Run `pwsh bin/Debug/net10.0/playwright.ps1 install chromium` after build

## Testing

- xUnit v3 + Moq. Project: `DMFT.Test.App/` (references main project)
- No tests written yet — the project is an empty scaffold
- Run: `dotnet test DMFT.Test.App/DMFT.Test.App.csproj`

## Troubleshooting

- **yt-dlp not found**: Place `yt-dlp.exe` in `{AppData}/DMFT/yt-dlp/` or in `./yt-dlp/` next to the binary
- **EF Core migration issues**: Delete `dmft.db` from AppData and restart (auto-migrates on startup)
- **Frontend not updating**: Run `npm run build` or `npm run watch` in `vite-project/`
