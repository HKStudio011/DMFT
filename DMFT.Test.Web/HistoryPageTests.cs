using DMFT.Core.Entities;
using Microsoft.Playwright;

namespace DMFT.Test.Web;

[Collection("WebApp")]
public class HistoryPageTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public HistoryPageTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new()
        {
            Headless = true,
            Args = new[] { "--no-sandbox" }
        });
        _page = await _browser.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_page is not null)
            await _page.CloseAsync();
        if (_browser is not null)
            await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task HistoryPage_Loads_ShowsPageTitle()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var h1 = _page.GetByRole(AriaRole.Heading, new() { Name = "Download History" });
        await Assertions.Expect(h1).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HistoryPage_NoHistory_ShowsEmptyState()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var emptyText = _page.GetByText("No download history yet");
        await Assertions.Expect(emptyText).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HistoryPage_NavigatesFromNavMenu()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Link, new() { Name = "History" }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var h1 = _page.GetByRole(AriaRole.Heading, new() { Name = "Download History" });
        await Assertions.Expect(h1).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HistoryPage_HasTableHeaders()
    {
        await _fixture.SeedDownloadItemAsync(new DownloadItem
        {
            Url = "https://www.youtube.com/watch?v=dQw4w9WgXcQ",
            Platform = "YouTube",
            VideoId = "dQw4w9WgXcQ",
            Status = 4
        });

        await _page.GotoAsync($"{_fixture.BaseUrl}/history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var platformHeader = _page.Locator("th", new() { HasText = "Platform" });
        var videoIdHeader = _page.Locator("th", new() { HasText = "Video ID" });
        var statusHeader = _page.Locator("th", new() { HasText = "Status" });

        await Assertions.Expect(platformHeader).ToBeVisibleAsync();
        await Assertions.Expect(videoIdHeader).ToBeVisibleAsync();
        await Assertions.Expect(statusHeader).ToBeVisibleAsync();
    }
}
