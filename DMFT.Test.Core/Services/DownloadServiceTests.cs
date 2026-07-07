using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DMFT.Test.Core.Services;

public class DownloadServiceTests
{
    private static Mock<IDbContextFactory<AppDbContext>> CreateFactory(Action<AppDbContext> seed)
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        using var seedContext = new AppDbContext(options);
        seed(seedContext);
        seedContext.SaveChanges();
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName).Options));
        return factory;
    }

    [Fact]
    public async Task GetMainLinksAsync_ReturnsItemsWithStatusUnder4()
    {
        var factory = CreateFactory(ctx =>
        {
            ctx.DownloadItems.AddRange(
                new DownloadItem { Id = Guid.NewGuid(), Status = StatusCodes.New, Url = "http://a.com", Platform = "YouTube" },
                new DownloadItem { Id = Guid.NewGuid(), Status = StatusCodes.Waiting, Url = "http://b.com", Platform = "TikTok" },
                new DownloadItem { Id = Guid.NewGuid(), Status = StatusCodes.Success, Url = "http://c.com", Platform = "YouTube" }
            );
        });
        var svc = new DownloadService(factory.Object);

        var result = await svc.GetMainLinksAsync();

        Assert.Equal(2, result.Count);
        Assert.All(result, item => Assert.True(item.Status < 4));
    }

    [Fact]
    public async Task GetMainLinksAsync_EmptyDb_ReturnsEmptyList()
    {
        var factory = CreateFactory(_ => { });
        var svc = new DownloadService(factory.Object);

        var result = await svc.GetMainLinksAsync();

        Assert.Empty(result);
    }

    [Fact]
    public async Task GetHistoryAsync_ReturnsSuccessCanceledAndErrorItems()
    {
        var factory = CreateFactory(ctx =>
        {
            ctx.DownloadItems.AddRange(
                new DownloadItem { Id = Guid.NewGuid(), Status = StatusCodes.Success, Url = "http://done.com", Platform = "YouTube" },
                new DownloadItem { Id = Guid.NewGuid(), Status = StatusCodes.Canceled, Url = "http://cancel.com", Platform = "TikTok" },
                new DownloadItem { Id = Guid.NewGuid(), Status = StatusCodes.Error, Url = "http://err.com", Platform = "YouTube" },
                new DownloadItem { Id = Guid.NewGuid(), Status = StatusCodes.New, Url = "http://new.com", Platform = "TikTok" }
            );
        });
        var svc = new DownloadService(factory.Object);

        var result = await svc.GetHistoryAsync();

        Assert.Equal(3, result.Count);
    }

    [Fact]
    public async Task AddDownloadAsync_InsertsItemAndSaves()
    {
        var factory = CreateFactory(_ => { });
        var svc = new DownloadService(factory.Object);
        var item = new DownloadItem
        {
            Id = Guid.NewGuid(),
            Url = "https://youtube.com/watch?v=test",
            Platform = "YouTube",
            Status = StatusCodes.New
        };

        await svc.AddDownloadAsync(item);

        var all = await svc.GetMainLinksAsync();
        Assert.Contains(all, i => i.Id == item.Id && i.Url == item.Url);
    }

    [Fact]
    public async Task UpdateDownloadAsync_UpdatesExistingItem()
    {
        var itemId = Guid.NewGuid();
        var factory = CreateFactory(ctx =>
        {
            ctx.DownloadItems.Add(new DownloadItem
            {
                Id = itemId,
                Url = "http://original.com",
                Platform = "YouTube",
                Status = StatusCodes.New
            });
        });
        var svc = new DownloadService(factory.Object);
        var item = await svc.GetMainLinksAsync();
        var existing = item.First();
        existing.Status = StatusCodes.Success;
        existing.DownloadedBytes = 1000;

        await svc.UpdateDownloadAsync(existing);

        var reloaded = await svc.GetMainLinksAsync();
        Assert.Empty(reloaded);
        var history = await svc.GetHistoryAsync();
        var updated = Assert.Single(history);
        Assert.Equal(StatusCodes.Success, updated.Status);
        Assert.Equal(1000, updated.DownloadedBytes);
    }

    [Fact]
    public async Task MoveToHistoryAsync_UpdatesStatusAndProgress()
    {
        var itemId = Guid.NewGuid();
        var factory = CreateFactory(ctx =>
        {
            ctx.DownloadItems.Add(new DownloadItem
            {
                Id = itemId,
                Url = "http://example.com",
                Platform = "YouTube",
                Status = StatusCodes.New
            });
        });
        var svc = new DownloadService(factory.Object);
        var moved = new DownloadItem
        {
            Id = itemId,
            Status = StatusCodes.Success,
            DownloadedBytes = 500,
            TotalBytes = 1000,
            Speed = 2.5,
            EtaSeconds = 5,
            ProgressPercent = 50,
            CurrentFileName = "test.mp4"
        };

        await svc.MoveToHistoryAsync(moved);

        var history = await svc.GetHistoryAsync();
        var item = Assert.Single(history);
        Assert.Equal(StatusCodes.Success, item.Status);
        Assert.Equal(500, item.DownloadedBytes);
        Assert.Equal(1000, item.TotalBytes);
        Assert.Equal("test.mp4", item.CurrentFileName);
    }

    [Fact]
    public async Task DeleteDownloadAsync_RemovesItem()
    {
        var itemId = Guid.NewGuid();
        var factory = CreateFactory(ctx =>
        {
            ctx.DownloadItems.Add(new DownloadItem
            {
                Id = itemId,
                Url = "http://delete-me.com",
                Platform = "TikTok",
                Status = StatusCodes.New
            });
        });
        var svc = new DownloadService(factory.Object);

        await svc.DeleteDownloadAsync(itemId);

        var all = await svc.GetMainLinksAsync();
        Assert.Empty(all);
    }

    [Fact]
    public async Task SetAppSettingAsync_UpsertsSetting()
    {
        var factory = CreateFactory(_ => { });
        var svc = new DownloadService(factory.Object);

        await svc.SetAppSettingAsync("theme", "dark");

        var value = await svc.GetAppSettingAsync("theme");
        Assert.Equal("dark", value);
    }

    [Fact]
    public async Task SaveDefaultPathAsync_SavesAndRetrievesPath()
    {
        var factory = CreateFactory(_ => { });
        var svc = new DownloadService(factory.Object);

        await svc.SaveDefaultPathAsync(@"C:\Downloads\DMFT");

        var path = await svc.GetDefaultPathAsync();
        Assert.Equal(@"C:\Downloads\DMFT", path);
    }
}
