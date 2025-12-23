using CommunityToolkit.Maui;
using DMFT.Model;
using DMFT.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace DMFT
{
    public static class MauiProgram
    {
        public static MauiApp CreateMauiApp()
        {
            var builder = MauiApp.CreateBuilder();
            builder
                .UseMauiApp<App>()
                .UseMauiCommunityToolkit()
                .ConfigureFonts(fonts =>
                {
                    fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                });

            builder.Services.AddMauiBlazorWebView();
            // Toast service first
            builder.Services.AddSingleton<ToastService>();
            builder.Services.AddSingleton<MainContainer>( provider =>
            {
                var container = new MainContainer();
                container.Toast = provider.GetService<ToastService>();
                return container;
            });
            builder.Services.AddSingleton<HistoryContainer>( provider =>
            {
                var container = new HistoryContainer();
                container.Toast = provider.GetService<ToastService>();
                return container;
            });
            // TikTok downloader services registrations
            builder.Services.AddSingleton<ITikTokLinkParser, TikTokLinkParser>();
            builder.Services.AddSingleton<ITikTokDownloaderService, TikTokDownloaderService>();
            builder.Services.AddSingleton<IMediaDownloader, MediaDownloader>();
            builder.Services.AddSingleton<IDownloadEngineAdapter, DownloadEngineAdapter>();
            builder.Services.AddSingleton<IDownloadQueue, DMFT.Services.DownloadQueue>();
            builder.Services.AddSingleton<YtDlpConfig>();
            builder.Services.AddSingleton<SeleniumServices>();

#if DEBUG
            builder.Services.AddBlazorWebViewDeveloperTools();
            builder.Logging.AddDebug();
#endif

            // Build the app and wire the real queue instance from DI
            var app = builder.Build();

            // Return the ready app instance
            return app;
        }
    }
}
