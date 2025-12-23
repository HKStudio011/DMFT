using OpenQA.Selenium;
using OpenQA.Selenium.Support.UI;
using System.Text.RegularExpressions;

namespace DMFT.Services
{
    public class TikTokSoundExtractor
    {
        public TikTokSoundExtractor(SeleniumServices seleniumServices)
        {
            SeleniumServices = seleniumServices;
        }
        private static readonly HttpClient _http = new HttpClient();

        public SeleniumServices SeleniumServices { get; }

        public async Task<string?> GetOriginalSoundUrlAsync(string videoUrl)
        {
            try
            {
                int seconds = 60;
                var driver = SeleniumServices.Driver;
                if (driver == null)
                {
                    SeleniumServices.GetWebDriver(isbackgorund: false);
                    driver = SeleniumServices.Driver;
                }
                await driver.Navigate().GoToUrlAsync(videoUrl);
                WebDriverWait wait = new WebDriverWait(driver, TimeSpan.FromSeconds(seconds));
                wait.Until(drv =>
                {
                    return ((IJavaScriptExecutor)drv).ExecuteScript("return document.readyState").Equals("complete"); 
                });


                wait = new WebDriverWait(driver, TimeSpan.FromSeconds(seconds));

                IWebElement element = wait.Until(drv =>
                {
                    var itemsA = drv.FindElements(By.XPath("//*[@id=\"one-column-item-0\"]/div/section[2]/a"));
                    if (itemsA.Count > 0 && itemsA[0].Displayed)
                        return itemsA[0];

                    var itemsB = drv.FindElements(By.XPath("//*[@id=\"media-card-0\"]/div/div[2]/div[2]/div[1]/div[5]/a"));
                    if (itemsB.Count > 0 && itemsB[0].Displayed)
                        return itemsB[0];

                    return null; // tiếp tục chờ nếu chưa thấy gì
                });

                string? href = null;
                if (element != null)
                {
                    href = element.GetAttribute("href");
                }

                if (href != null)
                {
                    await driver.Navigate().GoToUrlAsync(href);

                    wait = new WebDriverWait(driver, TimeSpan.FromSeconds(seconds));
                    wait.Until(drv =>
                    {
                        return ((IJavaScriptExecutor)drv).ExecuteScript("return document.readyState").Equals("complete");
                    });

                    wait = new WebDriverWait(driver, TimeSpan.FromSeconds(seconds));

                    element = wait.Until(drv =>
                    {
                        var itemsA = drv.FindElements(By.XPath("//*[@id=\"main-content-single_song\"]/div/div[1]/div[1]/div[2]/h1"));
                        if (itemsA.Count > 0 && itemsA[0].Displayed)
                            return itemsA[0];
                        ;
                        return null; // tiếp tục chờ nếu chưa thấy gì
                    });
                    string? soundName = null;
                    if (element != null)
                    {
                        soundName = element.Text;
                    }

                    string html = driver.PageSource;
                    string? soundLink = null;
                    Match match = Regex.Match(html, @"<div id=""mse""[\s\S]*?<video[^>]*src=""([^""]+)""");
                    if (match.Success)
                    {
                         soundLink = match.Groups[1].Value;
                    }

                    if (soundLink != null && soundName != null)
                    {
                        return string.Concat(soundName, "\n", soundLink);
                    }
                }
            }
            catch {

            }
            finally
            {
                
                SeleniumServices.Dispose();
            }
            return null;
        }
    }
}
