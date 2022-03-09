using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using WebIngest.Common.Models.OriginConfiguration;
using WebIngest.Core.Scraping;

namespace WebIngest.Core.Jobs.FetchJobs
{
    public class SiteMapCrawler
    {
        public static IEnumerable<Expression<Action>> GetJobsForSiteMapCrawler(
            string dataSourceName,
            OriginTypeConfiguration originConfig
        )
        {
            IList<string> sitemapHostUrls = 
                originConfig
                .SiteMapCrawlerConfiguration
                .Urls;

            var crawlUrls = new List<string>();
            foreach(var url in sitemapHostUrls)
                crawlUrls.AddRange(ScrapingHelpers.GetUrlPathsFromSitemaps(url).Result);

            originConfig.HttpConfiguration = originConfig.SiteMapCrawlerConfiguration;
            originConfig.HttpConfiguration.Urls = crawlUrls;
            return HttpJobs.GetJobsForHttp(dataSourceName, originConfig);
        }
    }
}