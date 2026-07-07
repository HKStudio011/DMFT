using Microsoft.Playwright;

namespace DMFT.Test.Web;

[Collection("WebApp")]
public class SettingsPageTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public SettingsPageTests(WebAppFixture fixture)
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
    public async Task SettingsPage_Loads_ShowsTitle()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var h1 = _page.GetByRole(AriaRole.Heading, new() { Name = "Settings" });
        await Assertions.Expect(h1).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_NavigatesFromNavMenu()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Link, new() { Name = "Settings" }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var h1 = _page.GetByRole(AriaRole.Heading, new() { Name = "Settings" });
        await Assertions.Expect(h1).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_ShowsThemeSection()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var section = _page.GetByRole(AriaRole.Heading, new() { Name = "Theme" });
        await Assertions.Expect(section).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_ShowsYtDlpConfigSection()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var section = _page.GetByRole(AriaRole.Heading, new() { Name = "yt-dlp Configuration" });
        await Assertions.Expect(section).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_ShowsQualitySection()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var section = _page.GetByRole(AriaRole.Heading, new() { Name = "Download Quality" });
        await Assertions.Expect(section).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_ShowsUpdatesSection()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var section = _page.GetByRole(AriaRole.Heading, new() { Name = "Updates" });
        await Assertions.Expect(section).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_HasSaveButton()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var saveBtn = _page.GetByRole(AriaRole.Button, new() { Name = "Save Settings" });
        await Assertions.Expect(saveBtn).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_HasResetButton()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var resetBtn = _page.GetByRole(AriaRole.Button, new() { Name = "Reset" });
        await Assertions.Expect(resetBtn).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_ThemeModeSelect_Exists()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var select = _page.Locator("select").First;
        await Assertions.Expect(select).ToBeVisibleAsync();
    }

    [Fact]
    public async Task SettingsPage_CheckForUpdatesButton_Exists()
    {
        await _page.GotoAsync($"{_fixture.BaseUrl}/settings");
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var checkBtn = _page.GetByRole(AriaRole.Button, new() { Name = "Check for Updates" });
        await Assertions.Expect(checkBtn).ToBeVisibleAsync();
    }
}
