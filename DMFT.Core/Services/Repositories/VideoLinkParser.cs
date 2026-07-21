using System.Text.RegularExpressions;

namespace DMFT.Core.Services;

public enum VideoPlatform
{
    Unknown,
    TikTok,
    YouTube,
    YouTubeShorts
}

public interface IVideoLinkParser
{
    bool IsSupportedUrl(string url);
    bool TryParseVideoId(string url, out string? videoId);
    VideoPlatform GetPlatform(string url);
    string GetPlatformLabel(VideoPlatform platform);
}

public class VideoLinkParser : IVideoLinkParser
{
    private static readonly Regex TikTokVideoIdRegex = new(@"(?:video|v)/(\d+)|videoId=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TikTokPhotoIdRegex = new(@"photo/(\d+)|videoId=(\d+)", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex YouTubeWatchRegex = new(@"(?:youtube\.com/watch\?v=|youtu\.be/)([a-zA-Z0-9_-]{11})", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex YouTubeShortRegex = new(@"youtube\.com/shorts/([a-zA-Z0-9_-]{11})", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public bool IsSupportedUrl(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return false;
        return url.Contains("tiktok.com", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("youtube.com", StringComparison.OrdinalIgnoreCase) ||
               url.Contains("youtu.be", StringComparison.OrdinalIgnoreCase);
    }

    public VideoPlatform GetPlatform(string url)
    {
        if (string.IsNullOrWhiteSpace(url)) return VideoPlatform.Unknown;
        if (url.Contains("tiktok.com", StringComparison.OrdinalIgnoreCase))
            return VideoPlatform.TikTok;
        if (url.Contains("youtube.com/shorts/", StringComparison.OrdinalIgnoreCase) ||
            url.Contains("youtu.be/", StringComparison.OrdinalIgnoreCase))
            return VideoPlatform.YouTubeShorts;
        if (url.Contains("youtube.com/watch", StringComparison.OrdinalIgnoreCase))
            return VideoPlatform.YouTube;
        return VideoPlatform.Unknown;
    }

    public string GetPlatformLabel(VideoPlatform platform) => platform switch
    {
        VideoPlatform.TikTok => "TikTok",
        VideoPlatform.YouTube => "YouTube",
        VideoPlatform.YouTubeShorts => "YouTube Shorts",
        _ => "Unknown"
    };

    public bool TryParseVideoId(string url, out string? videoId)
    {
        videoId = null;
        if (string.IsNullOrWhiteSpace(url)) return false;

        var m = TikTokVideoIdRegex.Match(url);
        if (!m.Success) m = TikTokPhotoIdRegex.Match(url);
        if (!m.Success) m = YouTubeWatchRegex.Match(url);
        if (!m.Success) m = YouTubeShortRegex.Match(url);

        if (!m.Success) return false;
        videoId = m.Groups[1].Value;
        return !string.IsNullOrWhiteSpace(videoId);
    }
}
