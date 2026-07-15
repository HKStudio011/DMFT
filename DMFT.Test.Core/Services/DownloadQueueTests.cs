using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;
using System.Threading;

namespace DMFT.Test.Core.Services;

public class DownloadQueueTests
{
    private static DownloadQueue CreateQueue(out Mock<IDownloadEngine> engineMock)
    {
        engineMock = new Mock<IDownloadEngine>();
        engineMock.Setup(e => e.StartDownloadAsync(It.IsAny<DownloadItem>()))
            .Returns(Task.CompletedTask);
        var serviceMock = new Mock<DownloadService>(Mock.Of<IDbContextFactory<AppDbContext>>());
        return new DownloadQueue(engineMock.Object, serviceMock.Object);
    }

    private static DownloadQueue CreateQueue()
    {
        return CreateQueue(out _);
    }

    private static async Task<bool> WaitForProcessingAsync(Func<bool> check, int timeoutMs = 5000)
    {
        for (var i = 0; i < timeoutMs / 50; i++)
        {
            if (check()) return true;
            await Task.Delay(50);
        }
        return false;
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

    [Fact]
    public async Task EnqueueDownloadAsync_StartsProcessing_CallsEngineWithItem()
    {
        var queue = CreateQueue(out var engineMock);
        var item = new DownloadItem { Url = "https://youtube.com/watch?v=abc", Platform = "YouTube" };
        var callCount = 0;
        engineMock.Setup(e => e.StartDownloadAsync(It.IsAny<DownloadItem>()))
            .Returns(Task.CompletedTask)
            .Callback<DownloadItem>(i => Interlocked.Increment(ref callCount));

        await queue.EnqueueDownloadAsync(item);

        var called = await WaitForProcessingAsync(() => callCount > 0);
        Assert.True(called, "Engine.StartDownloadAsync was not called within timeout");
        engineMock.Verify(e => e.StartDownloadAsync(It.Is<DownloadItem>(i => i.Url == item.Url)), Times.Once);
    }

    [Fact]
    public async Task EnqueueDownloadAsync_MultipleItems_ProcessesAll()
    {
        var queue = CreateQueue(out var engineMock);
        var item1 = new DownloadItem { Id = Guid.NewGuid(), Url = "http://a.com", Platform = "YouTube" };
        var item2 = new DownloadItem { Id = Guid.NewGuid(), Url = "http://b.com", Platform = "TikTok" };
        var callCount = 0;
        engineMock.Setup(e => e.StartDownloadAsync(It.IsAny<DownloadItem>()))
            .Returns(Task.CompletedTask)
            .Callback<DownloadItem>(i => Interlocked.Increment(ref callCount));

        await queue.EnqueueDownloadAsync(item1);
        await queue.EnqueueDownloadAsync(item2);

        var called = await WaitForProcessingAsync(() => callCount >= 2);
        Assert.True(called, $"Expected at least 2 engine calls but got {callCount} within timeout");
        engineMock.Verify(e => e.StartDownloadAsync(It.IsAny<DownloadItem>()), Times.AtLeast(2));
    }
}
