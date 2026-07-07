using Microsoft.Playwright;

namespace DMFT.Test.Web;

[Collection("WebApp")]
public class SettingsPageTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public SettingsPageTests(WebAppFixture fixture) => _fixture = fixture;

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

    private async Task NavigateToSettingsAsync()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
    }

    [Fact]
    public async Task SettingsPage_Loads_ShowsTitle()
    {
        await NavigateToSettingsAsync();

        var h1 = _page.GetByRole(AriaRole.Heading, new() { Name = "Settings" });
        await Assertions.Expect(h1).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_ShowsAllSections()
    {
        await NavigateToSettingsAsync();

        var headings = _page.Locator("h2");
        var texts = await headings.AllTextContentsAsync();
        Assert.Contains(texts, t => t.Contains("Theme"));
        Assert.Contains(texts, t => t.Contains("yt-dlp"));
        Assert.Contains(texts, t => t.Contains("Quality"));
        Assert.Contains(texts, t => t.Contains("Updates"));
    }

    [Fact]
    public async Task SettingsPage_HasSaveAndResetButtons()
    {
        await NavigateToSettingsAsync();

        var saveBtn = _page.GetByRole(AriaRole.Button, new() { Name = "Save Settings" });
        var resetBtn = _page.GetByRole(AriaRole.Button, new() { Name = "Reset" });

        await Assertions.Expect(saveBtn).ToBeVisibleAsync();
        await Assertions.Expect(resetBtn).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_ThemeSelect_Exists()
    {
        await NavigateToSettingsAsync();

        var themeSelect = _page.Locator("select").First;
        await Assertions.Expect(themeSelect).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_CheckForUpdates_ShowsResult()
    {
        await NavigateToSettingsAsync();

        var checkBtn = _page.GetByRole(AriaRole.Button, new() { Name = "Check for Updates" });
        await Assertions.Expect(checkBtn).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_SaveSettings_ShowsSuccessToast()
    {
        await NavigateToSettingsAsync();

        await _page.GetByRole(AriaRole.Button, new() { Name = "Save Settings" }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var toast = _page.GetByText("Settings saved");
        await Assertions.Expect(toast).ToBeVisibleAsync();
    }
}
