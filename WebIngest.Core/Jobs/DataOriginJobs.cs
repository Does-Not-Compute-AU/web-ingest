using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using WebIngest.Common.Helpers;
using WebIngest.Common.Models;
using WebIngest.Common.Models.OriginConfiguration;
using WebIngest.Core.Data;
using WebIngest.Core.Data.EntityStorage;
using WebIngest.Core.Extraction;

namespace WebIngest.Core.Jobs
{
    public static class DataOriginJobs
    {
        private static IMemoryCache _cache;
        public static IServiceScopeFactory _serviceScopeFactory;

        public static void SetServiceProvider(IServiceScopeFactory serviceScopeFactory)
        {
            _serviceScopeFactory = serviceScopeFactory;
            _cache = _serviceScopeFactory.CreateScope().ServiceProvider.GetService<IMemoryCache>();
        }

        public static IEnumerable<Expression<Action>> GetJobsForSource(DataOrigin origin) =>
            GetJobsForType(
                origin.OriginType,
                origin.OriginTypeConfiguration,
                origin.Name
            );

        public static IEnumerable<Expression<Action>> GetJobsForConfig(
            OriginTypeConfiguration config,
            string dataSourceName
        ) => GetJobsForType(config.GetConfigurationType(), config, dataSourceName);

        public static IEnumerable<Expression<Action>> GetJobsForType(
            OriginType originType,
            OriginTypeConfiguration config,
            string dataSourceName
        )
        {
            return originType switch
            {
                OriginType.HTTP => FetchJobs.HttpJobs.GetJobsForHttp(
                    dataSourceName,
                    config
                ),
                OriginType.Scripted => FetchJobs.ScriptedJobs.GetJobsForScripted(
                    dataSourceName,
                    config
                ),
                OriginType.SiteMapCrawler => FetchJobs.SiteMapCrawler.GetJobsForSiteMapCrawler(
                    dataSourceName,
                    config
                ),
                _ => throw new NotImplementedException($"Jobs Not Yet Supported for {originType}, contact developer")
            };
        }

        public static string SaveResult(string sourceName, string data, OriginTypeConfiguration originConfig,
            int? dataTypeId = null)
        {
            int savedObjects;
            using (var scope = _serviceScopeFactory.CreateScope())
            {
                var dataOrigin = _cache.GetOrCreate($"SaveResult-DataOrigin-{sourceName}", x =>
                {
                    x.AbsoluteExpirationRelativeToNow = TimeSpan.FromSeconds(10);
                    using var ctx = scope.ServiceProvider.GetRequiredService<ConfigurationContext>();
                    return ctx
                        .DataOrigins
                        .Include(o => o.Mappings)
                        .ThenInclude(m => m.DataType) 
                        .First(ds => ds.Name == sourceName);
                });
                
                var sourceMappings = dataOrigin.Mappings.ToList();

                if (dataTypeId != null)
                    sourceMappings = sourceMappings.Where(x => x.DataTypeId == dataTypeId).ToList();

                var allParsedObjects = new List<Tuple<Mapping, IDictionary<string, object>[]>>();
                // do file / result parsing in parallel and collect all results
                Parallel.ForEach(sourceMappings, mapping =>
                {
                    var parsedObjects = dataOrigin.ContentType switch
                    {
                        ContentType.JSON => JsonExtractor.ValuesFromJson(mapping, dataOrigin.ContentTypeConfiguration,
                            originConfig, data),
                        ContentType.HTML => HtmlExtractor.ValuesFromHtml(mapping, dataOrigin.ContentTypeConfiguration,
                            originConfig, data),
                        ContentType.CSV => CsvExtractor.ValuesFromCsv(mapping, dataOrigin.ContentTypeConfiguration,
                            originConfig, data),
                        _ => throw new NotSupportedException()
                    };

                    // remove all empty / null parse results
                    parsedObjects = parsedObjects.Where(x => x.Values.Any()).ToArray();

                    //dont proceed if no values were parsed to string insert
                    if (parsedObjects.Any())
                        allParsedObjects.Add(new Tuple<Mapping, IDictionary<string, object>[]>(mapping, parsedObjects));
                });

                // loop through all groups synchronously and insert results to db
                var storages =
                    scope.ServiceProvider
                        .GetServices<IEntityStorage>();
                foreach (var storage in storages)
                foreach (var (mapping, parsedObjects) in allParsedObjects)
                    storage.BulkInsertEntities(dataOrigin, mapping, parsedObjects);

                savedObjects = allParsedObjects.Sum(x => x.Item2.Length);
            }

            if (savedObjects != 0)
            {
                Task.Run(PublishChangesSaved);
                return $"Saved {savedObjects} Items";
            }

            return "No Items Parsed From Response";
        }

        private static async Task PublishChangesSaved()
        {
            var redis = _serviceScopeFactory.CreateScope().ServiceProvider.GetService<IConnectionMultiplexer>();
            var subscriber = redis.GetSubscriber();
            await subscriber.PublishAsync(EventHelper.DbSaveChangesEvent, "");
        }
    }
}