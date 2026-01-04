# Tài liệu DMFT - Giới thiệu Chi tiết

> **Liên quan**: [README](../README.md) | [AGENT](AGENT.md) | [ARCHITECTURE](ARCHITECTURE.md)

## Giới thiệu Project

**DMFT** (Download Manager for TikTok) là ứng dụng đa nền tảng được phát triển bằng **.NET MAUI Blazor**, cho phép người dùng quản lý và tải video TikTok một cách hiệu quả.

### Mục tiêu
- Cung cấp giao diện trực quan để quản lý link TikTok
- Hỗ trợ nhiều chế độ tải (video, audio, audio gốc,...)
- Tích hợp với yt-dlp để download video chất lượng cao
- Lưu trữ lịch sử và metadata của các video đã tải

### Công nghệ sử dụng

| Công nghệ | Phiên bản | Mục đích |
|-----------|-----------|----------|
| .NET | 10.0 | Cross-platform framework |
| MAUI | Latest | Mobile/desktop UI |
| Blazor | Latest | Web-based UI components |
| C# | Latest | Primary language |
| yt-dlp | Latest | Video downloading engine |

## Cấu trúc thư mục

```
DMFT/
├── Components/              # UI Components (Blazor)
│   ├── Layout/             # Layout components
│   │   ├── MainLayout.razor
│   │   ├── MainLayout.razor.css
│   │   ├── NavMenu.razor
│   │   └── NavMenu.razor.css
│   ├── Pages/              # Route pages
│   │   ├── Main.razor      # Trang chính
│   │   ├── History.razor   # Lịch sử tải
│   │   └── NotFound.razor  # Trang 404
│   └── Components/         # Reusable components
│       ├── AddModal.razor
│       ├── ToastContainer.razor
│       └── LoadingModal.razor
├── Model/                  # Data models
│   ├── LinkInfo.cs         # Thông tin link
│   ├── DownloadMode.cs     # Enum chế độ tải
│   ├── DownloadSettings.cs # Cấu hình tải
│   ├── HistoryContainer.cs # Container lịch sử
│   ├── MainContainer.cs    # Container chính
│   ├── StatusMessage.cs    # Thông báo trạng thái
│   ├── ToastService.cs     # Service toast
│   ├── TikTokTypes.cs      # Types TikTok
│   ├── YtDlpConfig.cs      # Cấu hình yt-dlp
│   └── BaseContainer.cs    # Base container
├── Services/               # Business logic
│   ├── TikTok/            # Services TikTok
│   │   ├── TikTokDownloaderService.cs
│   │   ├── TikTokLinkParser.cs
│   │   ├── MediaDownloader.cs
│   │   ├── DownloadEngineAdapter.cs
│   │   ├── DownloadQueue.cs
│   │   └── TikTokSoundExtractor.cs
│   └── Selenium/          # Selenium services
│       └── SeleniumServices.cs
├── Platforms/              # Platform-specific
│   ├── Windows/
│   ├── macOS/
│   ├── iOS/
│   └── Android/
├── Utilities/              # Helper utilities
│   └── FolderOpener.cs
├── Resources/              # Resources
├── wwwroot/               # Web root
├── docs/                  # Documentation
└── yt-dlp/               # yt-dlp binaries
```

## Tính năng

### Quản lý Link
- Thêm nhiều URL TikTok cùng lúc
- Parse và validate URL
- Trích xuất metadata (video ID, title, creator, duration,...)

### Download
- Hỗ trợ nhiều chế độ tải
- Queue management
- Progress tracking
- Retry mechanism

### Lưu trữ
- Metadata persistence
- Download history
- File organization

### Giao diện
- Toast notifications
- Loading states
- Responsive design

## Luồng hoạt động

```
User Input → URL Validation → Metadata Extraction
       ↓                                    ↓
    Add to Queue ←──── Download Process ────┘
       ↓
    Save to Disk
       ↓
    Update History
       ↓
    Show Toast Notification
```

## Related Documentation
- [README](../README.md) - Tổng quan nhanh
- [AGENT](AGENT.md) - Hướng dẫn cho AI agents
- [ARCHITECTURE](ARCHITECTURE.md) - Chi tiết kiến trúc
- [DATA_MODELS](DATA_MODELS.md) - Mô hình dữ liệu
- [DEVELOPMENT](DEVELOPMENT.md) - Hướng dẫn phát triển
- [UX_GUIDE](UX_GUIDE.md) - Hướng dẫn UX/UI
