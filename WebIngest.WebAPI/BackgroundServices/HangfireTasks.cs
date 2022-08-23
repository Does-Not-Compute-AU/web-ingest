using System;
using System.Collections.Generic;
using System.Linq;
using Hangfire;
using Hangfire.Dashboard;
using Hangfire.Storage;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.DependencyInjection;
using StackExchange.Redis;
using WebIngest.Common.Filters;
using WebIngest.Core.Data;
using WebIngest.Core.Jobs;

namespace WebIngest.WebAPI.BackgroundServices
{
    public static class HangfireTasks
    {
        private static IDictionary<string, BackgroundJobServer> _bjServers =
            new Dictionary<string, BackgroundJobServer>();

        private static IServiceScopeFactory _serviceScopeFactory;
        private const string DefaultJobName = "#refreshservers";

        public static void ConfigureHangfireInstance(this IApplicationBuilder app, string serverName = "MISC")
        {
            app.UseHangfireDashboard("/jobs", new DashboardOptions
            {
                IgnoreAntiforgeryToken = true,
                Authorization = new[] {new MyAuthorizationFilter()}
            });

            app.UseHangfireServer(new BackgroundJobServerOptions()
            {
                ServerName = serverName,
                Queues = new[] {"default"},
                WorkerCount = 10,
                SchedulePollingInterval = TimeSpan.FromSeconds(10)
            });

            _serviceScopeFactory = app.ApplicationServices.GetService<IServiceScopeFactory>();

            DataOriginJobs.SetServiceProvider(_serviceScopeFactory);
            RecurringJob.AddOrUpdate(
                DefaultJobName,
                () => EnqueueBackgroundJobServerRefresh(),
                "*/2 * * * *",
                TimeZoneInfo.Local
            );
        }

        public static void EnqueueBackgroundJobServerRefresh()
        {
            BackgroundJob.Enqueue(() => CheckBackgroundServers());
        }

        public static void CheckBackgroundServers()
        {
            lock (_bjServers)
            {
                var monitor = JobStorage.Current.GetMonitoringApi();
                using var jobStorage = JobStorage.Current.GetConnection();
                using var ctx = _serviceScopeFactory
                    .CreateScope()
                    .ServiceProvider
                    .GetRequiredService<ConfigurationContext>();

                var originsRequiringBackground =
                    ctx
                        .DataOrigins
                        .Where(QueryFilters.RequiresBackgroundServer)
                        .ToList();

                var desiredMutexes =
                    originsRequiringBackground
                        .Select(x => x.GetBackgroundServerMutex())
                        .ToList();

                var currentServerMutexes = _bjServers.Keys.ToList();
                var currentJobMutexes =
                    jobStorage
                        .GetRecurringJobs()
                        .Select(x => x.Id)
                        .ToList();

                var undesiredServerMutexes = currentServerMutexes.Except(desiredMutexes);
                var undesiredCurrentJobMutexes = currentJobMutexes.Except(desiredMutexes);

                // dispose and untrack stale job servers
                foreach (var mutex in undesiredServerMutexes)
                {
                    monitor.PurgeQueue(mutex);
                    _bjServers[mutex].Dispose();
                    _bjServers.Remove(mutex);
                }

                // delete stale recurring jobs
                foreach (var mutex in undesiredCurrentJobMutexes)
                {
                    if (mutex != DefaultJobName)
                        RecurringJob.RemoveIfExists(mutex);
                }

                var requiredNewServerMutexes = desiredMutexes.Except(currentServerMutexes);
                var requiredNewJobMutexes = desiredMutexes.Except(currentJobMutexes);

                // create required & not-yet-existing background servers 
                foreach (var mutex in requiredNewServerMutexes)
                {
                    var origin =
                        originsRequiringBackground
                            .First(x =>
                                x.GetBackgroundServerMutex() == mutex
                            );
                    _bjServers.Add(new(
                        mutex,
                        new BackgroundJobServer(
                            new BackgroundJobServerOptions()
                            {
                                Queues = new[] {origin.GetBackgroundServerMutex()},
                                WorkerCount = origin.Workers != 0 ? origin.Workers : Environment.ProcessorCount * 4,
                                SchedulePollingInterval = TimeSpan.FromSeconds(0.1)
                            }
                        )
                    ));
                }

                // create required & not-yet-existing job schedules
                foreach (var mutex in requiredNewJobMutexes)
                {
                    var origin =
                        originsRequiringBackground
                            .First(x =>
                                x.GetBackgroundServerMutex() == mutex
                            );
                    RecurringJob.AddOrUpdate(
                        origin.GetBackgroundServerMutex(),
                        () => EnqueueJobsForOrigin(origin.Name),
                        origin.Schedule,
                        TimeZoneInfo.Local
                    );
                }

                var redis = _serviceScopeFactory
                    .CreateScope()
                    .ServiceProvider
                    .GetRequiredService<IConnectionMultiplexer>();
                RedisSubscriber.TriggerStatsRefresh(redis);
            }
        }

        [JobDisplayName("Enqueue Jobs - {0}")]
        public static void EnqueueJobsForOrigin(string originName)
        {
            using var ctx = _serviceScopeFactory
                .CreateScope()
                .ServiceProvider
                .GetRequiredService<ConfigurationContext>();
            var origin = ctx.DataOrigins.First(x => x.Name == originName);

            foreach (var job in DataOriginJobs.GetJobsForSource(origin))
                HangfireExtensions.EnqueueBackgroundJob(origin.GetBackgroundServerMutex(), job);
        }
    }

    public class MyAuthorizationFilter : IDashboardAuthorizationFilter
    {
        public bool Authorize(DashboardContext context)
        {
            var httpContext = context.GetHttpContext();

            // Allow all authenticated users to see the Dashboard (potentially dangerous).
            return httpContext.User.Identity?.IsAuthenticated == true;
        }
    }
}