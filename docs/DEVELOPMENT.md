# Development Guide

> **Liên quan**: [README](../README.md) | [Architecture](ARCHITECTURE.md) | [Data Models](DATA_MODELS.md) | [UX Guide](UX_GUIDE.md)

## Setup Requirements

| Requirement | Mô tả |
|-------------|-------|
| .NET 10.0 SDK | MAUI development |
| MAUI Workload | Cross-platform support |
| **yt-dlp** | Download from [releases](https://github.com/yt-dlp/yt-dlp/releases) |

### yt-dlp Setup
1. Download yt-dlp binary
2. Place in `./yt-dlp` folder:
   ```
   DMFT/
   └── yt-dlp/
       ├── yt-dlp         # Linux/macOS
       ├── yt-dlp.exe     # Windows
       └── yt-dlp_macos   # macOS
   ```
3. Ensure executable permissions (Linux/macOS):
   ```bash
   chmod +x yt-dlp/yt-dlp
   ```

## Build & Run

```bash
# Restore dependencies
dotnet restore DMFT/DMFT.csproj

# Build
dotnet build DMFT/DMFT.csproj -c Release

# Build for specific platform
dotnet build DMFT/DMFT.csproj -c Release -t:Run -f net10.0-windows
```

## Project Structure

```
DMFT/
├── Components/          # Blazor UI
│   ├── Layout/         # Main layout, NavMenu
│   ├── Pages/          # Main, History, NotFound
│   └── Components/     # AddModal, ToastContainer
├── Model/              # Data models
│   ├── LinkInfo.cs
│   ├── DownloadMode.cs
│   └── ...
├── Services/           # Business logic
│   ├── TikTok/        # Download services
│   └── Selenium/      # Scraping services
├── Platforms/          # Platform-specific code
├── Utilities/          # Helper utilities
├── DMFT.Tests/        # Unit tests (xUnit)
└── docs/              # Documentation
```

## Testing

### Run Tests
```bash
# Run all tests
dotnet test DMFT.Tests/DMFT.Tests.csproj

# Test results: Passed!  - Failed: 0, Passed: 39, Skipped: 0, Total: 39
```

### Test Structure
| Test File | Coverage |
|-----------|----------|
| `Models/DownloadModeTests.cs` | DownloadMode enum validation |
| `Models/StatusMessageTests.cs` | StatusMessage enum validation |
| `Models/LinkInfoTests.cs` | LinkInfo class properties |
| `Models/TikTokTypesTests.cs` | TikTok types and enums |

### Add New Tests
1. Create test file in `DMFT.Tests/Models/` or `DMFT.Tests/Services/`
2. Add test methods with `[Fact]` or `[Theory]` attributes
3. Run `dotnet test` to verify

## Key Files Reference

| Category | File | Related Docs |
|----------|------|--------------|
| **UI Layout** | `Components/Layout/MainLayout.razor` | [UX Guide](UX_GUIDE.md) |
| **Toast** | `Components/Components/ToastContainer.razor` | [UX Guide](UX_GUIDE.md) |
| **Main Page** | `Components/Pages/Main.razor` | [UX Guide](UX_GUIDE.md) |
| **History Page** | `Components/Pages/History.razor` | [UX Guide](UX_GUIDE.md) |
| **Add Modal** | `Components/Components/AddModal.razor` | [UX Guide](UX_GUIDE.md) |
| **Download Service** | `Services/TikTok/DownloadEngineAdapter.cs` | [Architecture](ARCHITECTURE.md) |
| **Media Downloader** | `Services/TikTok/MediaDownloader.cs` | [Architecture](ARCHITECTURE.md) |
| **MAUI Startup** | `MauiProgram.cs` | - |
| **Data Models** | `Model/LinkInfo.cs` | [Data Models](DATA_MODELS.md) |

## Troubleshooting

### yt-dlp not found
**Triệu chứng**: Ứng dụng báo lỗi không tìm thấy yt-dlp

**Giải pháp**:
1. Download từ [yt-dlp releases](https://github.com/yt-dlp/yt-dlp/releases)
2. Đặt binary vào thư mục `./yt-dlp`
3. Đảm bảo quyền thực thi (Linux/macOS: `chmod +x yt-dlp`)

### Build Errors
```bash
# Clean and rebuild
dotnet clean
dotnet restore
dotnet build -c Release
```

## Related Documentation
- [README](../README.md) - Tổng quan project
- [Architecture](ARCHITECTURE.md) - Chi tiết kiến trúc
- [Data Models](DATA_MODELS.md) - Mô hình dữ liệu
- [UX Guide](UX_GUIDE.md) - Hướng dẫn UX/UI
