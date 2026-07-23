using System.ComponentModel.DataAnnotations.Schema;
using DMFT.Services;

namespace DMFT.Entities;

public class DownloadItem
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Url { get; set; } = string.Empty;
    public string Platform { get; set; } = "Unknown";
    public int Status { get; set; }
    public DateTime Time { get; set; } = DateTime.UtcNow;
    public string VideoId { get; set; } = string.Empty;
    public string OriginalUrl { get; set; } = string.Empty;
    public string ThumbnailUrl { get; set; } = string.Empty;
    public string TitleDescription { get; set; } = string.Empty;
    public string OriginalSoundUrl { get; set; } = string.Empty;
    public string OriginalSoundName { get; set; } = string.Empty;
    public string SaveLocation { get; set; } = string.Empty;
    public int DownloadMode { get; set; }
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
    public double Speed { get; set; }
    public int EtaSeconds { get; set; }
    public int ProgressPercent { get; set; }
    public string CurrentFileName { get; set; } = string.Empty;

    [NotMapped]
    public bool DownloadVideo
    {
        get => (DownloadMode & (int)Services.DownloadMode.Video) != 0;
        set => SetFlag((int)Services.DownloadMode.Video, value);
    }

    [NotMapped]
    public bool DownloadAudio
    {
        get => (DownloadMode & (int)Services.DownloadMode.Audio) != 0;
        set => SetFlag((int)Services.DownloadMode.Audio, value);
    }

    [NotMapped]
    public bool DownloadOriginAudio
    {
        get => (DownloadMode & (int)Services.DownloadMode.OriginAudio) != 0;
        set => SetFlag((int)Services.DownloadMode.OriginAudio, value);
    }

    private void SetFlag(int bit, bool on)
    {
        DownloadMode = on ? DownloadMode | bit : DownloadMode & ~bit;
    }
}
