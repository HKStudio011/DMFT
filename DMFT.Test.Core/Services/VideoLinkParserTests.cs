using DMFT.Core.Services;

namespace DMFT.Test.Core.Services;

public class VideoLinkParserTests
{
    private readonly IVideoLinkParser _parser = new VideoLinkParser();

    [Theory]
    [InlineData("https://www.tiktok.com/@user/video/1234567890")]
    [InlineData("https://vm.tiktok.com/abc123/")]
    [InlineData("https://www.youtube.com/watch?v=dQw4w9WgXcQ")]
    [InlineData("https://youtu.be/dQw4w9WgXcQ")]
    [InlineData("https://www.youtube.com/shorts/abc123defgh")]
    [InlineData("https://m.tiktok.com/v/1234567890")]
    public void IsSupportedUrl_ValidUrls_ReturnsTrue(string url)
    {
        var result = _parser.IsSupportedUrl(url);

        Assert.True(result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    [InlineData(null)]
    [InlineData("https://www.facebook.com/watch")]
    [InlineData("https://vimeo.com/12345")]
    [InlineData("not-a-url")]
    public void IsSupportedUrl_InvalidUrls_ReturnsFalse(string url)
    {
        var result = _parser.IsSupportedUrl(url);

        Assert.False(result);
    }

    [Fact]
    public void GetPlatform_TikTokUrl_ReturnsTikTok()
    {
        var result = _parser.GetPlatform("https://www.tiktok.com/@user/video/123");

        Assert.Equal(VideoPlatform.TikTok, result);
    }

    [Fact]
    public void GetPlatform_YouTubeWatchUrl_ReturnsYouTube()
    {
        var result = _parser.GetPlatform("https://www.youtube.com/watch?v=dQw4w9WgXcQ");

        Assert.Equal(VideoPlatform.YouTube, result);
    }

    [Fact]
    public void GetPlatform_YouTubeShortsUrl_ReturnsYouTubeShorts()
    {
        var result = _parser.GetPlatform("https://www.youtube.com/shorts/abc123defgh");

        Assert.Equal(VideoPlatform.YouTubeShorts, result);
    }

    [Fact]
    public void GetPlatform_YouTuBeUrl_ReturnsYouTubeShorts()
    {
        var result = _parser.GetPlatform("https://youtu.be/dQw4w9WgXcQ");

        Assert.Equal(VideoPlatform.YouTubeShorts, result);
    }

    [Theory]
    [InlineData("")]
    [InlineData("https://facebook.com")]
    [InlineData(null)]
    public void GetPlatform_UnknownUrl_ReturnsUnknown(string url)
    {
        var result = _parser.GetPlatform(url);

        Assert.Equal(VideoPlatform.Unknown, result);
    }

    [Fact]
    public void GetPlatformLabel_TikTok_ReturnsTikTok()
    {
        var result = _parser.GetPlatformLabel(VideoPlatform.TikTok);

        Assert.Equal("TikTok", result);
    }

    [Fact]
    public void GetPlatformLabel_YouTube_ReturnsYouTube()
    {
        var result = _parser.GetPlatformLabel(VideoPlatform.YouTube);

        Assert.Equal("YouTube", result);
    }

    [Fact]
    public void GetPlatformLabel_YouTubeShorts_ReturnsYouTubeShorts()
    {
        var result = _parser.GetPlatformLabel(VideoPlatform.YouTubeShorts);

        Assert.Equal("YouTube Shorts", result);
    }

    [Fact]
    public void GetPlatformLabel_Unknown_ReturnsUnknown()
    {
        var result = _parser.GetPlatformLabel(VideoPlatform.Unknown);

        Assert.Equal("Unknown", result);
    }

    [Fact]
    public void TryParseVideoId_TikTokVideoUrl_ExtractsVideoId()
    {
        var result = _parser.TryParseVideoId("https://www.tiktok.com/@user/video/1234567890", out var videoId);

        Assert.True(result);
        Assert.Equal("1234567890", videoId);
    }

    [Fact]
    public void TryParseVideoId_YouTubeWatchUrl_ExtractsVideoId()
    {
        var result = _parser.TryParseVideoId("https://www.youtube.com/watch?v=dQw4w9WgXcQ", out var videoId);

        Assert.True(result);
        Assert.Equal("dQw4w9WgXcQ", videoId);
    }

    [Fact]
    public void TryParseVideoId_YouTubeShortsUrl_ExtractsVideoId()
    {
        var result = _parser.TryParseVideoId("https://www.youtube.com/shorts/abc123defgh", out var videoId);

        Assert.True(result);
        Assert.Equal("abc123defgh", videoId);
    }

    [Fact]
    public void TryParseVideoId_YouTuBeUrl_ExtractsVideoId()
    {
        var result = _parser.TryParseVideoId("https://youtu.be/dQw4w9WgXcQ", out var videoId);

        Assert.True(result);
        Assert.Equal("dQw4w9WgXcQ", videoId);
    }

    [Fact]
    public void TryParseVideoId_TikTokPhotoUrl_ExtractsPhotoId()
    {
        var result = _parser.TryParseVideoId("https://www.tiktok.com/@user/photo/9876543210", out var videoId);

        Assert.True(result);
        Assert.Equal("9876543210", videoId);
    }

    [Fact]
    public void TryParseVideoId_UnsupportedUrl_ReturnsFalse()
    {
        var result = _parser.TryParseVideoId("https://facebook.com/watch", out var videoId);

        Assert.False(result);
        Assert.Null(videoId);
    }

    [Fact]
    public void TryParseVideoId_NullUrl_ReturnsFalse()
    {
        var result = _parser.TryParseVideoId(null!, out var videoId);

        Assert.False(result);
        Assert.Null(videoId);
    }

    [Fact]
    public void TryParseVideoId_EmptyUrl_ReturnsFalse()
    {
        var result = _parser.TryParseVideoId("", out var videoId);

        Assert.False(result);
        Assert.Null(videoId);
    }

    [Fact]
    public void TryParseVideoId_TikTokMobileUrl_ExtractsVideoId()
    {
        var result = _parser.TryParseVideoId("https://m.tiktok.com/v/1234567890", out var videoId);

        Assert.True(result);
        Assert.Equal("1234567890", videoId);
    }
}
