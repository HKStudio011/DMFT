# Data Models

> **Liên quan**: [Architecture](ARCHITECTURE.md) | [UX Guide](UX_GUIDE.md) | [Development](DEVELOPMENT.md)

## Core Models

### LinkInfo (Extended)
> File: `Model/LinkInfo.cs`

```csharp
public class LinkInfo Base fields
    public
{
    // string Id { get; set; }
    public string Url { get; set; }
    public string FilePath { get; set; }
    public string Status { get; set; }

    // TikTok-specific fields
    public string VideoId { get; set; }
    public string OriginalUrl { get; set; }
    public string CreatorName { get; set; }
    public int VideoDurationSeconds { get; set; }
    public string ThumbnailUrl { get; set; }
    public string TitleDescription { get; set; }
    public WatermarkPreference WatermarkPreference { get; set; }
    public DownloadFormat DownloadFormat { get; set; }
    public TikTokVideoInfo TikTokMetadata { get; set; }
}
```

### TikTokVideoInfo
```csharp
public class TikTokVideoInfo
{
    public string VideoId { get; set; }
    public string Author { get; set; }
    public string Title { get; set; }
    public int DurationSeconds { get; set; }
    public string MusicTitle { get; set; }
    public bool HasWatermark { get; set; }
    public List<QualityOption> QualityOptions { get; set; }
}
```

### DownloadMode
> Enum định nghĩa chế độ tải

| Value | Mô tả |
|-------|-------|
| `Video` | Tải video |
| `Audio Only` | Chỉ tải audio |
| `Audio Origin Only` | Tải audio gốc |
| `Video And Audio Origin` | Video + Audio gốc |

## Data Storage

### AppData Files
| File | Mục đích |
|------|----------|
| `main_data.json` | Lưu trữ danh sách link chính |
| `history_data.json` | Lưu trữ lịch sử tải |

### File Naming Convention
```
{creator}_{title}_{videoId}.{extension}
```

## Related Files
| File | Mô tả |
|------|-------|
| `Model/LinkInfo.cs` | Core link model |
| `Model/TikTokTypes.cs` | TikTok-specific types |
| `Model/MainContainer.cs` | Main data container |
| `Model/HistoryContainer.cs` | History data container |
| `Model/BaseContainer.cs` | Base container class |
| `Model/DownloadMode.cs` | Download mode enum |
| `Model/DownloadSettings.cs` | Download settings |

## Related Documentation
- [Architecture](ARCHITECTURE.md) - Tổng quan hệ thống
- [UX Guide](UX_GUIDE.md) - User flow và UI
