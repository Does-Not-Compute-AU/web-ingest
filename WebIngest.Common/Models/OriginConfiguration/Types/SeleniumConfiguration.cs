namespace WebIngest.Common.Models.OriginConfiguration.Types;

public class SeleniumConfiguration
{
    /// <summary>
    /// A master flag, that webingest will look for as whether selenium should be used.
    /// If false, any other selenium config becomes irrelevant and HTTPClient is used.
    /// </summary>
    public bool UseSeleniumDriver { get; set; }
    
    /// <summary>
    /// Whether to use the binary selenium driver packaged with this repo.
    /// Better alternative is to configure selenium grid
    /// </summary>
    public bool UseBinaryDriver { get; set; } = false;
    
    /// <summary>
    /// Whether the selenium driver should be headless or not. Some websites
    /// require non-headless to trigger lazy-loaded assets.
    /// </summary>
    public bool Headless { get; set; } = true;
    
    /// <summary>
    /// Manual time-wait option for page load.
    /// Least effective option, but sometimes necessary.
    /// </summary>
    public int ThreadSleepSeconds { get; set; } = 0;
    
    /// <summary>
    /// How long the browser should wait for elements by selector or PageContains
    /// </summary>
    public int TimeoutSeconds { get; set; } = 10;
    
    /// <summary>
    /// A CSS selector to wait for the existence of as evidence of page load complete
    /// </summary>
    public string ElementWaitSelector { get; set; }
    
    /// <summary>
    /// A piece of text to wait for the existence of as evidence of page load complete
    /// </summary>
    public string WaitForPageContains { get; set; }
    
    /// <summary>
    /// A piece of text to wait for the disappearance of as evidence of page load complete
    /// </summary>
    public string WaitForPageNotContains { get; set; }
}