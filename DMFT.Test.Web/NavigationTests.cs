using Microsoft.Playwright;

namespace DMFT.Test.Web;

[Collection("WebApp")]
public class NavigationTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public NavigationTests(WebAppFixture fixture) => _fixture = fixture;

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
    public async Task Navigation_NavMenuShowsThreeLinks()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var navLinks = _page.Locator("nav a");
        var count = await navLinks.CountAsync();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Navigation_ClickHistory_ShowsHistoryPage()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Link, new() { Name = "History" }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var heading = _page.GetByRole(AriaRole.Heading, new() { Name = "Download History" });
        await Assertions.Expect(heading).ToBeVisibleAsync();
        Assert.Contains("/history", _page.Url);
    }

    [Fact]
    public async Task Navigation_ClickSettings_ShowsSettingsPage()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var heading = _page.GetByRole(AriaRole.Heading, new() { Name = "Settings" });
        await Assertions.Expect(heading).ToBeVisibleAsync();
        Assert.Contains("/settings", _page.Url);
    }
}
