using System.Text.RegularExpressions;

namespace DMFT.Services
{
    public interface IVideoLinkParser
    {
        bool IsSupportedUrl(string url);
        bool TryParseVideoId(string url, out string? videoId);
        string GetPlatform(string url);
    }

    public class VideoLinkParser : IVideoLinkParser
    {
        private static readonly Regex TikTokVideoIdRegex = new Regex(@"video/(\d+)|videoId=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TikTokPhotoIdRegex = new Regex(@"photo/(\d+)|videoId=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        
        private static readonly Regex YouTubeWatchRegex = new Regex(@"(?:youtube\.com/watch\?v=|youtu\.be/)([a-zA-Z0-9_-]{11})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex YouTubeShortRegex = new Regex(@"youtu\.be/([a-zA-Z0-9_-]{11})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public bool IsSupportedUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return IsTikTokUrl(url) || IsYouTubeUrl(url);
        }

        public bool IsTikTokUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.Contains("tiktok.com", System.StringComparison.OrdinalIgnoreCase);
        }

        public bool IsYouTubeUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.Contains("youtube.com", System.StringComparison.OrdinalIgnoreCase) ||
                   url.Contains("youtu.be", System.StringComparison.OrdinalIgnoreCase);
        }

        public string GetPlatform(string url)
        {
            if (IsYouTubeUrl(url)) return "YouTube";
            if (IsTikTokUrl(url)) return "TikTok";
            return "Unknown";
        }

        public bool TryParseVideoId(string url, out string? videoId)
        {
            videoId = null;
            if (string.IsNullOrWhiteSpace(url)) return false;

            var m = TikTokVideoIdRegex.Match(url);
            if (!m.Success) 
                m = TikTokPhotoIdRegex.Match(url);
            if (!m.Success)
                m = YouTubeWatchRegex.Match(url);
            if (!m.Success)
                m = YouTubeShortRegex.Match(url);
            
            if (!m.Success) return false;
            videoId = m.Groups[1].Value;
            return !string.IsNullOrWhiteSpace(videoId);
        }
    }
}