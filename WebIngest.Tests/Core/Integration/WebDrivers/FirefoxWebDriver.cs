using System;
using NUnit.Framework;
using WebIngest.Common.Models.OriginConfiguration.Types;
using WebIngest.Core.Scraping.WebClients;

namespace WebIngest.Tests.Core.Integration.WebDrivers;

public class FirefoxWebDriver
{
    [Test]
    public void TestWebDriverStringDownload()
    {
        var driver = new SeleniumWebClient(new HttpConfiguration()
        {
            UseSeleniumDriver = true,
            RandomUserAgents = false,
            ProxyRequests = false
        });
        var googleHtml = driver.DownloadString("https://google.com");
        Assert.AreNotEqual(String.IsNullOrEmpty(googleHtml), true);
    }
    
}