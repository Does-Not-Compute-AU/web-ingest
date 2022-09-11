using System;
using System.Drawing;
using System.IO;
using System.Threading;
using Castle.Core.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OpenQA.Selenium;
using OpenQA.Selenium.Firefox;
using OpenQA.Selenium.Remote;
using OpenQA.Selenium.Support.UI;
using WebIngest.Common;
using WebIngest.Common.Models.OriginConfiguration.Types;
using WebIngest.Core.Jobs;

namespace WebIngest.Core.Scraping.WebClients;

public class SeleniumWebClient : IWebIngestWebClient
{
    private readonly SeleniumConfiguration _seleniumConfig;
    private readonly HttpConfiguration _httpConfiguration;

    public SeleniumWebClient(HttpConfiguration configuration)
    {
        _httpConfiguration = configuration;
        _seleniumConfig = configuration.SeleniumConfiguration;
    }

    public string DownloadString(string url)
    {
        string html;
        using var driver = InitFirefoxWebDriver(_seleniumConfig.Headless);
        try
        {
            var timeout = TimeSpan.FromSeconds(_seleniumConfig.TimeoutSeconds);
            driver.Navigate().GoToUrl(url);

            // manual thread-sleep-seconds-wait if configured
            if (_seleniumConfig.ThreadSleepSeconds > 0)
                Thread.Sleep(TimeSpan.FromSeconds(_seleniumConfig.ThreadSleepSeconds));

            // wait-for-element by css selector, if configured
            if (!_seleniumConfig.ElementWaitSelector.IsNullOrEmpty())
                new WebDriverWait(driver, timeout)
                    .Until(d => d.FindElement(By.CssSelector(_seleniumConfig.ElementWaitSelector)));

            // wait until page contains some text
            if (!_seleniumConfig.WaitForPageContains.IsNullOrEmpty())
                new WebDriverWait(driver, timeout)
                    .Until(d => d.PageSource.Contains(_seleniumConfig.WaitForPageContains));

            // wait until page doesn't contain some text
            if (!_seleniumConfig.WaitForPageNotContains.IsNullOrEmpty())
                new WebDriverWait(driver, timeout)
                    .Until(d => !d.PageSource.Contains(_seleniumConfig.WaitForPageNotContains));

            // always wait until page source has something in it
            new WebDriverWait(driver, timeout)
                .Until(d => !d.PageSource.IsNullOrEmpty());

            html = driver.PageSource;
            driver.Quit();
        }
        catch (Exception e)
        {
            driver.Quit();
            throw;
        }

        return html;
    }

    private FirefoxDriverService GetLocalFirefoxBinaryService()
    {
        var buildOutputDirectory = Directory.GetParent(System.Reflection.Assembly.GetExecutingAssembly().Location);
        var binariesFolder = Path.Combine(buildOutputDirectory.FullName, "Binaries");
        return FirefoxDriverService.CreateDefaultService(binariesFolder, "geckodriver.exe");
    }

    public IWebDriver InitFirefoxWebDriver(bool headless = true)
    {
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

        IWebDriver driver = null;
        try
        {
            if (_seleniumConfig.UseBinaryDriver)
                driver = new FirefoxDriver(GetLocalFirefoxBinaryService(), fxOptions);
            else
            {
                var configuration =
                    DataOriginJobs
                        ._serviceScopeFactory
                        .CreateScope()
                        .ServiceProvider
                        .GetRequiredService<IConfiguration>();
                var seleniumHost = configuration.GetSeleniumGridHost();
                var seleniumPort = configuration.GetSeleniumGridPort();
                driver = new RemoteWebDriver(new($"{seleniumHost}:{seleniumPort}"), fxOptions);
            }

            driver.Manage().Timeouts().ImplicitWait = TimeSpan.FromSeconds(_seleniumConfig.TimeoutSeconds);
            driver.Manage().Window.Size = new Size(1920, 1400);
            //driver.Manage().Window.Minimize();
        }
        catch
        {
            try
            {
                var stale = driver;
                driver = null;
                stale?.Quit();
            }
            catch
            {
            }
        }

        if (driver == null)
            throw new Exception("Problem with driver init");
        return driver;
    }
}