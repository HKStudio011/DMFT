using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Microsoft.EntityFrameworkCore;
using Moq;

namespace DMFT.Test.Core.Services;

public class AppSettingsReaderTests
{
    [Fact]
    public async Task ReadYtDlpConfigAsync_SettingsExist_ReturnsValues()
    {
        using var context = CreateDbContextWithSettings();
        var dbFactory = CreateDbFactory(context);

        var (extraArgs, outputTemplate, formatString) = await AppSettingsReader.ReadYtDlpConfigAsync(dbFactory.Object);

        Assert.Equal("--no-warnings", extraArgs);
        Assert.Equal("%(title)s.%(ext)s", outputTemplate);
        Assert.Equal("bestvideo+bestaudio", formatString);
    }

    [Fact]
    public async Task ReadYtDlpConfigAsync_NoSettings_ReturnsNulls()
    {
        using var context = CreateEmptyDbContext();
        var dbFactory = CreateDbFactory(context);

        var (extraArgs, outputTemplate, formatString) = await AppSettingsReader.ReadYtDlpConfigAsync(dbFactory.Object);

        Assert.Null(extraArgs);
        Assert.Null(outputTemplate);
        Assert.Null(formatString);
    }

    [Fact]
    public async Task ReadQueueSettingsAsync_SettingsExist_ReturnsValues()
    {
        using var context = CreateDbContextWithSettings();
        var dbFactory = CreateDbFactory(context);

        var (maxConcurrent, delayBetweenMs) = await AppSettingsReader.ReadQueueSettingsAsync(dbFactory.Object);

        Assert.Equal(3, maxConcurrent);
        Assert.Equal(5000, delayBetweenMs);
    }

    [Fact]
    public async Task ReadQueueSettingsAsync_NoSettings_ReturnsNulls()
    {
        using var context = CreateEmptyDbContext();
        var dbFactory = CreateDbFactory(context);

        var (maxConcurrent, delayBetweenMs) = await AppSettingsReader.ReadQueueSettingsAsync(dbFactory.Object);

        Assert.Null(maxConcurrent);
        Assert.Null(delayBetweenMs);
    }

    private static AppDbContext CreateEmptyDbContext()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"TestDb_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static AppDbContext CreateDbContextWithSettings()
    {
        var ctx = CreateEmptyDbContext();
        ctx.AppSettings.AddRange(
            new AppSetting { Id = "ytdlp_extra_args", Value = "--no-warnings" },
            new AppSetting { Id = "ytdlp_output_template", Value = "%(title)s.%(ext)s" },
            new AppSetting { Id = "ytdlp_format", Value = "bestvideo+bestaudio" },
            new AppSetting { Id = "maxConcurrent", Value = "3" },
            new AppSetting { Id = "delayBetweenMs", Value = "5000" }
        );
        ctx.SaveChanges();
        return ctx;
    }

    private static Mock<IDbContextFactory<AppDbContext>> CreateDbFactory(AppDbContext context)
    {
        var factory = new Mock<IDbContextFactory<AppDbContext>>();
        factory.Setup(f => f.CreateDbContextAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(context);
        return factory;
    }
}
