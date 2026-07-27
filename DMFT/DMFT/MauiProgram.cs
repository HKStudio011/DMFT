using CommunityToolkit.Maui;
using DMFT.Data;
using DMFT.Services;
using DMFT.Shared.Utilities;
using DMFT.Utilities;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace DMFT;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        InteractiveRenderSettings.ConfigureBlazorHybridRenderModes();
#if WINDOWS
        TargetPlatform.SetCurrentPlatform(TargetPlatform.Platform.Windows | TargetPlatform.Platform.Maui);
#endif


        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Platform services
        builder.Services.AddSingleton<IStoragePathProvider>(_ =>
        {
#if WINDOWS
            var appDataPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "DMFT");
            return new StoragePathProvider(appDataPath);
#else
            var appDataPath = FileSystem.Current.AppDataDirectory;
            return new StoragePathProvider(appDataPath);
#endif
        });

        // App settings (must be registered before YtDlpConfigProvider)
        builder.Services.AddSingleton<IAppSettingsService, AppSettingsService>();

        // yt-dlp config
        builder.Services.AddSingleton<IYtDlpConfigProvider, YtDlpConfigProvider>();

        // EF Core + SQLite
        builder.Services.AddDbContextFactory<AppDbContext>((sp, options) =>
        {
            var storage = sp.GetRequiredService<IStoragePathProvider>();
            options.UseSqlite($"Data Source={storage.GetDatabasePath()}");
        });

        // Core services
        builder.Services.AddSingleton<DownloadService>();
        builder.Services.AddSingleton<IVideoLinkParser, VideoLinkParser>();
        builder.Services.AddSingleton<IMediaDownloader, YtDlpService>();
        builder.Services.AddSingleton<IYtDlpUpdateService, YtDlpUpdateService>();
        builder.Services.AddSingleton<IDownloadEngine, DownloadEngine>();
        builder.Services.AddSingleton<ITikTokSoundExtractor, TikTokSoundExtractor>();
        builder.Services.AddSingleton<IDownloadQueue, DownloadQueue>();
        builder.Services.AddSingleton<ToastService>();

        // App update
        builder.Services.AddSingleton<IAppUpdateService>(sp =>
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(60) };
            return new AppUpdateService(http);
        });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        var app = builder.Build();

        // Auto-apply pending EF Core migrations on startup
        try
        {
            using (var scope = app.Services.CreateScope())
            {
                var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
                using (var context = factory.CreateDbContext())
                {
                    context.Database.Migrate();
                }

                // Data migration: set InHistory for legacy Canceled/Success items
                using (var migrateCtx = factory.CreateDbContext())
                {
                    var legacyHistorical = migrateCtx.DownloadItems
                        .Where(x => !x.InHistory && (x.Status == 3 || x.Status == 4))
                        .ToList();
                    if (legacyHistorical.Count > 0)
                    {
                        foreach (var item in legacyHistorical)
                            item.InHistory = true;
                        migrateCtx.SaveChanges();
                    }
                }

                var settings = scope.ServiceProvider.GetRequiredService<IAppSettingsService>();
                settings.InitAsync().GetAwaiter().GetResult();
            }
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Database migration failed: {ex.Message}");
        }

        return app;
    }
}
