using System;

namespace DMFT.Model
{
    public class LinkInfo
    {
        public string Url { get; set; } = string.Empty;
        public int Status { get; set; }
        public DateTime Time { get; set; } = DateTime.Now;

        public string VideoId { get; set; } = string.Empty;
        public string OriginalUrl { get; set; } = string.Empty;
        public string ThumbnailUrl { get; set; } = string.Empty;
        public string TitleDescription { get; set; } = string.Empty;

        // TikTok-specific: original sound info
        public string OriginalSoundUrl { get; set; } = string.Empty;
        public string OriginalSoundName { get; set; } = string.Empty;

        public string SaveLocation { get; set; } = string.Empty;
        public DownloadMode DownloadMode { get; set; } = DownloadMode.Video;

        public long DownloadedBytes { get; set; }
        public long TotalBytes { get; set; }
        public double Speed { get; set; }
        public int EtaSeconds { get; set; }
        public int ProgressPercent { get; set; }
        public string CurrentFileName { get; set; } = string.Empty;
    }
}
