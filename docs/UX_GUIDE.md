# UX Guidelines

> **Liên quan**: [Architecture](ARCHITECTURE.md) | [Data Models](DATA_MODELS.md) | [Development](DEVELOPMENT.md)

## User Flow

```
┌─────────────────────────────────────────────────────────┐
│  1. Select Save Folder (Browse)                         │
│                    ↓                                    │
│  2. Add TikTok URLs (Add button - one per line)         │
│                    ↓                                    │
│  3. Download: Individual or "Download All"              │
│                    ↓                                    │
│  4. Monitor Status (Download/ReInstall/Cancel)          │
│                    ↓                                    │
│  5. View History                                        │
└─────────────────────────────────────────────────────────┘
```

## UI Components

| Component | File | Mô tả | Related Code |
|-----------|------|-------|--------------|
| **AddModal** | `Components/Components/AddModal.razor` | Validates TikTok URLs, shows errors | [TikTokLinkParser](ARCHITECTURE.md#servicestiktok) |
| **Main Grid** | `Components/Pages/Main.razor` | Thumbnail, title, creator, duration, status | [LinkInfo](DATA_MODELS.md#linkinfo-extended) |
| **ToastContainer** | `Components/Components/ToastContainer.razor` | Success/error notifications | [ToastService](ARCHITECTURE.md#model) |
| **History Page** | `Components/Pages/History.razor` | Download history | [HistoryContainer](DATA_MODELS.md#related-files) |
| **Main Layout** | `Components/Layout/MainLayout.razor` | App shell, navigation | - |
| **NavMenu** | `Components/Layout/NavMenu.razor` | Navigation menu | - |

## Features

### Download Modes
> Xem chi tiết: [Data Models - DownloadMode](DATA_MODELS.md#downloadmode)

- **Video** - Tải video TikTok
- **Audio Only** - Trích xuất audio
- **Audio Origin Only** - Tải audio gốc từ video
- **Video + Audio Origin** - Video với audio gốc

### Preview
- Hiển thị metadata/thumbnail trước khi tải
- Thông tin bao gồm: VideoId, Creator, Duration, Title

### Notifications
- Toast messages cho success/error
- Real-time status updates
- Download progress (xem [DownloadQueue](ARCHITECTURE.md#servicestiktok))

### Legal
- Copyright warning khi download
- Hướng dẫn sử dụng hợp pháp

## Platform Support

| Platform | Status | Notes |
|----------|--------|-------|
| Windows | ✅ Supported | Primary development |
| macOS | ✅ Supported | Requires yt-dlp_macos |
| iOS | ✅ Supported | - |
| Android | ✅ Supported | - |

## File Structure - UI
```
Components/
├── Layout/
│   ├── MainLayout.razor      # App shell
│   ├── MainLayout.razor.css
│   ├── NavMenu.razor         # Navigation
│   └── NavMenu.razor.css
├── Pages/
│   ├── Main.razor            # Main download page
│   ├── History.razor         # Download history
│   └── NotFound.razor        # 404 page
└── Components/
    ├── AddModal.razor        # Add URL modal
    ├── ToastContainer.razor  # Toast notifications
    └── LoadingModal.razor    # Loading indicator
```

## Related Documentation
- [Architecture](ARCHITECTURE.md) - Data flow và services
- [Data Models](DATA_MODELS.md) - LinkInfo và TikTok metadata
- [Development](DEVELOPMENT.md) - Setup và troubleshooting
