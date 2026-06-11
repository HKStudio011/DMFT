using Microsoft.Playwright;

namespace DMFT.Core.Services;

public interface ITikTokSoundExtractor
{
    Task<(string? soundName, string? soundUrl)> GetOriginalSoundAsync(string videoUrl);
}

public class TikTokSoundExtractor : ITikTokSoundExtractor
{
    public async Task<(string? soundName, string? soundUrl)> GetOriginalSoundAsync(string videoUrl)
    {
        try
        {
            var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            await using var browser = await playwright.Chromium.LaunchAsync(new()
            {
                Headless = true,
                Args = new[] { "--no-sandbox" }
            });
            var page = await browser.NewPageAsync();
            await page.GotoAsync(videoUrl, new() { Timeout = 60000 });
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var musicLink = await page.QuerySelectorAsync("a[href^='/music/']");
            if (musicLink == null) return (null, null);

            await musicLink.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle);

            var nameEl = await page.QuerySelectorAsync("h1");
            var soundName = nameEl != null ? await nameEl.TextContentAsync() : null;

            var html = await page.ContentAsync();
            var match = System.Text.RegularExpressions.Regex.Match(html,
                @"<div id=""mse""[\s\S]*?<video[^>]*src=""([^""]+)""");
            var soundUrl = match.Success ? match.Groups[1].Value : null;

            await browser.CloseAsync();
            return (soundName?.Trim(), soundUrl);
        }
        catch
        {
            return (null, null);
        }
    }
}
