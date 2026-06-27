using DMFT.Core.Data;
using Microsoft.EntityFrameworkCore;

namespace DMFT.Core.Services;

public static class AppSettingsReader
{
    public static async Task<(string? extraArgs, string? outputTemplate, string? formatString)> ReadYtDlpConfigAsync(IDbContextFactory<AppDbContext> dbFactory)
    {
        try
        {
            using var db = await dbFactory.CreateDbContextAsync();

            var extraArgs = (await db.AppSettings.FindAsync("ytdlp_extra_args"))?.Value;
            var outputTemplate = (await db.AppSettings.FindAsync("ytdlp_output_template"))?.Value;
            var formatString = (await db.AppSettings.FindAsync("ytdlp_format"))?.Value;

            return (extraArgs, outputTemplate, formatString);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppSettingsReader] Failed to read yt-dlp config: {ex.Message}");
            return (null, null, null);
        }
    }

    public static async Task<(int? maxConcurrent, int? delayBetweenMs)> ReadQueueSettingsAsync(IDbContextFactory<AppDbContext> dbFactory)
    {
        try
        {
            using var db = await dbFactory.CreateDbContextAsync();

            var conc = (await db.AppSettings.FindAsync("maxConcurrent"))?.Value;
            var delay = (await db.AppSettings.FindAsync("delayBetweenMs"))?.Value;

            int? maxConcurrent = conc != null && int.TryParse(conc, out var c) ? c : null;
            int? delayBetweenMs = delay != null && int.TryParse(delay, out var d) ? d : null;

            return (maxConcurrent, delayBetweenMs);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"[AppSettingsReader] Failed to read queue settings: {ex.Message}");
            return (null, null);
        }
    }
}
