using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Net;
using System.Net.Http;
using Hangfire;
using WebIngest.Common.Extensions;
using WebIngest.Common.Models.OriginConfiguration;
using WebIngest.Core.Scraping;
using WebIngest.Core.Scraping.WebClients;

namespace WebIngest.Core.Jobs.FetchJobs
{
    public static class HttpJobs
    {
        public static IEnumerable<Expression<Action>> GetJobsForHttp(
            string dataSourceName,
            OriginTypeConfiguration originConfig
        )
        {
            IList<string> urls = new List<string>(
                originConfig
                    .HttpConfiguration
                    .Urls
            );

            if (originConfig.HttpConfiguration.ShuffleUrls)
                urls = urls.Shuffle();

            // purge potentially massive list of source URLs to prevent serialization and storage of now-redundant prop
            originConfig.HttpConfiguration.Urls.Clear();

            return urls
                .Select(url =>
                    (Expression<Action>) (() =>
                        FetchHttp(dataSourceName, url, originConfig))
                );
        }

        [JobDisplayName("Fetch HTTP Resource - {0}")]
        [AutomaticRetry(Attempts = 1, DelaysInSeconds = new []{900}, OnAttemptsExceeded = AttemptsExceededAction.Fail)]
        public static string FetchHttp(string dataSourceName, string url, OriginTypeConfiguration originConfig)
        {

            IWebIngestWebClient client = originConfig.HttpConfiguration.UseSeleniumDriver
                ? new SeleniumWebClient(originConfig.HttpConfiguration)
                : new IngestWebClient(originConfig.HttpConfiguration);

            var data = string.Empty;
            try
            {
                data = client.DownloadString(url);
            }
            catch (WebException)
            {
                if (originConfig.HttpConfiguration.ThrowWebExceptions)
                    throw;
            }

            originConfig.PassThroughVars["WI_SOURCE_URL"] = url;
            return string.IsNullOrEmpty(data)
                ? string.Empty
                : DataOriginJobs.SaveResult(dataSourceName, data, originConfig);
        }
    }
}