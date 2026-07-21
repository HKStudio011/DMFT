using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DMFT.Test.Web;

public class WebAppFixture : WebApplicationFactory<Program>, IAsyncLifetime
{
    public string BaseUrl { get; private set; } = null!;

    public WebAppFixture()
    {
        UseKestrel(0);
    }

    public async ValueTask InitializeAsync()
    {
        var client = CreateDefaultClient();
        BaseUrl = client.BaseAddress?.ToString().TrimEnd('/') ?? "http://localhost";

        try
        {
            using var scope = Services.CreateScope();
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            using var context = await factory.CreateDbContextAsync();
            await context.Database.MigrateAsync();
            var settings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();
            await settings.InitAsync();
        }
        catch (Exception ex)
        {
            var logger = Services.GetRequiredService<ILogger<WebAppFixture>>();
            logger.LogWarning(ex, "Database migration failed (non-fatal)");
        }
    }

    public new async ValueTask DisposeAsync()
    {
        Dispose();
    }

    public async Task ResetDatabaseAsync()
    {
        using var scope = Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = await factory.CreateDbContextAsync();
        context.Database.EnsureCreated();
        context.RemoveRange(context.Set<DownloadItem>());
        await context.SaveChangesAsync();
    }

    public async Task SeedDownloadItemAsync(DownloadItem item)
    {
        var svc = Services.GetRequiredService<DownloadService>();
        await svc.AddDownloadAsync(item);
    }

    public async Task SeedMainItemAsync(string url, string platform = "YouTube", int mode = 1, string videoId = "test123")
    {
        var svc = Services.GetRequiredService<DownloadService>();
        await svc.AddDownloadAsync(new DownloadItem
        {
            Id = Guid.NewGuid(),
            Url = url,
            Platform = platform,
            VideoId = videoId,
            Status = StatusCodes.New,
            DownloadMode = mode,
            Time = DateTime.UtcNow
        });
    }

    public async Task SeedHistoryItemAsync(string url, string platform = "YouTube", int statusCode = 4)
    {
        var svc = Services.GetRequiredService<DownloadService>();
        await svc.AddDownloadAsync(new DownloadItem
        {
            Id = Guid.NewGuid(),
            Url = url,
            Platform = platform,
            VideoId = Guid.NewGuid().ToString()[..8],
            Status = statusCode,
            DownloadMode = 1,
            Time = DateTime.UtcNow.AddHours(-1)
        });
    }

    public async Task SeedAppSettingAsync(string key, string value)
    {
        var svc = Services.GetRequiredService<DownloadService>();
        await svc.SetAppSettingAsync(key, value);
    }
}
