using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using TurnerSoftware.SitemapTools;
using WebIngest.Common.Extensions;
using WebIngest.Core.Scraping.WebClients;

namespace WebIngest.Core.Scraping
{
    public static class ScrapingHelpers
    {
        public static async Task<IEnumerable<string>> GetUrlPathsFromSitemaps(string url)
        {
            var baseUri = new Uri(url);
            var sitemapQuery = new SitemapQuery();
            IEnumerable<SitemapFile> queryRes = Enumerable.Empty<SitemapFile>();
            try
            {
                queryRes = await sitemapQuery.GetAllSitemapsForDomainAsync(baseUri.Host);
            }
            catch
            {
                // ignored
            }

            var sitemapEntries = queryRes.ToList();

            // if no sitemap entries found, try fetching robots.txt with added headers
            if (!sitemapEntries.Any())
            {
                sitemapQuery = new SitemapQuery(new HttpClient()
                {
                    DefaultRequestHeaders =
                    {
                        {"user-agent", WebIngestClientHelpers.GetRandomUserAgent()},
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