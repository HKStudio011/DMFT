using System.Text.RegularExpressions;

namespace DMFT.Services
{
    public interface ITikTokLinkParser
    {
        bool IsTikTokUrl(string url);
        bool TryParseVideoId(string url, out string? videoId);
    }

    // Lightweight URL parser for TikTok video IDs
    public class TikTokLinkParser : ITikTokLinkParser
    {
        private static readonly Regex VideoIdRegex = new Regex(@"video/(\d+)|videoId=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex PhotoIdRegex = new Regex(@"photo/(\d+)|videoId=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public bool IsTikTokUrl(string url)
        {
            if (string.IsNullOrWhiteSpace(url)) return false;
            return url.Contains("tiktok.com", System.StringComparison.OrdinalIgnoreCase);
        }

        public bool TryParseVideoId(string url, out string? videoId)
        {
            videoId = null;
            if (string.IsNullOrWhiteSpace(url)) return false;
            var m = VideoIdRegex.Match(url);
            if (!m.Success) 
                m = PhotoIdRegex.Match(url);
            if (!m.Success) return false;
            videoId = m.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(videoId)) videoId = m.Groups[2].Value;
            return !string.IsNullOrWhiteSpace(videoId);
        }
    }
}
