using System.Collections.Generic;

namespace DMFT.Model
{
    public enum WatermarkPreference
    {
        Watermarked,
        Original,
        NoPreference
    }

    public enum DownloadFormat
    {
        VideoMp4,
        AudioMp3,
        Both
    }

    public class TikTokVideoInfo
    {
        public string VideoId { get; set; } = string.Empty;
        public string Author { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public int DurationSeconds { get; set; }
        public string MusicTitle { get; set; } = string.Empty;
        public bool HasWatermark { get; set; }
        public List<string> QualityOptions { get; set; } = new List<string>();
    }
}
