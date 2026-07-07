using Microsoft.Playwright;

namespace DMFT.Test.Web;

[Collection("WebApp")]
public class NavigationTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public NavigationTests(WebAppFixture fixture)
    {
        _fixture = fixture;
    }

    public async ValueTask InitializeAsync()
    {
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
    public async Task Navigation_NavMenuShowsThreeLinks()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var navLinks = _page.Locator("nav a");
        var count = await navLinks.CountAsync();
        Assert.Equal(3, count);
    }

    [Fact]
    public async Task Navigation_MainLink_NavigatesToHome()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/history");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Link, new() { Name = "Main" }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var heading = _page.GetByRole(AriaRole.Heading, new() { Name = "Downloads" });
        await Assertions.Expect(heading).ToBeVisibleAsync();
    }

    [Fact]
    public async Task Navigation_NotFoundPage_ShowsNotFound()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/nonexistent-page");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var body = _page.Locator("body");
        var text = await body.InnerTextAsync();
        Assert.Contains("Not Found", text, StringComparison.OrdinalIgnoreCase);
    }
}
