using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using WebIngest.Core.Data;
using StackExchange.Redis;
using WebIngest.Common.Helpers;
using WebIngest.Common.Models.Messaging;
using WebIngest.Core.Info;
using WebIngest.WebAPI.Hubs;

namespace WebIngest.WebAPI.BackgroundServices
{
    public class RedisSubscriber : BackgroundService
    {
        public const string UpdateStatsChannel = "RefreshStatistics";

        private readonly IConnectionMultiplexer _connectionMultiplexer;
        private readonly ConfigurationContext _context;
        private readonly IConfiguration _configuration;
        private readonly IHubContext<SignalRHub> _signalrHubContext;
        private readonly StatisticsService _statisticsService;

        public RedisSubscriber(
            IServiceScopeFactory scopeFactory,
            IConfiguration configuration,
            IConnectionMultiplexer connectionMultiplexer,
            IHubContext<SignalRHub> signalrHubContext,
            StatisticsService statisticsService
        )
        {
            _signalrHubContext = signalrHubContext;
            _statisticsService = statisticsService;
            _configuration = configuration;
            _connectionMultiplexer = connectionMultiplexer;

            using var scope = scopeFactory.CreateScope();
            _context = scope.ServiceProvider.GetRequiredService<ConfigurationContext>();
        }

        public static void TriggerStatsRefresh(IConnectionMultiplexer connectionMultiplexer)
        {
            var subscriber = connectionMultiplexer.GetSubscriber();
            subscriber.PublishAsync(UpdateStatsChannel, "");
        }

        protected override Task ExecuteAsync(CancellationToken stoppingToken)
        {
            var subscriber = _connectionMultiplexer.GetSubscriber();
            
            return Task.Run(() =>
            {
                // DB Save Event Actions
                subscriber.SubscribeAsync(EventHelper.DbSaveChangesEvent,
                    (channel, message) =>
                    {
                        TriggerStatsRefresh(_connectionMultiplexer);
                    });

                // notification messages
                subscriber.SubscribeAsync(NotificationViewModel.SubscriberChannel, (channel, message) =>
                {
                    _signalrHubContext
                        .Clients
                        .All
                        .SendAsync(
                            NotificationViewModel.SubscriberChannel,
                            new NotificationViewModel()
                            {
                                Notification = message
                            },
                            stoppingToken);
                });

                // calls to refresh stats
                subscriber.SubscribeAsync(UpdateStatsChannel, (channel, message) =>
                {
                    // send updated stats to signalR clients
                    Task.Run(() => _signalrHubContext.SendStatistics(_statisticsService, stoppingToken), stoppingToken);
                    Task.Run(() => _signalrHubContext.SendHomePageStats(_statisticsService, stoppingToken),
                        stoppingToken);
                });
            }, stoppingToken);
        }
    }
}