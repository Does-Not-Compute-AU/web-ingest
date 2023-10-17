using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using TurnerSoftware.SitemapTools;
using WebIngest.Core.Scraping.WebClients;

namespace WebIngest.Core.Scraping
{
    public static class ScrapingHelpers
    {
        public static async Task<IEnumerable<string>> GetUrlPathsFromSitemaps(string url, WebProxy proxy = null)
        {
            var baseUri = new Uri(url);
            IEnumerable<SitemapFile> queryRes = Enumerable.Empty<SitemapFile>();
            IList<Exception> exceptions = new List<Exception>();

            // try first with decompression, collect error for rethrow
            try
            {
                queryRes = await new SitemapQuery(
                    new HttpClient(disposeHandler: true, handler: new HttpClientHandler
                    {
                        AutomaticDecompression = DecompressionMethods.GZip | DecompressionMethods.Deflate,
                        Proxy = proxy
                    })
                    {
                        DefaultRequestHeaders = {
                            { "user-agent", WebIngestClientHelpers.GetRandomUserAgent() },
                            { "origin", baseUri.GetLeftPart(UriPartial.Authority) },
                            { "user-agent", baseUri.Host },
                            { "accept", "application/xml" }
                        }
                    }
                ).GetAllSitemapsForDomainAsync(baseUri.Host);
            }
            catch(Exception e)
            {
                exceptions.Add(e);
            }

            var sitemapEntries = queryRes.ToList();

            // if no sitemap entries found yet, try without decompression but keeping headers
            if (!sitemapEntries.Any())
            {
                try
                {
                    queryRes = await new SitemapQuery(new HttpClient()
                    {
                        DefaultRequestHeaders = {
                            { "user-agent", WebIngestClientHelpers.GetRandomUserAgent() },
                            { "origin", baseUri.GetLeftPart(UriPartial.Authority) },
                            { "user-agent", baseUri.Host },
                            { "accept", "application/xml" }
                        }
                    }).GetAllSitemapsForDomainAsync(baseUri.Host);
                    sitemapEntries = queryRes.ToList();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            
            // if still no sitemap entries found, try without customised httpClient
            if (!sitemapEntries.Any())
            {
                try
                {
                    queryRes = await new SitemapQuery().GetAllSitemapsForDomainAsync(baseUri.Host);
                    sitemapEntries = queryRes.ToList();
                }
                catch (Exception e)
                {
                    exceptions.Add(e);
                }
            }
            
            // if no sitemaps found by now, but exceptions were thrown, throw them now
            if(!sitemapEntries.Any() && exceptions.Any())
                throw new AggregateException(exceptions);

            sitemapEntries = Enumerable.DistinctBy(sitemapEntries, x => x.Location).ToList();
            return sitemapEntries
                .SelectMany(x => x.Urls?.Select(y => y.Location?.AbsoluteUri))
                .Where(x => !string.IsNullOrEmpty(x))
                .Distinct();
        }
    }
}