<p align="center">
  <img src="DMFT/DMFT/Resources/Images/dotnet_bot.svg" alt="DMFT" width="120" />
</p>

# DMFT — Download Manager For TikTok & YouTube

> **⚠️ Lưu ý**: Tài liệu dự án chi tiết nằm trong thư mục [docs/](docs/README.md).

## 📖 Giới thiệu

**DMFT** (v2) là ứng dụng đa nền tảng **MAUI Blazor Hybrid** cho phép quản lý và tải video từ **TikTok**, **YouTube**, **YouTube Shorts** thông qua công cụ **yt-dlp**.

Ứng dụng cung cấp giao diện web-based hiện đại (Tailwind CSS v4), hàng đợi tải, lịch sử tải xuống, hệ thống toast thông báo, và hỗ trợ trích xuất âm thanh gốc (Origin Audio) qua Playwright.

### Kiến trúc (v2)

| Layer | Công nghệ |
|-------|-----------|
| UI | Blazor Hybrid + Vite 7 + Tailwind CSS v4 |
| Backend | .NET 10 / MAUI |
| Database | SQLite qua EF Core (Code First + Migrations) |
| Download engine | yt-dlp (wrapper process) |
| Origin Audio | Playwright (.NET) browser automation |

## ✨ Tính năng chính

- Hỗ trợ đa nền tảng: TikTok, YouTube, YouTube Shorts
- Nhiều chế độ tải (dùng `[Flags]` enum):
  - **Video** (1) — tải video, có watermark
  - **Audio** (2) — chỉ lấy audio
  - **Origin Audio** (4) — trích xuất âm thanh gốc (TikTok/YT Shorts)
  - Có thể kết hợp: Video + Audio, Video + Origin Audio, cả 3
- Hàng đợi tải tự động xử lý tuần tự
- Trích xuất Origin Audio qua Playwright (tự động phát hiện browser)
- Giao diện sáng/tối + 5 màu nhấn
- Lưu trữ SQLite (`dmft.db` trong thư mục AppData)
- Áp dụng chế độ tải hàng loạt (Apply to All)
- Hủy tải với cleanup tài nguyên (đóng Playwright browser, kill yt-dlp)

## ⚙️ Yêu cầu hệ thống

- .NET **10.0 Runtime**
- **yt-dlp** — tự động dò theo thứ tự: AppData → `./yt-dlp/` → PATH
- **Playwright browsers** (cho Origin Audio):
  ```bash
  pwsh bin/Debug/net10.0/playwright.ps1 install chromium
  ```

## 🚀 Cài đặt & chạy

### Build (yêu cầu .NET 10.0 SDK + MAUI workload + Node.js 20+)

```bash
dotnet restore DMFT.slnx
dotnet build DMFT.slnx -c Release
```

### Frontend (Vite + Tailwind)

```bash
cd DMFT/DMFT/Components/vite-project
npm install && npm run build
```

### Chạy ứng dụng

```bash
dotnet run --project DMFT/DMDT -c Release
```

## 🖱️ Hướng dẫn sử dụng

- **Thêm URL**: Nhấn **Add**, dán URL TikTok/YouTube/YouTube Shorts
- **Chọn chế độ tải**: Video, Audio, Origin Audio (từng item hoặc Apply to All)
- **Tải xuống**: Nhấn **Download** trên item hoặc **Download All**
- **Theo dõi**: Progress bar, tốc độ, ETA trên từng item
- **Lịch sử**: Trang **History** — các item đã tải thành công
- **Hủy**: Nhấn **Cancel** — tự động dọn Playwright browser + kill yt-dlp
- **Cài đặt**: Trang **Settings** — thư mục mặc định, delay giữa các lần tải

## 🧪 Testing

```bash
dotnet test DMFT.Test.App/DMFT.Test.App.csproj
```

> **Ghi chú**: Test project (xUnit v3 + Moq) hiện là scaffold rỗng — chưa có test nào.

## 🗂️ Cấu trúc dự án

```
DMFT.slnx
├── DMFT/DMFT/                          # Ứng dụng chính (MAUI Blazor)
│   ├── Entities/                        # EF Core models
│   │   ├── DownloadItem.cs              # Item tải xuống
│   │   ├── DownloadSetting.cs           # Cấu hình tải
│   │   └── AppSetting.cs                # Cài đặt ứng dụng
│   ├── Data/
│   │   └── AppDbContext.cs              # DbContext (SQLite)
│   ├── Services/
│   │   ├── Interfaces/                  # Interface contracts
│   │   └── Implements/                  # Implementations
│   │       ├── DownloadService.cs       # CRUD + history
│   │       ├── DownloadQueue.cs         # Hàng đợi tuần tự
│   │       ├── DownloadEngine.cs        # Điều phối tải
│   │       ├── YtDlpService.cs          # Wrapper yt-dlp
│   │       ├── YtDlpConfigProvider.cs   # Cấu hình runtime
│   │       ├── SoundExtractor.cs        # Playwright extraction
│   │       ├── VideoLinkParser.cs        # Parse URL
│   │       └── AppSettingsService.cs    # Settings cache
│   ├── Components/
│   │   ├── Pages/                       # Blazor pages
│   │   ├── Components/                  # Shared components
│   │   └── vite-project/               # Frontend build
│   ├── MauiProgram.cs                   # DI setup
│   └── wwwroot/                         # Static assets
├── DMFT.Test.App/                       # Unit tests (scaffold)
└── docs/                                # Tài liệu
```

## 🔧 Cấu hình nâng cao

- **Thư mục lưu mặc định**: Cài đặt trong **Settings**, fallback `%USERPROFILE%\Music`
- **Delay giữa các lần tải**: Mặc định 2000ms, cấu hình trong Settings
- **DownloadMode flags**:
  - `Video = 1`
  - `Audio = 2`
  - `OriginAudio = 4`
- **Origin Audio** chỉ hỗ trợ trên TikTok và YouTube Shorts (cần Playwright browser)

## 🛠 Troubleshooting

| Vấn đề | Nguyên nhân | Cách khắc phục |
|--------|-------------|----------------|
| Không tìm thấy yt-dlp | yt-dlp chưa được cài | Đặt `yt-dlp.exe` trong `%AppData%/DMFT/yt-dlp/` hoặc `./yt-dlp/` |
| Origin Audio không hoạt động | Thiếu Playwright browser | Chạy `playwright.ps1 install chromium` |
| Lỗi EF Core migration | DB cũ bị lỗi | Xóa `dmft.db` trong AppData, khởi động lại (auto-migrate) |
| Frontend không cập nhật | Chưa build CSS/JS | `npm run build` trong `vite-project/` |

## 📦 Công nghệ sử dụng

- [.NET 10 / MAUI](https://dotnet.microsoft.com/) — Cross-platform framework
- [Blazor Hybrid](https://learn.microsoft.com/aspnet/core/blazor/hybrid/) — UI
- [EF Core](https://learn.microsoft.com/ef/core/) + SQLite — Persistence
- [yt-dlp](https://github.com/yt-dlp/yt-dlp) — Download engine
- [Playwright .NET](https://playwright.dev/dotnet/) — Browser automation
- [Vite 7](https://vitejs.dev/) — Frontend tooling
- [Tailwind CSS v4](https://tailwindcss.com/) — Styling
