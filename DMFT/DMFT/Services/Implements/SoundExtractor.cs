using Microsoft.Playwright;

namespace DMFT.Services;

public interface ISoundExtractor
{
    Task<(string? soundName, string? soundUrl, string? videoId)> GetOriginalSoundTiktokAsync(string videoUrl);
    Task<string?> GetOriginalSoundYTShortAsync(string videoUrl);
    Task<bool> CheckAvailableAsync();
    Task CancelAsync();
}

public class SoundExtractor : ISoundExtractor
{
    private readonly IVideoLinkParser _parser;
    private bool? _available;
    private IPlaywright? _currentPlaywright;
    private Microsoft.Playwright.IBrowser? _currentBrowser;

    public SoundExtractor(IVideoLinkParser parser)
    {
        _parser = parser;
    }

    public async Task<bool> CheckAvailableAsync()
    {
        if (_available.HasValue) return _available.Value;
        try
        {
            using var playwright = await Microsoft.Playwright.Playwright.CreateAsync();
            var browser = await TryLaunchAsync(playwright, headless: true);
            if (browser != null) await browser.CloseAsync();
            _available = browser != null;
        }
        catch
        {
            _available = false;
        }
        return _available.Value;
    }

    public async Task<(string? soundName, string? soundUrl, string? videoId)> GetOriginalSoundTiktokAsync(string videoUrl)
    {
        _parser.TryParseVideoId(videoUrl, out var videoId);
        _currentPlaywright = await Microsoft.Playwright.Playwright.CreateAsync();
        _currentBrowser = await TryLaunchAsync(_currentPlaywright, headless: false);
        if (_currentBrowser == null) return (null, null, videoId);

        var page = await _currentBrowser.NewPageAsync();
        await page.GotoAsync(videoUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);
        await page.WaitForSelectorAsync("a[href^='/music/']", new() { Timeout = 300_000 });
        var musicLink = await page.QuerySelectorAsync("a[href^='/music/']");
        if (musicLink == null) return (null, null, videoId);

        await musicLink.ClickAsync();
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);
        await page.WaitForSelectorAsync("div#mse", new() { State = WaitForSelectorState.Attached, Timeout = 30000 });

        var nameEl = await page.QuerySelectorAsync("h1");
        var soundName = nameEl != null ? await nameEl.TextContentAsync() : null;

        var html = await page.ContentAsync();
        var match = System.Text.RegularExpressions.Regex.Match(html,
            @"<div id=""mse""[\s\S]*?<video[^>]*src=""([^""]+)""");
        var soundUrl = match.Success ? match.Groups[1].Value : null;

        return (soundName?.Trim(), soundUrl, videoId);
    }

    public async Task<string?> GetOriginalSoundYTShortAsync(string videoUrl)
    {
        _currentPlaywright = await Microsoft.Playwright.Playwright.CreateAsync();
        _currentBrowser = await TryLaunchAsync(_currentPlaywright, headless: false);
        if (_currentBrowser == null) return null;

        var page = await _currentBrowser.NewPageAsync();
        await page.GotoAsync(videoUrl);
        await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);

        var soundBtn =  page.Locator("#experiment-overlay > ytd-reel-player-overlay-renderer > yt-reel-player-overlay-view-model > div.ytReelPlayerOverlayViewModelActionsContainer > reel-action-bar-view-model > pivot-button-view-model > a");
        await soundBtn.WaitForAsync(new() { State = WaitForSelectorState.Attached, Timeout = 300_000 });
        if (await soundBtn.CountAsync() == 0) return null;
        await soundBtn.ClickAsync();

        await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);

        var panel = page.Locator("#anchored-panel");
        await panel.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        if (await panel.CountAsync() == 0) return null;
        
        var children = panel.Locator("[target-id=\"engagement-panel-shorts-audio-pivot\"]");
        await children.WaitForAsync(new() { State = WaitForSelectorState.Visible, Timeout = 60_000 });
        if (await children.CountAsync() == 0) return null;

        var firstContents =  children.Locator("#contents");
        if (await firstContents.CountAsync() == 0) return null;

        await Task.Delay(TimeSpan.FromSeconds(3));

        var items = await firstContents.Locator(":scope > *").AllAsync();

        if (items.Count <= 1 && items.Count > 0)
        {
            await items[0].ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);
            return page.Url;
        }

        if (items.Count >= 2)
        {
            var header = children.Locator("#header > yt-page-header-view-model > div > div.ytPageHeaderViewModelHeadline > yt-content-preview-image-view-model");
            if (await header.CountAsync() == 0) return null;
            await header.ClickAsync();
            await page.WaitForLoadStateAsync(LoadState.NetworkIdle | LoadState.DOMContentLoaded);
            return page.Url;
        }

        return null;
    }

    private static async Task<Microsoft.Playwright.IBrowser?> TryLaunchAsync(IPlaywright pw, bool headless = true)
    {
        try { return await pw.Chromium.LaunchAsync(new() { Headless = headless, Channel = "chrome" }); } catch { }
        try { return await pw.Chromium.LaunchAsync(new() { Headless = headless, Channel = "msedge" }); } catch { }
        try { return await pw.Firefox.LaunchAsync(new() { Headless = headless }); } catch { }
        try { return await pw.Chromium.LaunchAsync(new() { Headless = headless, Args = new[] { "--no-sandbox" } }); } catch { }
        return null;
    }
}
