
using OpenQA.Selenium;
using OpenQA.Selenium.Chrome;
using OpenQA.Selenium.Edge;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Safari;
using System.Runtime.InteropServices;

namespace DMFT.Services;

public class SeleniumServices
{
    public SeleniumServices()
    {

    }
    public IWebDriver Driver { get; private set; } = null;
    public void GetWebDriver(bool isbackgorund = false)
    {
        IWebDriver driver = null;
        // macOS: prefer SafariDriver when available
        if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
        {
            driver = CreateSafariDriver() ?? CreateChromeDriver(isbackgorund) ?? CreateEdgeDriver(isbackgorund) ?? CreateFirefoxDriver(isbackgorund);
        }
        else
        {
            // Linux/Other: try Chrome, Firefox, Edge
            driver = CreateChromeDriver(isbackgorund) ?? CreateEdgeDriver(isbackgorund) ?? CreateFirefoxDriver(isbackgorund);
        }
        if (driver == null) throw new Exception("Error: Selenium Driver not found for this platform.");
        Driver = driver;
    }
    ~SeleniumServices()
    {
        Dispose();
    }

    public void Dispose()
    {
        if (Driver != null)
        {
            Driver.Quit();
            Driver.Dispose();
            Driver = null;
        }
    }

    // Platform-specific driver factories
    private IWebDriver CreateChromeDriver(bool headless)
    {
        try
        {
            var options = new ChromeOptions();
            if (headless)
            {
                options.AddArgument("--headless");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
            }
            var service = ChromeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            return new ChromeDriver(service, options);
        }
        catch { return null; }
    }

    private IWebDriver CreateEdgeDriver(bool headless)
    {
        try
        {
            var options = new EdgeOptions();
            if (headless)
            {
                options.AddArgument("--headless");
                options.AddArgument("--disable-gpu");
                options.AddArgument("--window-size=1920,1080");
            }
            var service = EdgeDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            return new EdgeDriver(service, options);
        }
        catch { return null; }
    }

    private IWebDriver CreateFirefoxDriver(bool headless)
    {
        try
        {
            var options = new FirefoxOptions();
            if (headless) options.AddArgument("--headless");
            var service = FirefoxDriverService.CreateDefaultService();
            service.HideCommandPromptWindow = true;
            return new FirefoxDriver(service, options);
        }
        catch { return null; }
    }

    private IWebDriver CreateSafariDriver()
    {
        try
        {
            // SafariDriver is built into macOS; ensure remote automation is allowed by user
            return new SafariDriver();
        }
        catch { return null; }
    }
}
