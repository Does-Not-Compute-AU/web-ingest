using System;
using System.Net;
using WebIngest.Common.Models.OriginConfiguration.Types;

namespace WebIngest.Core.Scraping.WebClients
{
    public class IngestWebClient : WebClient, IWebIngestWebClient
    {
        private readonly HttpConfiguration _httpConfiguration;

        public IngestWebClient(HttpConfiguration configuration)
        {
            _httpConfiguration = configuration;
            this.Proxy = _httpConfiguration.Proxy;
        }

        protected override WebRequest GetWebRequest(Uri address)
        {
            var request = base.GetWebRequest(address) as HttpWebRequest;
            if (request != null)
            {
                var userAgent = _httpConfiguration.RandomUserAgents
                    ? WebIngestClientHelpers.GetRandomUserAgent()
                    : _httpConfiguration.SpecifiedUserAgent ??  WebIngestClientHelpers.DefaultUserAgent;

                request.Headers.Add("user-agent", userAgent);
                request.Headers.Add("origin", address.GetLeftPart(UriPartial.Authority));
                request.Headers.Add("host", address.Host);
                request.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
                request.Headers.Add("accept-encoding", "gzip, deflate, br");
                request.Headers.Add("accept-language", "en-AU,en-GB;q=0.9,en-US;q=0.8,en;q=0.7,zh-TW;q=0.6,zh;q=0.5");
                request.Headers.Add("upgrade-insecure-requests", "1");
                request.AutomaticDecompression = DecompressionMethods.All;
            }

            return request;
        }
    }
}