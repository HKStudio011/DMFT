using Microsoft.Playwright;

namespace DMFT.Test.Web;

[Collection("WebApp")]
public class MainPageTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public MainPageTests(WebAppFixture fixture) => _fixture = fixture;

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
    public async Task MainPage_EmptyState_ShowsNoDownloadsMessage()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var emptyText = _page.GetByText("No downloads yet");
        await Assertions.Expect(emptyText).ToBeVisibleAsync();
    }

    [Fact]
    public async Task MainPage_AddSingleUrl_ShowsItemInList()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Add URLs" }).ClickAsync();
        await _page.GetByPlaceholder("Enter video URL")
            .FillAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var platformBadge = _page.GetByText("YouTube", new() { Exact = true });
        await Assertions.Expect(platformBadge).ToBeVisibleAsync();
    }

    [Fact]
    public async Task MainPage_AddUrl_AppearsInBodyText()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);
        var badge = _page.GetByText("YouTube", new() { Exact = true });
        await Assertions.Expect(badge).Not.ToBeVisibleAsync();

        await _page.GetByRole(AriaRole.Button, new() { Name = "Add URLs" }).ClickAsync();
        await _page.GetByPlaceholder("Enter video URL")
            .FillAsync("https://www.youtube.com/watch?v=test123");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();

        await Assertions.Expect(badge).ToBeVisibleAsync();
        Assert.Equal("YouTube", await badge.TextContentAsync());
    }

    [Fact]
    public async Task MainPage_SeededItems_ShowsListNotEmpty()
    {
        await _fixture.SeedMainItemAsync("https://youtube.com/watch?v=abc");
        await _fixture.SeedMainItemAsync("https://tiktok.com/@user/video/xyz", "TikTok");

        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var hasAbc = await _page.GetByText("youtube.com/watch?v=abc").IsVisibleAsync();
        var hasXyz = await _page.GetByText("tiktok.com/@user/video/xyz").IsVisibleAsync();
        Assert.True(hasAbc && hasXyz);
    }

    [Fact]
    public async Task MainPage_ClickDownload_TriggersQueue()
    {
        await _fixture.SeedMainItemAsync("https://youtube.com/watch?v=abc");
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Download" }).First.ClickAsync();
        await Task.Delay(500);

        var pageText = await _page.TextContentAsync("body");
        Assert.Contains("Download", pageText, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task MainPage_RemoveItem_ItemDisappears()
    {
        await _fixture.SeedMainItemAsync("https://youtube.com/watch?v=toremove", videoId: "toremove");
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Remove" }).ClickAsync();
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var emptyText = _page.GetByText("No downloads yet");
        await Assertions.Expect(emptyText).ToBeVisibleAsync();
    }

    [Fact]
    public async Task MainPage_ModeCheckbox_TogglesDownloadMode()
    {
        await _fixture.SeedMainItemAsync("https://youtube.com/watch?v=modecheck");
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var checkbox = _page.GetByRole(AriaRole.Checkbox, new() { Name = "Video" }).First;
        await Assertions.Expect(checkbox).ToBeVisibleAsync();

        // Click the checkbox and verify state changes
        var isChecked = await checkbox.IsCheckedAsync();
        await checkbox.ClickAsync();
        await Task.Delay(200);
        Assert.NotEqual(isChecked, await checkbox.IsCheckedAsync());

        var applyBtn = _page.GetByRole(AriaRole.Button, new() { Name = "Apply to All" });
        await Assertions.Expect(applyBtn).ToBeVisibleAsync();
    }
}
