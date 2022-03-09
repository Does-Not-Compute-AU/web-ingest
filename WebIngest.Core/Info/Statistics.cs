using System;
using System.Linq;
using Hangfire;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.DependencyInjection;
using WebIngest.Common.Filters;
using WebIngest.Common.Models;
using WebIngest.Common.Models.Messaging;
using WebIngest.Core.Data;
using WebIngest.Core.Data.EntityStorage;

namespace WebIngest.Core.Info
{
    public class StatisticsService
    {
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly IEntityStorage _entityStorage;
        private readonly IMemoryCache _cache;

        private ConfigurationContext GetConfigurationContext() =>
            _scopeFactory.CreateScope().ServiceProvider.GetService<ConfigurationContext>();

        public StatisticsService(
            IServiceScopeFactory scopeFactory,
            IEntityStorage entityStorage,
            IMemoryCache cache
        )
        {
            _entityStorage = entityStorage;
            _cache = cache;
            _scopeFactory = scopeFactory;
        }

        public HomePageViewModel GetStatisticsByOrigin()
        {
            using var ctx = GetConfigurationContext();
            var res =
                new HomePageViewModel()
                {
                    OriginStatistics = ctx.DataOrigins.Select(GetStatistics)
                };
            return res;
        }

        public StatisticsViewModel GetStatistics(DataOrigin dataOrigin = null)
        {
            return _cache.GetOrCreate($"StatisticsViewModel-{dataOrigin?.Name}" ,x =>
            {
                x.SlidingExpiration = TimeSpan.FromSeconds(5);
                
                var hangfireMonitor = JobStorage.Current.GetMonitoringApi();
                var hangfireQueues = hangfireMonitor.Queues();
                using var ctx = GetConfigurationContext();
                IQueryable<DataType> dataTypes = ctx.DataTypes;

                // if origin filter is specified, filter queues and types
                if (dataOrigin != null)
                {
                    var isBackground = QueryFilters.RequiresBackgroundServer(dataOrigin);
                    hangfireQueues = isBackground
                        ? hangfireQueues
                            .Where(q => q.Name == dataOrigin.GetBackgroundServerMutex())
                            .ToList()
                        : new();

                    dataTypes =
                        ctx
                            .Mappings
                            .Where(x => x.DataOrigin == dataOrigin)
                            .Select(x => x.DataType)
                            .Distinct();
                }

                long totalDataPoints =
                    dataTypes
                        .ToList()
                        .Sum(dt =>
                            _entityStorage.CountStorageEntries(dt, dataOrigin)
                        );

                return new StatisticsViewModel()
                {
                    OriginMutex = dataOrigin?.GetBackgroundServerMutex(),
                    OriginId = dataOrigin?.Id,
                    OriginName = dataOrigin?.Name,
                    OriginSchedule = dataOrigin?.Schedule,
                    DataPoints = totalDataPoints,

                    ProcessingJobs = hangfireQueues.Sum(x => x.Fetched ?? 0),
                    QueuedJobs = hangfireQueues.Sum(x => x.Length),
                    FailedJobs = dataOrigin == null ? hangfireMonitor.FailedCount() : 0
                };
            });
        }
    }
}