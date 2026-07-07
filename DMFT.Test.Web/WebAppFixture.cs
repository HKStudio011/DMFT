using DMFT.Core.Data;
using DMFT.Core.Entities;
using DMFT.Core.Services;
using DMFT.Shared.Services;
using DMFT.Web.Components;
using DMFT.Web.Services;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Linq;

namespace DMFT.Test.Web;

public class WebAppFixture : IAsyncLifetime
{
    private WebApplication? _app;
    public string BaseUrl { get; private set; } = null!;

    public async ValueTask InitializeAsync()
    {
        var webProjectPath = Path.GetFullPath(Path.Combine(
            AppContext.BaseDirectory, "..", "..", "..", "..", "DMFT", "DMFT.Web"));
        var builder = WebApplication.CreateBuilder(new WebApplicationOptions
        {
            ContentRootPath = webProjectPath,
            ApplicationName = "DMFT.Web",
            EnvironmentName = "Development"
        });

        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents()
            .AddInteractiveWebAssemblyComponents();

        builder.Services.AddSingleton<IFormFactor, FormFactor>();
        builder.Services.AddSingleton<IStoragePathProvider, StoragePathProvider>();
        builder.Services.AddSingleton<IFolderPicker, FolderPicker>();
        builder.Services.AddSingleton<IYtDlpConfigProvider, YtDlpConfigProvider>();
        builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var storage = sp.GetRequiredService<IStoragePathProvider>();
            options.UseSqlite($"Data Source={storage.GetDatabasePath()}");
        });
        builder.Services.AddSingleton<DownloadService>();
        builder.Services.AddSingleton<IVideoLinkParser, VideoLinkParser>();
        builder.Services.AddSingleton<IMediaDownloader, YtDlpService>();
        builder.Services.AddSingleton<IYtDlpUpdateService, YtDlpUpdateService>();
        builder.Services.AddSingleton<IDownloadEngine, DownloadEngine>();
        builder.Services.AddSingleton<ITikTokSoundExtractor, TikTokSoundExtractor>();
        builder.Services.AddSingleton<IDownloadQueue, DownloadQueue>();
        builder.Services.AddSingleton<DownloadQueue>();
        builder.Services.AddSingleton<ToastService>();
        builder.Services.AddSingleton<IAppUpdateService>(sp =>
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DMFT/2.0");
            return new AppUpdateService(http);
        });

        var app = builder.Build();

        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using (var context = factory.CreateDbContext())
                {
                    await context.Database.MigrateAsync();
                }

                var config = scope.ServiceProvider.GetRequiredService<IYtDlpConfigProvider>();
                var queue = scope.ServiceProvider.GetRequiredService<IDownloadQueue>();
                await Task.WhenAll(
                    config.InitializeFromDbAsync(factory),
                    queue.InitializeFromDbAsync(factory)
                );
            }
        }
        catch (Exception ex)
        {
            var logger = app.Services.GetRequiredService<ILogger<Program>>();
            logger.LogWarning(ex, "Database migration failed (non-fatal)");
        }

        if (app.Environment.IsDevelopment())
        {
            app.UseWebAssemblyDebugging();
        }
        else
        {
            app.UseExceptionHandler("/Error", createScopeForErrors: true);
            app.UseHsts();
        }
        app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
        app.UseAntiforgery();
        app.MapStaticAssets();
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode()
            .AddInteractiveWebAssemblyRenderMode()
            .AddAdditionalAssemblies(
                typeof(DMFT.Shared._Imports).Assembly,
                typeof(DMFT.Web.Client._Imports).Assembly);

        app.Urls.Add("http://127.0.0.1:0");
        await app.StartAsync();
        BaseUrl = app.Urls.First()!;
        _app = app;
    }

    public async ValueTask DisposeAsync()
    {
        if (_app is not null)
        {
            await _app.StopAsync();
            await _app.DisposeAsync();
        }
    }

    public async Task ResetDatabaseAsync()
    {
        if (_app is null) return;
        using var scope = _app.Services.CreateScope();
        var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
        using var context = await factory.CreateDbContextAsync();
        context.Database.EnsureCreated();
        context.RemoveRange(context.Set<DMFT.Core.Entities.DownloadItem>());
        await context.SaveChangesAsync();
    }

    public async Task SeedDownloadItemAsync(DMFT.Core.Entities.DownloadItem item)
    {
        if (_app is null) return;
        var svc = _app.Services.GetRequiredService<DownloadService>();
        await svc.AddDownloadAsync(item);
    }

    public async Task SeedMainItemAsync(string url, string platform = "YouTube", int mode = 1, string videoId = "test123")
    {
        if (_app is null) return;
        var svc = _app.Services.GetRequiredService<DownloadService>();
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
        if (_app is null) return;
        var svc = _app.Services.GetRequiredService<DownloadService>();
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
        if (_app is null) return;
        var svc = _app.Services.GetRequiredService<DownloadService>();
        await svc.SetAppSettingAsync(key, value);
    }
}
