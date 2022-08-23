using System.Collections.Generic;
using System.Net;
using Newtonsoft.Json;

namespace WebIngest.Common.Models.OriginConfiguration.Types
{
    public class HttpConfiguration
    {
        public bool ThrowWebExceptions { get; set; }
        public bool UseSeleniumDriver { get; set; }
        public bool ShuffleUrls { get; set; }
        public bool RandomUserAgents { get; set; }
        public bool ProxyRequests { get; set; }
        public string SpecifiedUserAgent { get; set; }
        public string ProxyAddress { get; set; }
        public string ProxyUsername { get; set; }
        public string ProxyPassword { get; set; }
        public List<string> Urls { get; set; } = new();

        [JsonIgnore]
        public WebProxy Proxy => !ProxyRequests
            ? null
            : new(ProxyAddress)
            {
                Credentials =
                    string.IsNullOrEmpty(ProxyUsername) ||
                    string.IsNullOrEmpty(ProxyPassword)
                        ? null
                        : new NetworkCredential()
                        {
                            UserName = ProxyUsername,
                            Password = ProxyPassword
                        }
            };
    }
}