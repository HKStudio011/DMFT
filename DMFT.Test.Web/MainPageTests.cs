using Microsoft.Playwright;

namespace DMFT.Test.Web;

[Collection("WebApp")]
public class MainPageTests : IAsyncLifetime
{
    private readonly WebAppFixture _fixture;
    private IPlaywright _playwright = null!;
    private IBrowser _browser = null!;
    private IPage _page = null!;

    public MainPageTests(WebAppFixture fixture)
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
        await _page.CloseAsync();
        await _browser.CloseAsync();
        _playwright.Dispose();
    }

    [Fact]
    public async Task MainPage_Loads_ShowsPageTitle()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var title = await _page.TitleAsync();
        Assert.Contains("DMFT", title);
    }

    [Fact]
    public async Task MainPage_NoDownloads_ShowsEmptyState()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var emptyText = _page.GetByText("No downloads yet");
        await Assertions.Expect(emptyText).ToBeVisibleAsync();
    }

    [Fact]
    public async Task MainPage_AddButton_IsVisible()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        var addBtn = _page.GetByRole(AriaRole.Button, new() { Name = "Add URLs" });
        await Assertions.Expect(addBtn).ToBeVisibleAsync();
    }

    [Fact]
    public async Task MainPage_ClickAddButton_OpensModal()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Add URLs" }).ClickAsync();
        var textarea = _page.GetByPlaceholder("Enter video URL");
        await Assertions.Expect(textarea).ToBeVisibleAsync();
    }

    [Fact]
    public async Task MainPage_ShowsModeCheckboxes()
    {
        await _page.GotoAsync(_fixture.BaseUrl);
        await _page.WaitForLoadStateAsync(LoadState.NetworkIdle);

        await _page.GetByRole(AriaRole.Button, new() { Name = "Add URLs" }).ClickAsync();
        await _page.GetByPlaceholder("Enter video URL").FillAsync("https://www.youtube.com/watch?v=dQw4w9WgXcQ");
        await _page.GetByRole(AriaRole.Button, new() { Name = "Add", Exact = true }).ClickAsync();
        await Task.Delay(500);

        var videoCheckbox = _page.GetByRole(AriaRole.Checkbox, new() { Name = "Video" }).First;
        await Assertions.Expect(videoCheckbox).ToBeVisibleAsync();
    }
}
