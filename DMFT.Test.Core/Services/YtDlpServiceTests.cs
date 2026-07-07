using DMFT.Core.Services;
using Moq;

namespace DMFT.Test.Core.Services;

public class YtDlpServiceTests
{
    private static YtDlpService CreateService(Action<Mock<IYtDlpConfigProvider>>? setup = null)
    {
        var configMock = new Mock<IYtDlpConfigProvider>();
        configMock.SetupGet(c => c.ExecutablePath).Returns("yt-dlp.exe");
        configMock.SetupGet(c => c.ExtraArguments).Returns("--no-warnings");
        configMock.SetupGet(c => c.FormatString).Returns("bestvideo+bestaudio");
        configMock.SetupGet(c => c.OutputTemplate).Returns("");
        setup?.Invoke(configMock);
        return new YtDlpService(configMock.Object);
    }

    [Fact]
    public void OnProgress_DelegatesFires_WithProgressData()
    {
        var service = CreateService();
        DownloadProgress? captured = null;
        service.OnProgress = p => captured = p;
        var progress = new DownloadProgress
        {
            Status = "downloading",
            DownloadedBytes = 5000,
            TotalBytes = 10000,
            Speed = 2500000.0,
            EtaSeconds = 30
        };

        service.OnProgress?.Invoke(progress);

        Assert.NotNull(captured);
        Assert.Equal("downloading", captured.Status);
        Assert.Equal(5000, captured.DownloadedBytes);
        Assert.Equal(10000, captured.TotalBytes);
        Assert.Equal(2500000.0, captured.Speed);
        Assert.Equal(30, captured.EtaSeconds);
    }

    [Fact]
    public void OnProgress_DefaultValues_WhenFieldsNotSet()
    {
        var service = CreateService();
        DownloadProgress? captured = null;
        service.OnProgress = p => captured = p;

        service.OnProgress?.Invoke(new DownloadProgress { Status = "finished" });

        Assert.NotNull(captured);
        Assert.Equal("finished", captured.Status);
        Assert.Equal(0, captured.DownloadedBytes);
        Assert.Equal(0, captured.TotalBytes);
        Assert.Equal(0, captured.Speed);
        Assert.Equal(0, captured.EtaSeconds);
    }

    [Fact]
    public void OnProgress_NullDelegate_DoesNotThrow()
    {
        var service = CreateService();

        var ex = Record.Exception(() =>
        {
            service.OnProgress?.Invoke(new DownloadProgress { Status = "test" });
        });

        Assert.Null(ex);
    }
}
