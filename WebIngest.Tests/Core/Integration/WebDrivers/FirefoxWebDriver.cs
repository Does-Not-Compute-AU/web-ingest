using System;
using NUnit.Framework;
using WebIngest.Common.Models.OriginConfiguration.Types;
using WebIngest.Core.Scraping.WebClients;

namespace WebIngest.Tests.Core.Integration.WebDrivers;

public class FirefoxWebDriver
{
    // TODO Find a way to run this on github actions
    [Test]
    public void TestWebDriverStringDownload()
    {
        return;
        var driver = new SeleniumWebClient(new HttpConfiguration()
        {
            SeleniumConfiguration = new SeleniumConfiguration()
            {
                UseSeleniumDriver = true,
            },
            RandomUserAgents = false,
            ProxyRequests = false
        });
        var googleHtml = driver.DownloadString("https://google.com");
        Assert.AreNotEqual(String.IsNullOrEmpty(googleHtml), true);
    }
}