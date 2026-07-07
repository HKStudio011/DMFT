using OpenQA.Selenium.Appium;
using OpenQA.Selenium.Appium.Windows;

namespace DMFT.Test.App;

public class AppLaunchTests
{
    private const string AppiumUrl = "http://127.0.0.1:4723";
    private const string AppId = @"C:\Program Files\DMFT\DMFT.exe";

    [Fact(Skip = "Requires Appium server and deployed MAUI app")]
    public async Task App_Launches_MainWindowAppears()
    {
        var options = new AppiumOptions();
        options.App = AppId;
        options.PlatformName = "Windows";
        options.DeviceName = "WindowsPC";

        using var driver = new WindowsDriver(new Uri(AppiumUrl), options);
        await Task.Delay(3000);

        var windowHandle = driver.WindowHandles;
        Assert.NotEmpty(windowHandle);
    }

    [Fact(Skip = "Requires Appium server and deployed MAUI app")]
    public async Task App_NavigatesToSettings()
    {
        var options = new AppiumOptions();
        options.App = AppId;
        options.PlatformName = "Windows";
        options.DeviceName = "WindowsPC";

        using var driver = new WindowsDriver(new Uri(AppiumUrl), options);
        await Task.Delay(3000);

        try
        {
            var settingsLink = driver.FindElement(MobileBy.AccessibilityId("Settings"));
            settingsLink.Click();
            await Task.Delay(1000);
        }
        catch (OpenQA.Selenium.NoSuchElementException)
        {
            Assert.Fail("WebView2 elements not accessible via Appium directly. Consider testing via Playwright against the web version.");
        }
    }

    [Fact(Skip = "Requires Appium server and deployed MAUI app")]
    public void AppiumServer_IsReachable()
    {
        using var httpClient = new HttpClient();
        try
        {
            var response = httpClient.GetAsync($"{AppiumUrl}/status")
                .GetAwaiter().GetResult();
            Assert.True(response.IsSuccessStatusCode);
        }
        catch
        {
            Assert.Fail("Appium server is not running at " + AppiumUrl);
        }
    }
}
