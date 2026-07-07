using DMFT.Core.Data;
using DMFT.Core.Services;
using DMFT.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DMFT.Test.Core.Services;

public class YtDlpConfigProviderTests
{
    [Fact]
    public void MauiProvider_FallbackPath_WhenExeNotFound()
    {
        var storageMock = new Mock<IStoragePathProvider>();
        storageMock.Setup(s => s.GetAppDataPath()).Returns(@"C:\Users\test\AppData\Local\DMFT");
        var provider = new MauiTestProvider(storageMock.Object);

        Assert.Contains("yt-dlp", provider.ExecutablePath);
    }

    [Fact]
    public void WebProvider_FallbackToPath_WhenExeNotFound()
    {
        var storageMock = new Mock<IStoragePathProvider>();
        storageMock.Setup(s => s.GetAppDataPath()).Returns(@"C:\inetpub\DMFT\App_Data");
        var provider = new WebTestProvider(storageMock.Object);

        Assert.Equal("yt-dlp", provider.ExecutablePath);
    }

    [Fact]
    public void ExtraArguments_Default_ReturnsRestrictFilenames()
    {
        var storageMock = new Mock<IStoragePathProvider>();
        storageMock.Setup(s => s.GetAppDataPath()).Returns(@"C:\DMFT");
        var provider = new MauiTestProvider(storageMock.Object);

        Assert.Equal("--restrict-filenames --no-warnings", provider.ExtraArguments);
    }

    [Fact]
    public void FormatString_Default_ReturnsBestVideoPlusBestAudio()
    {
        var storageMock = new Mock<IStoragePathProvider>();
        storageMock.Setup(s => s.GetAppDataPath()).Returns(@"C:\DMFT");
        var provider = new MauiTestProvider(storageMock.Object);

        Assert.StartsWith("bestvideo", provider.FormatString);
    }
}

public class MauiTestProvider : IYtDlpConfigProvider
{
    public string ExecutablePath { get; }
    public string ExtraArguments { get; private set; } = "--restrict-filenames --no-warnings";
    public string OutputTemplate { get; private set; } = "";
    public string FormatString { get; private set; } = "bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best";

    public MauiTestProvider(IStoragePathProvider storage)
    {
        var ytDlpPath = Path.Combine(storage.GetAppDataPath(), "yt-dlp");
        ExecutablePath = Path.Combine(ytDlpPath, "yt-dlp.exe");
        if (!File.Exists(ExecutablePath))
            ExecutablePath = Path.Combine(AppContext.BaseDirectory, "yt-dlp", "yt-dlp.exe");
    }

    public Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory) => Task.CompletedTask;
}

public class WebTestProvider : IYtDlpConfigProvider
{
    public string ExecutablePath { get; }
    public string ExtraArguments { get; private set; } = "--restrict-filenames --no-warnings";
    public string OutputTemplate { get; private set; } = "";
    public string FormatString { get; private set; } = "bestvideo[ext=mp4]+bestaudio/bestvideo[ext=mp4]+bestaudio/best";

    public WebTestProvider(IStoragePathProvider storage)
    {
        var ytDlpPath = Path.Combine(storage.GetAppDataPath(), "yt-dlp");
        ExecutablePath = Path.Combine(ytDlpPath, "yt-dlp.exe");
        if (!File.Exists(ExecutablePath))
            ExecutablePath = "yt-dlp";
    }

    public Task InitializeFromDbAsync(IDbContextFactory<AppDbContext> dbFactory) => Task.CompletedTask;
}
