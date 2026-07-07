using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DMFT.Test.Core.Services;

public class DownloadQueueTests
{
    private readonly Mock<IDownloadEngine> _engineMock;
    private readonly Mock<DownloadService> _serviceMock;
    private readonly DownloadQueue _queue;

    public DownloadQueueTests()
    {
        _engineMock = new Mock<IDownloadEngine>();
        _serviceMock = new Mock<DownloadService>(Mock.Of<IDbContextFactory<AppDbContext>>());
        _queue = new DownloadQueue(_engineMock.Object, _serviceMock.Object);
    }

    [Fact]
    public void MaxConcurrent_Default_ReturnsOne()
    {
        Assert.Equal(1, _queue.MaxConcurrent);
    }

    [Fact]
    public void MaxConcurrent_SetBelowOne_ClampsToOne()
    {
        _queue.MaxConcurrent = -5;

        Assert.Equal(1, _queue.MaxConcurrent);
    }

    [Fact]
    public void DelayBetweenMs_Default_Returns2000()
    {
        Assert.Equal(2000, _queue.DelayBetweenMs);
    }

    [Fact]
    public void DelayBetweenMs_SetBelow500_ClampsTo500()
    {
        _queue.DelayBetweenMs = 100;

        Assert.Equal(500, _queue.DelayBetweenMs);
    }

    [Fact]
    public void IsProcessing_Initially_ReturnsFalse()
    {
        Assert.False(_queue.IsProcessing);
    }

    [Fact]
    public async Task EnqueueDownloadAsync_NullItem_DoesNotThrow()
    {
        var ex = await Record.ExceptionAsync(() => _queue.EnqueueDownloadAsync(null!));

        Assert.Null(ex);
    }

    [Fact]
    public async Task EnqueueDownloadAsync_ValidItem_SetsStatusWaiting()
    {
        var item = new DownloadItem();

        await _queue.EnqueueDownloadAsync(item);

        Assert.Equal(StatusCodes.Waiting, item.Status);
    }

    [Fact]
    public async Task EnqueueDownloadAsync_ValidItem_FiresOnQueueUpdated()
    {
        var fired = false;
        _queue.OnQueueUpdated += () => fired = true;
        var item = new DownloadItem();

        await _queue.EnqueueDownloadAsync(item);

        Assert.True(fired);
    }
}
