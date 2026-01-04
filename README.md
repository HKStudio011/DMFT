
---

# DMFT - TikTok Downloader (MAUI Blazor)

> **⚠️ Lưu ý**: Đây là file giới thiệu dự án (project README). Tài liệu dự án chi tiết nằm trong thư mục [docs/](docs/README.md).

## 📖 Giới thiệu
**DMFT** là ứng dụng đa nền tảng (MAUI Blazor) cho phép quản lý và tải video TikTok thông qua công cụ **yt-dlp**.  
Ứng dụng cung cấp giao diện trực quan với danh sách liên kết, hàng đợi tải, lịch sử tải xuống, cùng hệ thống thông báo lỗi/thành công bằng **toast**.

## ✨ Tính năng chính
- Quản lý liên kết TikTok trong một nơi duy nhất.
- Hỗ trợ nhiều chế độ tải:
  - Video
  - Audio only
  - Audio origin only
  - Video + Audio origin
- Hàng đợi tải và lịch sử tải xuống.
- **Thay đổi gần đây (04/01/2026)**:
  - Thêm tính năng chọn Download Mode cho tất cả items trong danh sách
  - Sửa bug ghi nhật ký history khi history chưa được load
  - Triển khai 39 unit tests cho models
- Thông báo lỗi/thành công bằng toast.
- Lưu trữ dữ liệu trong **AppData** (`main_data.json`, `history_data.json`).

## ⚙️ Yêu cầu hệ thống
- .NET **10.0 SDK** (MAUI)
- Công cụ phát triển MAUI + trình giả lập/thiết bị (Windows, macOS, iOS, Android)
- **ơyt-dlp** [Xem tài liệu chính thức của yt-dlp](https://github.com/yt-dlp/yt-dlp)


---

## 🚀 Cài đặt & chạy
### Chuẩn bị môi trường
- Cài đặt .NET 10.0 SDK và workload MAUI phù hợp hệ điều hành.
- Đảm bảo **yt-dlp** có sẵn trong đường dẫn được `./yt-dlp`.

### Khôi phục & build
```bash
dotnet restore DMFT/DMFT.csproj
dotnet build DMFT/DMFT.csproj -c Release
```

### Chạy ứng dụng
- Mở ứng dụng (Windows/Mac)
- Truy cập trang **Main** để thêm URL TikTok và tải về.

## 🖱️ Hướng dẫn sử dụng
- **Chọn folder lưu**
  - Nhấn **Browse** đề chọn thư mục lưu file.
- **Thêm URL TikTok**
  - Nhấn **Add** trên trang Chính, dán một hoặc nhiều URL (mỗi URL một dòng).
- **Tải xuống**
  - Mỗi liên kết có nút **Download/ReInstall/Cancel** tùy trạng thái.
  - Có thể dùng **Download All** để tải tất cả liên kết chưa tải.
- **Theo dõi & quản lý**
  - Liên kết đang tải hoặc đã tải thành công được đánh dấu.
  - Lịch sử hiển thị trong trang **History**.
- **Thông báo lỗi**
  - Lỗi tải xuống hiển thị toast với chi tiết lỗi.

---

## ⚡ Cấu hình nâng cao
- **DownloadMode**:
  - `Video`
  - `Audio Only`
  - `Audio Origin Only`
  - `Video And Audio Origin`

---

## 🧪 Testing

### Chạy Unit Tests
```bash
# Build và chạy tests
dotnet test DMFT.Tests/DMFT.Tests.csproj

# Kết quả: Passed!  - Failed: 0, Passed: 39, Skipped: 0, Total: 39
```

### Test Coverage
| Test Class | Số lượng test | Mục đích |
|------------|---------------|----------|
| `DownloadModeTests` | 4 | Kiểm tra enum DownloadMode |
| `StatusMessageTests` | 8 | Kiểm tra enum StatusMessage |
| `LinkInfoTests` | 14 | Kiểm tra class LinkInfo |
| `TikTokTypesTests` | 9 | Kiểm tra TikTok types |

### Thêm Tests Mới
1. Tạo file test trong `DMFT.Tests/Models/` hoặc `DMFT.Tests/Services/`
2. Thêm các test methods với attribute `[Fact]` hoặc `[Theory]`
3. Chạy lại `dotnet test` để xác nhận tests pass

---

## 🔧 Khuyến nghị
- Đảm bảo **yt-dlp** có quyền thực thi trên thiết bị.
- Theo dõi cảnh báo nullable để tránh crash.

---

## 📚 Tài nguyên mã nguồn
- **Layout & Toast**: `DMFT/Components/Layout/MainLayout.razor`, `DMFT/Components/Components/ToastContainer.razor`
- **Trang Chính**: `DMFT/Components/Pages/Main.razor`
- **Trang Lịch sử**: `DMFT/Components/Pages/History.razor`
- **Lưu trữ chính**: `DMFT/Model/MainContainer.cs`, `DMFT/Model/HistoryContainer.cs`
- **Điều phối tải**: `DMFT/Services/TikTok/DownloadEngineAdapter.cs`, `DMFT/Services/TikTok/MediaDownloader.cs`
- **Cấu hình yt-dlp**: `DMFT/Model/YtDlpConfig.cs`
- **Maui startup**: `DMFT/MauiProgram.cs`

---

## 🛠 Troubleshooting phổ biến
1. Lỗi không tìm thấy yt-dlp
- Triệu chứng: Khi tải video, ứng dụng báo lỗi không tìm thấy yt-dlp.
- Nguyên nhân: Binary yt-dlp chưa được cài đặt hoặc không nằm trong đường dẫn mà YtDlpConfig tìm kiếm.
- Cách khắc phục:
- Tải yt-dlp từ [trang chính thức](https://github.com/yt-dlp/yt-dlp/releases).
- Đặt file binary vào thư mục "./yt-dlp"
- Đảm bảo file có quyền thực thi (Linux/macOS: chmod +x yt-dlp)
