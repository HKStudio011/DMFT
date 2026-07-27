using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace DMFT.Entities;

public class DownloadItem
{
    [Key]
    public Guid Id { get; set; } = Guid.NewGuid();
    [MaxLength(2048)]
    public string Url { get; set; } = string.Empty;
    [MaxLength(50)]
    public string Platform { get; set; } = "Unknown";
    public int Status { get; set; }
    public DateTime Time { get; set; } = DateTime.UtcNow;
    [MaxLength(100)]
    public string VideoId { get; set; } = string.Empty;
    [MaxLength(2048)]
    public string OriginalUrl { get; set; } = string.Empty;
    [MaxLength(2048)]
    public string ThumbnailUrl { get; set; } = string.Empty;
    [MaxLength(500)]
    public string TitleDescription { get; set; } = string.Empty;
    [MaxLength(2048)]
    public string OriginalSoundUrl { get; set; } = string.Empty;
    [MaxLength(300)]
    public string OriginalSoundName { get; set; } = string.Empty;
    [MaxLength(1000)]
    public string SaveLocation { get; set; } = string.Empty;
    public int DownloadMode { get; set; }
    public long DownloadedBytes { get; set; }
    public long TotalBytes { get; set; }
    [NotMapped]
    public double Speed { get; set; }
    [NotMapped]
    public int EtaSeconds { get; set; }
    [NotMapped]
    public int ProgressPercent { get; set; }
    [NotMapped]
    public string SpeedFormatted
    {
        get
        {
            if (Speed <= 0) return "";
            if (Speed >= 1_000_000_000) return $"{Speed / 1_000_000_000:F1} GB/s";
            if (Speed >= 1_000_000) return $"{Speed / 1_000_000:F1} MB/s";
            if (Speed >= 1_000) return $"{Speed / 1_000:F1} KB/s";
            return $"{Speed:F0} B/s";
        }
    }
    [NotMapped]
    public string EtaFormatted
    {
        get
        {
            if (EtaSeconds <= 0) return "";
            var ts = TimeSpan.FromSeconds(EtaSeconds);
            if (ts.TotalHours >= 1) return $"{ts.Hours}h {ts.Minutes}m";
            if (ts.TotalMinutes >= 1) return $"{ts.Minutes}m {ts.Seconds}s";
            return $"{ts.Seconds}s";
        }
    }
    [MaxLength(500)]
    public string CurrentFileName { get; set; } = string.Empty;
    public bool InHistory { get; set; }

    [NotMapped]
    public bool DownloadVideo
    {
        get => (DownloadMode & (int)Entities.DownloadMode.Video) != 0;
        set => SetFlag((int)Entities.DownloadMode.Video, value);
    }

    [NotMapped]
    public bool DownloadAudio
    {
        get => (DownloadMode & (int)Entities.DownloadMode.Audio) != 0;
        set => SetFlag((int)Entities.DownloadMode.Audio, value);
    }

    [NotMapped]
    public bool DownloadOriginAudio
    {
        get => (DownloadMode & (int)Entities.DownloadMode.OriginAudio) != 0;
        set => SetFlag((int)Entities.DownloadMode.OriginAudio, value);
    }

    private void SetFlag(int bit, bool on)
    {
        DownloadMode = on ? DownloadMode | bit : DownloadMode & ~bit;
    }
}
