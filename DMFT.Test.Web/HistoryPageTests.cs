using Microsoft.Playwright;

namespace DMFT.Test.Web;

[Collection("WebApp")]
public class HistoryPageTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public HistoryPageTests(WebAppFixture fixture) => _fixture = fixture;

    public async ValueTask InitializeAsync()
    {
        await _fixture.ResetDatabaseAsync();
        _playwright = await Microsoft.Playwright.Playwright.CreateAsync();
        _browser = await _playwright.Chromium.LaunchAsync(new() { Headless = true, Args = new[] { "--no-sandbox" } });
        _page = await _browser.NewPageAsync();
    }

    public async ValueTask DisposeAsync()
    {
        if (_page is not null) await _page.CloseAsync();
        if (_browser is not null) await _browser.CloseAsync();
        _playwright?.Dispose();
    }

    [Fact]
    public async Task HistoryPage_NoHistory_ShowsEmptyMessage()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var emptyText = _page.GetByText("No download history yet");
        await Assertions.Expect(emptyText).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HistoryPage_SeededItems_ShowsTable()
    {
        await _fixture.SeedHistoryItemAsync("https://youtube.com/watch?v=done1", "YouTube");
        await _fixture.SeedHistoryItemAsync("https://tiktok.com/@user/video/old", "TikTok");

        await _page.GotoAsync($"{_fixture.BaseUrl}/history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rows = _page.Locator("table tbody tr");
        var count = await rows.CountAsync();
        Assert.Equal(2, count);
    }

    [Fact]
    public async Task HistoryPage_Table_HasColumnHeaders()
    {
        await _fixture.SeedHistoryItemAsync("https://youtube.com/watch?v=hdr");

        await _page.GotoAsync($"{_fixture.BaseUrl}/history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var headers = _page.GetByRole(AriaRole.Columnheader);
        await Assertions.Expect(headers.First).ToBeVisibleAsync();
        var headerTexts = await headers.AllTextContentsAsync();
        Assert.Contains(headerTexts, h => h.Contains("Platform"));
    }

    [Fact]
    public async Task HistoryPage_DeleteItem_RemovesFromList()
    {
        await _fixture.SeedHistoryItemAsync("https://youtube.com/watch?v=delete-me");

        await _page.GotoAsync($"{_fixture.BaseUrl}/history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var rowCount = await _page.Locator("tbody tr").CountAsync();
        Assert.Equal(1, rowCount);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Delete" }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var emptyText = _page.GetByText("No download history yet");
        await Assertions.Expect(emptyText).ToBeVisibleAsync();
    }

    [Fact]
    public async Task HistoryPage_RetryItem_ItemAppearsInMain()
    {
        await _fixture.SeedHistoryItemAsync("https://youtube.com/watch?v=retry-me");

        await _page.GotoAsync($"{_fixture.BaseUrl}/history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Retry" }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var hasItem = await _page.GetByText("youtube.com/watch?v=retry-me").IsVisibleAsync();
        Assert.True(hasItem);
    }
}
