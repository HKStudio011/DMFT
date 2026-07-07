using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DMFT.Test.Core.Services;

public class DownloadEngineTests
{
    private readonly Mock<IDbContextFactory<AppDbContext>> _factoryMock;
    private readonly Mock<IMediaDownloader> _mediaMock;
    private readonly DownloadService _svc;
    private readonly Mock<ITikTokSoundExtractor> _soundMock;
    private readonly DownloadEngine _engine;
    private readonly string _dbName;

    public DownloadEngineTests()
    {
        _dbName = $"EngineTest_{Guid.NewGuid()}";
        _mediaMock = new Mock<IMediaDownloader>();
        _factoryMock = new Mock<IDbContextFactory<AppDbContext>>();
        _factoryMock.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(_dbName).Options));
        _svc = new DownloadService(_factoryMock.Object);
        _soundMock = new Mock<ITikTokSoundExtractor>();
        _engine = new DownloadEngine(_mediaMock.Object, _svc, _soundMock.Object);
    }

    private async Task<DownloadItem> CreateItemAsync(int mode)
    {
        var item = new DownloadItem
        {
            Id = Guid.NewGuid(),
            Url = "https://youtube.com/watch?v=test",
            Platform = "YouTube",
            SaveLocation = @"C:\Downloads",
            VideoId = "test",
            DownloadMode = mode,
            Status = StatusCodes.New
        };
        await _svc.AddDownloadAsync(item);
        return item;
    }

    [Fact]
    public async Task StartDownloadAsync_VideoOnly_CallsMediaDownloader()
    {
        _mediaMock.Setup(m => m.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(Task.CompletedTask);
        var item = await CreateItemAsync((int)DownloadMode.Video);

        await _engine.StartDownloadAsync(item);

        _mediaMock.Verify(m => m.DownloadAsync(item.Url,
            It.Is<string>(p => p.EndsWith("_video.mp4")), true), Times.Once);
    }

    [Fact]
    public async Task StartDownloadAsync_AudioOnly_CallsDownloadAudio()
    {
        _mediaMock.Setup(m => m.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var item = await CreateItemAsync((int)DownloadMode.Audio);

        await _engine.StartDownloadAsync(item);

        _mediaMock.Verify(m => m.DownloadAudioAsync(item.Url,
            It.Is<string>(p => p.EndsWith("_audio.mp3"))), Times.Once);
    }

    [Fact]
    public async Task StartDownloadAsync_VideoAndAudio_CallsBoth()
    {
        _mediaMock.Setup(m => m.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(Task.CompletedTask);
        _mediaMock.Setup(m => m.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var item = await CreateItemAsync((int)(DownloadMode.Video | DownloadMode.Audio));

        await _engine.StartDownloadAsync(item);

        _mediaMock.Verify(m => m.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), true), Times.Once);
        _mediaMock.Verify(m => m.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()), Times.Once);
    }

    [Fact]
    public async Task StartDownloadAsync_OriginAudio_CallsSoundExtractor()
    {
        _soundMock.Setup(s => s.GetOriginalSoundAsync(It.IsAny<string>()))
            .ReturnsAsync(("Original Sound", "https://sound-url.com/original.mp3"));
        _mediaMock.Setup(m => m.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .Returns(Task.CompletedTask);
        var item = await CreateItemAsync((int)DownloadMode.OriginAudio);

        await _engine.StartDownloadAsync(item);

        _soundMock.Verify(s => s.GetOriginalSoundAsync(item.Url), Times.Once);
    }

    [Fact]
    public async Task StartDownloadAsync_OnSuccess_SetsStatusSuccess()
    {
        _mediaMock.Setup(m => m.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), true))
            .Returns(Task.CompletedTask);
        var item = await CreateItemAsync((int)DownloadMode.Video);

        await _engine.StartDownloadAsync(item);

        Assert.Equal(StatusCodes.Success, item.Status);
    }

    [Fact]
    public async Task StartDownloadAsync_OnMediaError_SetsVideoError()
    {
        _mediaMock.Setup(m => m.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), true))
            .ThrowsAsync(new Exception("yt-dlp failed"));
        var item = await CreateItemAsync((int)DownloadMode.Video);

        await _engine.StartDownloadAsync(item);

        Assert.Equal(StatusCodes.VideoError, item.Status);
    }

    [Fact]
    public async Task StartDownloadAsync_VideoAndAudioError_SetsVideoError()
    {
        _mediaMock.Setup(m => m.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), true))
            .ThrowsAsync(new Exception("video failed"));
        _mediaMock.Setup(m => m.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("audio failed"));
        var item = await CreateItemAsync((int)(DownloadMode.Video | DownloadMode.Audio));

        await _engine.StartDownloadAsync(item);

        Assert.Equal(StatusCodes.VideoError, item.Status);
    }

    [Fact]
    public async Task StartDownloadAsync_VideoAndOriginError_SetsVideoAudioOriginError()
    {
        _mediaMock.Setup(m => m.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), true))
            .ThrowsAsync(new Exception("video failed"));
        _soundMock.Setup(s => s.GetOriginalSoundAsync(It.IsAny<string>()))
            .ReturnsAsync(("Sound", "https://sound.url"));
        _mediaMock.Setup(m => m.DownloadAudioAsync(It.Is<string>(s => s.Contains("sound.url")), It.IsAny<string>()))
            .ThrowsAsync(new Exception("origin failed"));
        var item = await CreateItemAsync((int)(DownloadMode.Video | DownloadMode.OriginAudio));

        await _engine.StartDownloadAsync(item);

        Assert.Equal(StatusCodes.VideoAudioOriginError, item.Status);
    }

    [Fact]
    public async Task StartDownloadAsync_OriginAudioOnlyError_SetsAudioOriginError()
    {
        _soundMock.Setup(s => s.GetOriginalSoundAsync(It.IsAny<string>()))
            .ReturnsAsync(("Sound", "https://sound.url"));
        _mediaMock.Setup(m => m.DownloadAudioAsync(It.IsAny<string>(), It.IsAny<string>()))
            .ThrowsAsync(new Exception("origin failed"));
        var item = await CreateItemAsync((int)DownloadMode.OriginAudio);

        await _engine.StartDownloadAsync(item);

        Assert.Equal(StatusCodes.AudioOriginError, item.Status);
    }

    [Fact]
    public async Task StartDownloadAsync_NullItem_DoesNothing()
    {
        var ex = await Record.ExceptionAsync(() => _engine.StartDownloadAsync(null!));

        Assert.Null(ex);
        _mediaMock.Verify(m => m.DownloadAsync(It.IsAny<string>(), It.IsAny<string>(), true), Times.Never);
    }

    [Fact]
    public async Task CancelDownloadAsync_CallsMediaCancel()
    {
        var item = new DownloadItem();

        await _engine.CancelDownloadAsync(item);

        _mediaMock.Verify(m => m.CancelAsync(), Times.Once);
    }
}
