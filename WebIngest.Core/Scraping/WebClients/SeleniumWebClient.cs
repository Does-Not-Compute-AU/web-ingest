using System;
using System.IO;
using System.Threading;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Support.UI;
using WebIngest.Common.Models.OriginConfiguration.Types;

namespace WebIngest.Core.Scraping.WebClients;

public class SeleniumWebClient : IWebIngestWebClient
{
    private readonly HttpConfiguration _httpConfiguration;

    public SeleniumWebClient(HttpConfiguration configuration)
    {
        _httpConfiguration = configuration;
    }
    

    public string DownloadString(string url)
    {
        var driver = InitFirefoxWebDriver();
        driver.Navigate().GoToUrl(url);
        
        Thread.Sleep(TimeSpan.FromMilliseconds(3000));
        
        var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
        wait.Until(x => x.FindElement(By.TagName("html")).Displayed);
        
        
        new WebDriverWait(driver, TimeSpan.FromMilliseconds(500))
            .Until(
            d => ((IJavaScriptExecutor) d).ExecuteScript("return document.readyState").Equals("complete"));

        new WebDriverWait(driver, TimeSpan.FromSeconds(5));
        var html = driver.FindElement(By.TagName("html")).GetAttribute("innerHTML");
        driver.Dispose();
        return html;
    }

    private static IWebDriver InitFirefoxWebDriver(bool headless = true)
    {
        var buildOutputDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var binariesFolder = Path.Combine(buildOutputDirectory.FullName, "Binaries");
        FirefoxDriverService service = FirefoxDriverService.CreateDefaultService(binariesFolder, "geckodriver.exe");

        var fxProfile = new FirefoxProfile();
        fxProfile.SetPreference("general.useragent.override", WebIngestClientHelpers.GetRandomUserAgent());
        var fxOptions = new FirefoxOptions()
        {
            Profile = fxProfile,
            AcceptInsecureCertificates = true,
        };
        if (headless)
            fxOptions.AddArgument("--headless");
        //fxOptions.AddArgument("--headless");
        var driver = new FirefoxDriver(service, fxOptions);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

        //options.AddArguments("--proxy-server=http://user:password@yourProxyServer.com:8080");

        return driver;
    }
}