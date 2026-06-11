using DMFT.Core.Data;
using DMFT.Core.Services;
using DMFT.Services;
using DMFT.Shared.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace DMFT;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();
        builder
            .UseMauiApp<App>()
            .ConfigureFonts(fonts =>
            {
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
            });

        // Platform services
        builder.Services.AddSingleton<IFormFactor, FormFactor>();
        builder.Services.AddSingleton<IStoragePathProvider, StoragePathProvider>();
        builder.Services.AddSingleton<IFolderPicker, FolderPicker>();

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
        builder.Services.AddSingleton<IDownloadQueue, DownloadQueue>();
        builder.Services.AddSingleton<ToastService>();

        // App update
        builder.Services.AddSingleton<IAppUpdateService>(sp =>
        {
            var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
            http.DefaultRequestHeaders.UserAgent.ParseAdd("DMFT/2.0");
            return new AppUpdateService(http);
        });

        builder.Services.AddMauiBlazorWebView();

#if DEBUG
        builder.Services.AddBlazorWebViewDeveloperTools();
        builder.Logging.AddDebug();
#endif

        return builder.Build();
    }
}
