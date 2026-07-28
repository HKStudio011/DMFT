## Context

App hiện tại là TikTok downloader viết bằng MAUI Blazor, dùng yt-dlp làm backend download engine. Model folder có 10 file trong đó `TikTokTypes.cs` chứa các enum/class không được sử dụng: `WatermarkPreference`, `DownloadFormat`, `TikTokVideoInfo`. UI vẫn hardcoded "TikTok" trong labels/titles trong khi yt-dlp đã support YouTube natively.

## Goals / Non-Goals

**Goals:**
- Loại bỏ dead code trong Model folder
- Mở rộng app để support YouTube URLs ngoài TikTok
- Rename UI labels để generic hơn ("Video" thay vì "TikTok")

**Non-Goals:**
- Không thay đổi backend download logic (yt-dlp đã handle)
- Không thêm tính năng mới ngoài YouTube support
- Không refactor services - chỉ cleanup models và UI labels

## Decisions

### 1. Xóa TikTokTypes.cs thay vì giữ lại
**Rationale:** File này hoàn toàn là dead code - không có reference trong codebase ngoài definition trong LinkInfo.cs. Nếu cần TikTok features sau, có thể recreate từ git history.

### 2. LinkInfo fields cần remove
**Rationale:** `WatermarkPreference`, `DownloadFormat`, `TikTokMetadata` được define trong LinkInfo nhưng không sử dụng ở đâu cả. Xóa để giảm confusion.

### 3. Parser mở rộng bằng regex patterns
**Rationale:** Thay vì viết lại hoàn toàn, mở rộng `TikTokLinkParser` với thêm patterns cho YouTube URLs. Đây là cách ít invasive.

### 4. UI labels generic
**Rationale:** Thay "TikTok Downloader" → "Video Downloader", "TikTok Link" → "Video Link". User có thể hiểu app tải được nhiều nguồn.

## Risks / Trade-offs

- [Risk] Parser có thể không nhận diện một số YouTube URL formats → [Mitigation] Test với các format phổ biến, fallback vẫn gửi cho yt-dlp xử lý
- [Risk] Dead code removal có thể break serialization nếu JSON data cũ chứa these fields → [Mitigation] JsonSerializer ignore unknown properties by default trong .NET

## Migration Plan

1. Xóa TikTokTypes.cs
2. Update LinkInfo.cs - remove unused properties
3. Update TikTokLinkParser.cs - add YouTube URL detection
4. Update Main.razor - rename labels
5. Update History.razor - rename labels
6. Build và test

Rollback: Revert các file đã thay đổi qua git.