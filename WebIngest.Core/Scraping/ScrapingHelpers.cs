using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TurnerSoftware.SitemapTools;
using WebIngest.Common.Extensions;

namespace WebIngest.Core.Scraping
{
    public static class ScrapingHelpers
    {
        public static async Task<IEnumerable<string>> GetUrlPathsFromSitemaps(string url)
        {
            var baseUri = new Uri(url);
            var sitemapQuery = new SitemapQuery();
            var queryRes = await sitemapQuery.GetAllSitemapsForDomainAsync(baseUri.Host);
            
            var sitemapEntries = queryRes.ToList();

            // if no sitemap entries found, try fetching robots.txt with added headers
            if (!sitemapEntries.Any())
            {
                sitemapQuery = new SitemapQuery(new HttpClient()
                {
                    DefaultRequestHeaders =
                    {
                        {"user-agent", IngestWebClient.GetRandomUserAgent()},
                        {"origin", baseUri.GetLeftPart(UriPartial.Authority)},
                        {"user-agent", baseUri.Host},
                        {"accept", "application/xml"}
                    }
                });
                queryRes = await sitemapQuery.GetAllSitemapsForDomainAsync(baseUri.Host);
                sitemapEntries = queryRes.ToList();
            }

            sitemapEntries = Enumerable.DistinctBy(sitemapEntries, x => x.Location).ToList();
            return sitemapEntries
                .SelectMany(x => x.Urls?.Select(y => y.Location?.AbsoluteUri))
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct();
        }
    }
}