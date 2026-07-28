## Why

Model folder chứa dead code từ thiết kế TikTok-only ban đầu (`TikTokTypes.cs` với `WatermarkPreference`, `DownloadFormat`, `TikTokVideoInfo` không được sử dụng). Ngoài ra, app hiện tại chỉ gắn với TikTok trong UI nhưng đã dùng yt-dlp - một công cụ hỗ trợ cả YouTube. Cần cleanup để mở đường cho YouTube support.

## What Changes

- Xóa file `TikTalkTypes.cs` (dead code không sử dụng)
- Làm sạch `LinkInfo.cs` - remove unused fields (`WatermarkPreference`, `DownloadFormat`, `TikTokMetadata`)
- Rename UI labels từ "TikTok" → "Video" để hỗ trợ cả YouTube
- Mở rộng `TikTokLinkParser` để nhận diện cả YouTube URLs
- Cập nhật page titles và headings

## Capabilities

### New Capabilities
- `youtube-support`: Khả năng tải video từ YouTube bên cạnh TikTok

### Modified Capabilities
- (none - đây là refactor không thay đổi requirements)

## Impact

- **Code affected**: `DMFT/Model/TikTokTypes.cs`, `DMFT/Model/LinkInfo.cs`
- **UI affected**: `Main.razor`, `History.razor` - labels và titles
- **Services affected**: `TikTokLinkParser.cs` - mở rộng URL detection
- **No breaking changes** - chỉ remove dead code và rename labels