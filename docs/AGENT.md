# Agent Instructions for DMFT Project

> **Liên quan**: [README](../README.md) | [Development](DEVELOPMENT.md) | [Architecture](ARCHITECTURE.md)

## Project Overview
DMFT là ứng dụng đa nền tảng TikTok downloader được xây dựng bằng **MAUI Blazor**. Cho phép người dùng quản lý và tải video TikTok sử dụng **yt-dlp**.

## Technology Stack

| Technology | Purpose |
|------------|---------|
| **.NET 10.0** | Cross-platform framework |
| **MAUI** | Mobile/desktop UI framework |
| **Blazor** | Web-based UI components |
| **C#** | Primary language |
| **yt-dlp** | Video downloading engine |

## Documentation Structure

| File | Purpose |
|------|---------|
| [README](../README.md) | Project overview và quick start |
| [README](README.md) | Giới thiệu chi tiết |
| [ARCHITECTURE](ARCHITECTURE.md) | Kiến trúc hệ thống |
| [DATA_MODELS](DATA_MODELS.md) | Mô hình dữ liệu |
| [DEVELOPMENT](DEVELOPMENT.md) | Hướng dẫn phát triển |
| [UX_GUIDE](UX_GUIDE.md) | Hướng dẫn UX/UI |

## Key Conventions

### Code Style
- **NO comments** unless explicitly requested
- Follow existing patterns in codebase
- Use existing libraries and utilities

### File Locations
```
DMFT/
├── Components/          # Blazor UI (Razor)
│   ├── Layout/         # Main layout, NavMenu
│   ├── Pages/          # Route pages
│   └── Components/     # Reusable components
├── Model/              # C# data models
├── Services/           # Business logic (C#)
│   ├── TikTok/        # TikTok download services
│   └── Selenium/      # Selenium services
├── Platforms/          # Platform-specific code
├── Utilities/          # Helper utilities
└── docs/              # Documentation
```

### Data Storage
- **AppData**: `main_data.json`, `history_data.json`
- **yt-dlp binaries**: `./yt-dlp/` folder

## Workflow for Changes

1. **Understand** existing codebase structure
2. **Make minimal** changes following existing patterns
3. **Verify** changes compile successfully
4. **Run lint/typecheck** if available

## Important Rules

| Rule | Description |
|------|-------------|
| ❌ **NEVER commit** | Changes unless explicitly requested |
| ❌ **NEVER guess** | Ask if unclear |
| ❌ **NO comments** | In code unless asked |
| ✅ **ALWAYS verify** | Builds after changes |
| ✅ **Check docs** | Before making structural changes |

## Common Commands

```bash
# Restore dependencies
dotnet restore DMFT/DMFT.csproj

# Build
dotnet build DMFT/DMFT.csproj -c Release

# Build for Windows
dotnet build DMFT/DMFT.csproj -c Release -f net10.0-windows

# Clean build
dotnet clean && dotnet restore && dotnet build -c Release
```

## Build Tools
- Standard .NET SDK build tools
- Check `build_command.txt` for custom build steps
- Solution file: `DMFT.slnx`

## Code References
- **Layout & Toast**: `Components/Layout/MainLayout.razor`, `Components/Components/ToastContainer.razor`
- **Main Page**: `Components/Pages/Main.razor`
- **History Page**: `Components/Pages/History.razor`
- **Download Services**: `Services/TikTok/DownloadEngineAdapter.cs`, `Services/TikTok/MediaDownloader.cs`
- **Models**: `Model/LinkInfo.cs`, `Model/DownloadMode.cs`, `Model/TikTokTypes.cs`
- **MAUI Startup**: `MauiProgram.cs`

## Related Documentation
- [README](../README.md) - Project overview
- [DEVELOPMENT](DEVELOPMENT.md) - Setup và troubleshooting
- [ARCHITECTURE](ARCHITECTURE.md) - Chi tiết kiến trúc
- [DATA_MODELS](DATA_MODELS.md) - Mô hình dữ liệu
- [UX_GUIDE](UX_GUIDE.md) - UX/UI guidelines
