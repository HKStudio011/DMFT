using System.Diagnostics;
using System.IO.Compression;
using System.Reflection;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace DMFT.Services;

public record ReleaseInfo(
    [property: JsonPropertyName("tag_name")] string TagName,
    [property: JsonPropertyName("html_url")] string HtmlUrl,
    [property: JsonPropertyName("body")] string? Body,
    [property: JsonPropertyName("assets")] List<ReleaseAsset> Assets
);

public record ReleaseAsset(
    [property: JsonPropertyName("name")] string Name,
    [property: JsonPropertyName("browser_download_url")] string BrowserDownloadUrl
);

public interface IAppUpdateService
{
    string CurrentVersion { get; }
    Task<ReleaseInfo?> CheckForUpdatesAsync();
    bool IsUpdateAvailable(ReleaseInfo release);
    Task<string?> DownloadUpdateAsync(ReleaseInfo release, IProgress<int>? progress);
    string? DownloadedZipPath { get; }
    Task<bool> InstallUpdateAsync();
}

public class AppUpdateService : IAppUpdateService
{
    private readonly HttpClient _http;
    private const string RepoOwner = "HKStudio011";
    private const string RepoName = "DMFT";

    public string CurrentVersion { get; }
    public string? DownloadedZipPath { get; private set; }

    public AppUpdateService(HttpClient http)
    {
        _http = http;

        CurrentVersion = Assembly.GetEntryAssembly()?.GetName().Version?.ToString() ?? "0.0.0";
    }

    public async Task<ReleaseInfo?> CheckForUpdatesAsync()
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                $"https://api.github.com/repos/{RepoOwner}/{RepoName}/releases/latest");
            request.Headers.UserAgent.ParseAdd("DMFT/" + CurrentVersion);

            var response = await _http.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ReleaseInfo>(json);
        }
        catch { return null; }
    }

    public bool IsUpdateAvailable(ReleaseInfo release)
    {
        var tag = release.TagName.TrimStart('v');
        return CompareSemanticVersions(tag, CurrentVersion) > 0;
    }

    private static int CompareSemanticVersions(string v1, string v2)
    {
        var parts1 = v1.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var parts2 = v2.Split('.', StringSplitOptions.RemoveEmptyEntries);
        var maxParts = Math.Max(parts1.Length, parts2.Length);

        for (var i = 0; i < maxParts; i++)
        {
            var num1 = i < parts1.Length && int.TryParse(parts1[i], out var p1) ? p1 : 0;
            var num2 = i < parts2.Length && int.TryParse(parts2[i], out var p2) ? p2 : 0;
            if (num1 != num2) return num1.CompareTo(num2);
        }
        return 0;
    }

    public async Task<string?> DownloadUpdateAsync(ReleaseInfo release, IProgress<int>? progress)
    {
        try
        {
            var asset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                !a.Name.Contains("symbols", StringComparison.OrdinalIgnoreCase));

            if (asset == null) return null;

            var tempDir = Path.Combine(Path.GetTempPath(), "DMFT_Update");
            Directory.CreateDirectory(tempDir);

            var zipPath = Path.Combine(tempDir, asset.Name);

            var request = new HttpRequestMessage(HttpMethod.Get, asset.BrowserDownloadUrl);
            request.Headers.UserAgent.ParseAdd("DMFT/" + CurrentVersion);

            using var response = await _http.SendAsync(request, HttpCompletionOption.ResponseHeadersRead);
            if (!response.IsSuccessStatusCode) return null;

            var totalBytes = response.Content.Headers.ContentLength ?? -1;
            using var contentStream = await response.Content.ReadAsStreamAsync();
            using var fileStream = new FileStream(zipPath, FileMode.Create, FileAccess.Write, FileShare.None);

            var buffer = new byte[81920];
            long readBytes = 0;
            int bytesRead;
            var lastReported = 0;

            while ((bytesRead = await contentStream.ReadAsync(buffer)) > 0)
            {
                await fileStream.WriteAsync(buffer, 0, bytesRead);
                readBytes += bytesRead;

                if (totalBytes > 0 && progress != null)
                {
                    var pct = (int)(readBytes * 100 / totalBytes);
                    if (pct > lastReported)
                    {
                        lastReported = pct;
                        progress.Report(pct);
                    }
                }
            }

            DownloadedZipPath = zipPath;
            return zipPath;
        }
        catch { return null; }
    }

    public Task<bool> InstallUpdateAsync()
    {
        if (DownloadedZipPath == null || !File.Exists(DownloadedZipPath))
            return Task.FromResult(false);

        try
        {
            var appDir = AppContext.BaseDirectory;
            var updaterPath = FindUpdaterPath(appDir);
            if (updaterPath == null) return Task.FromResult(false);

            var currentPid = Environment.ProcessId;

            var psi = new ProcessStartInfo
            {
                FileName = updaterPath,
                Arguments = $"--zip \"{DownloadedZipPath}\" --pid {currentPid} --app-dir \"{appDir}\"",
                UseShellExecute = true,
                WindowStyle = ProcessWindowStyle.Normal
            };

            Process.Start(psi);
            return Task.FromResult(true);
        }
        catch { return Task.FromResult(false); }
    }

    private static string? FindUpdaterPath(string appDir)
    {
        var candidates = new[]
        {
            Path.Combine(appDir, "DMFT.Updater.exe"),
        };

        return candidates.FirstOrDefault(File.Exists);
    }
}
