using System.Text.Json;
using System.Text.Json.Serialization;

namespace DMFT.Core.Services;

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
    Task<ReleaseInfo?> CheckForUpdatesAsync(string currentVersion);
    Task<string?> DownloadReleaseAsync(ReleaseInfo release, string destDir);
    bool IsUpdateAvailable(ReleaseInfo release, string currentVersion);
}

public class AppUpdateService : IAppUpdateService
{
    private readonly HttpClient _http;

    public AppUpdateService(HttpClient http)
    {
        _http = http;
    }

    public async Task<ReleaseInfo?> CheckForUpdatesAsync(string currentVersion)
    {
        try
        {
            var request = new HttpRequestMessage(HttpMethod.Get,
                "https://api.github.com/repos/owner/dmft/releases/latest");
            request.Headers.UserAgent.ParseAdd("DMFT/2.0");

            var response = await _http.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode) return null;

            var json = await response.Content.ReadAsStringAsync();
            return JsonSerializer.Deserialize<ReleaseInfo>(json);
        }
        catch { return null; }
    }

    public bool IsUpdateAvailable(ReleaseInfo release, string currentVersion)
    {
        var tag = release.TagName.TrimStart('v');
        return string.Compare(tag, currentVersion,
            StringComparison.OrdinalIgnoreCase) > 0;
    }

    public async Task<string?> DownloadReleaseAsync(ReleaseInfo release, string destDir)
    {
        try
        {
            var asset = release.Assets.FirstOrDefault(a =>
                a.Name.EndsWith(".zip", StringComparison.OrdinalIgnoreCase) &&
                a.Name.Contains("win", StringComparison.OrdinalIgnoreCase));

            if (asset == null) return null;

            Directory.CreateDirectory(destDir);

            var request = new HttpRequestMessage(HttpMethod.Get, asset.BrowserDownloadUrl);
            request.Headers.UserAgent.ParseAdd("DMFT/2.0");

            var response = await _http.SendAsync(request,
                HttpCompletionOption.ResponseHeadersRead);

            if (!response.IsSuccessStatusCode) return null;

            var zipPath = Path.Combine(destDir, asset.Name);
            using var fs = new FileStream(zipPath, FileMode.Create,
                FileAccess.Write, FileShare.None);
            await response.Content.CopyToAsync(fs);

            return zipPath;
        }
        catch { return null; }
    }
}
