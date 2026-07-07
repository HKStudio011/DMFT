using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DMFT.Test.Core.Services;

public class DownloadQueueTests
{
    private static DownloadQueue CreateQueue()
    {
        var engineMock = new Mock<IDownloadEngine>();
        var serviceMock = new Mock<DownloadService>(Mock.Of<IDbContextFactory<AppDbContext>>());
        return new DownloadQueue(engineMock.Object, serviceMock.Object);
    }

    [Fact]
    public void MaxConcurrent_Default_ReturnsOne()
    {
        var queue = CreateQueue();

        var result = queue.MaxConcurrent;

        Assert.Equal(1, result);
    }

    [Fact]
    public void MaxConcurrent_SetBelowOne_ClampsToOne()
    {
        var queue = CreateQueue();

        queue.MaxConcurrent = -5;

        Assert.Equal(1, queue.MaxConcurrent);
    }

    [Fact]
    public void DelayBetweenMs_Default_Returns2000()
    {
        var queue = CreateQueue();

        var result = queue.DelayBetweenMs;

        Assert.Equal(2000, result);
    }

    [Fact]
    public void DelayBetweenMs_SetBelow500_ClampsTo500()
    {
        var queue = CreateQueue();

        queue.DelayBetweenMs = 100;

        Assert.Equal(500, queue.DelayBetweenMs);
    }

    [Fact]
    public void IsProcessing_Initially_ReturnsFalse()
    {
        var queue = CreateQueue();

        var result = queue.IsProcessing;

        Assert.False(result);
    }

    [Fact]
    public async Task EnqueueDownloadAsync_NullItem_DoesNotThrow()
    {
        var queue = CreateQueue();

        var ex = await Record.ExceptionAsync(() => queue.EnqueueDownloadAsync(null!));

        Assert.Null(ex);
    }

    [Fact]
    public async Task EnqueueDownloadAsync_ValidItem_SetsStatusWaiting()
    {
        var queue = CreateQueue();
        var item = new DownloadItem();

        await queue.EnqueueDownloadAsync(item);

        Assert.Equal(StatusCodes.Waiting, item.Status);
    }

    [Fact]
    public async Task EnqueueDownloadAsync_ValidItem_FiresOnQueueUpdated()
    {
        var queue = CreateQueue();
        var item = new DownloadItem();
        var fired = false;
        queue.OnQueueUpdated += () => fired = true;

        await queue.EnqueueDownloadAsync(item);

        Assert.True(fired);
    }
}
