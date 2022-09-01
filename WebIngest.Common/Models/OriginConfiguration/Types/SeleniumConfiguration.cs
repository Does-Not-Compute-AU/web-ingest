namespace WebIngest.Common.Models.OriginConfiguration.Types;

public class SeleniumConfiguration
{
    public bool UseSeleniumDriver { get; set; }
    public int ThreadSleepSeconds { get; set; } = 0;
    public bool Headless { get; set; } = true;
    public int TimeoutSeconds { get; set; } = 15;
    public string ElementWaitSelector { get; set; }
    public string WaitForPageContains { get; set; }
    public string WaitForPageNotContains { get; set; }
}