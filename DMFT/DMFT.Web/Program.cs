using DMFT.Core.Data;
using DMFT.Core.Services;
using DMFT.Shared.Services;
using DMFT.Web.Components;
using DMFT.Web.Services;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents()
    .AddInteractiveWebAssemblyComponents();

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
builder.Services.AddSingleton<ITikTokSoundExtractor, TikTokSoundExtractor>();
builder.Services.AddSingleton<IDownloadQueue, DownloadQueue>();
builder.Services.AddSingleton<ToastService>();

// App update
builder.Services.AddSingleton<IAppUpdateService>(sp =>
{
    var http = new HttpClient { Timeout = TimeSpan.FromSeconds(30) };
    http.DefaultRequestHeaders.UserAgent.ParseAdd("DMFT/2.0");
    return new AppUpdateService(http);
});

var app = builder.Build();

// Auto-apply pending EF Core migrations on startup
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
app.UseHttpsRedirection();
app.UseAntiforgery();
app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode()
    .AddInteractiveWebAssemblyRenderMode()
    .AddAdditionalAssemblies(
        typeof(DMFT.Shared._Imports).Assembly,
        typeof(DMFT.Web.Client._Imports).Assembly);

app.Run();
