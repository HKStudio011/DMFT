using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DMFT.Test.Core.Services;

public class AppSettingsServiceTests
{
    [Fact]
    public async Task InitAsync_LoadsAllSettings()
    {
        var (svc, ctx) = CreateServiceWithSettings();

        await svc.InitAsync();

        Assert.Equal("--no-warnings", svc.Get("ytdlp_extra_args"));
        Assert.Equal("%(title)s.%(ext)s", svc.Get("ytdlp_output_template"));
        Assert.Equal("bestvideo+bestaudio", svc.Get("ytdlp_format"));
        Assert.Equal(3, svc.GetInt("maxConcurrent", 0));
        Assert.Equal(5000, svc.GetInt("delayBetweenMs", 0));
        Assert.Equal("dark", svc.Get("theme"));
        Assert.Equal("gold", svc.Get("accentColor"));
        Assert.Equal(@"C:\Downloads", svc.Get("defaultPath"));
    }

    [Fact]
    public async Task InitAsync_EmptyDb_ReturnsNulls()
    {
        var (svc, _) = CreateEmptyService();

        await svc.InitAsync();

        Assert.Null(svc.Get("ytdlp_extra_args"));
        Assert.Equal(42, svc.GetInt("missing", 42));
    }

    [Fact]
    public async Task SetAsync_UpsertsAndCaches()
    {
        var (svc, ctx) = CreateEmptyService();

        await svc.SetAsync("theme", "dark");

        Assert.Equal("dark", svc.Get("theme"));
        var db = ctx.Object;
        var saved = await db.AppSettings.FindAsync("theme");
        Assert.Equal("dark", saved?.Value);
    }

    [Fact]
    public async Task SetAsync_OverridesExisting()
    {
        var (svc, _) = CreateServiceWithSettings();

        await svc.InitAsync();
        await svc.SetAsync("theme", "light");

        Assert.Equal("light", svc.Get("theme"));
    }

    [Fact]
    public void Get_NotInitialized_ReturnsNull()
    {
        var (svc, _) = CreateEmptyService();

        Assert.Null(svc.Get("anything"));
    }

    [Fact]
    public void GetInt_NotInitialized_ReturnsDefault()
    {
        var (svc, _) = CreateEmptyService();

        Assert.Equal(99, svc.GetInt("anything", 99));
    }

    [Fact]
    public async Task GetInt_ParsesInteger()
    {
        var (svc, _) = CreateEmptyService();
        await svc.InitAsync();

        Assert.Equal(0, svc.GetInt("missing", 0));
    }

    private static (AppSettingsService, Mock<AppDbContext>) CreateServiceWithSettings()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        var ctx = new AppDbContext(options);
        ctx.AppSettings.AddRange(
            new AppSetting { Id = "ytdlp_extra_args", Value = "--no-warnings" },
            new AppSetting { Id = "ytdlp_output_template", Value = "%(title)s.%(ext)s" },
            new AppSetting { Id = "ytdlp_format", Value = "bestvideo+bestaudio" },
            new AppSetting { Id = "maxConcurrent", Value = "3" },
            new AppSetting { Id = "delayBetweenMs", Value = "5000" },
            new AppSetting { Id = "theme", Value = "dark" },
            new AppSetting { Id = "accentColor", Value = "gold" },
            new AppSetting { Id = "defaultPath", Value = @"C:\Downloads" }
        );
        ctx.SaveChanges();

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName).Options));

        var svc = new AppSettingsService(factory.Object);

        var mockCtx = new Mock<AppDbContext>(options);

        return (svc, mockCtx);
    }

    private static (AppSettingsService, Mock<AppDbContext>) CreateEmptyService()
    {
        var dbName = $"TestDb_{Guid.NewGuid()}";
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(dbName)
            .Options;
        _ = new AppDbContext(options);

        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(() => new AppDbContext(new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(dbName).Options));

        var svc = new AppSettingsService(factory.Object);

        var mockCtx = new Mock<AppDbContext>(options);

        return (svc, mockCtx);
    }
}
