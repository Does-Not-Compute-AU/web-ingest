using System;
using System.IO;
using System.Threading;
using Castle.Core.Internal;
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
        string html;
        using var driver = InitFirefoxWebDriver();
        try
        {
            driver.Navigate().GoToUrl(url);

            var wait = new WebDriverWait(driver, TimeSpan.FromSeconds(15));
            wait.Until(x => x.FindElement(By.TagName("html")).Displayed);

            // TODO Driver wait-for-page needs serious improvement
            Thread.Sleep(TimeSpan.FromMilliseconds(3000));

            html = driver.FindElement(By.TagName("html")).GetAttribute("innerHTML");
        }
        catch(Exception e)
        {
            driver.Quit();
            throw;
        }

        return html;
    }

    private IWebDriver InitFirefoxWebDriver(bool headless = true)
    {
        var buildOutputDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var binariesFolder = Path.Combine(buildOutputDirectory.FullName, "Binaries");
        FirefoxDriverService service = FirefoxDriverService.CreateDefaultService(binariesFolder, "geckodriver.exe");

        var fxProfile = new FirefoxProfile();
        if (_httpConfiguration.RandomUserAgents)
            fxProfile.SetPreference("general.useragent.override", WebIngestClientHelpers.GetRandomUserAgent());
        else if (!_httpConfiguration.SpecifiedUserAgent.IsNullOrEmpty())
            fxProfile.SetPreference("general.useragent.override", _httpConfiguration.SpecifiedUserAgent);

        var fxOptions = new FirefoxOptions()
        {
            Profile = fxProfile,
            AcceptInsecureCertificates = true,
        };
        if (headless)
            fxOptions.AddArgument("--headless");

        // TODO: better support for proxy types
        if (_httpConfiguration.ProxyRequests)
        {
            if (_httpConfiguration.ProxyAddress.Contains("http://"))
            {
                var proxyString = _httpConfiguration.ProxyAddress.Replace("http://", "");
                if (!_httpConfiguration.ProxyUsername.IsNullOrEmpty() &&
                    !_httpConfiguration.ProxyPassword.IsNullOrEmpty())
                    proxyString = "{_httpConfiguration.ProxyUsername}:{_httpConfiguration.ProxyPassword}" + proxyString;

                fxOptions.Proxy = new Proxy()
                {
                    HttpProxy = proxyString,
                    SslProxy = proxyString
                };
            }
        }


        var driver = new FirefoxDriver(service, fxOptions);
        driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(10);

        return driver;
    }
}