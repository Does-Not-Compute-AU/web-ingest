using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using WebIngest.Common.Models.Messaging;
using WebIngest.Core.Info;

namespace WebIngest.WebAPI.Hubs
{
    [Authorize]
    public class SignalRHub : Hub
    {
        private StatisticsService _statisticsService; 
        public SignalRHub(StatisticsService statisticsService)
        {
            _statisticsService = statisticsService;
        }
        
        [HubMethodName(HomePageViewModel.RefreshChannel)]
        public async Task RefreshHomePageStats()
        {
            await Clients
                .Caller
                .SendAsync(
                    HomePageViewModel.SubscriberChannel,
                    _statisticsService.GetStatisticsByOrigin()
                );
        }
        
        [HubMethodName(StatisticsViewModel.RefreshChannel)]
        public async Task RefreshStatistics()
        {
            await Clients
                .Caller
                .SendAsync(
                    StatisticsViewModel.SubscriberChannel,
                    _statisticsService.GetStatistics()
                );
        }
    }

    public static class SignalRHubMethods
    {
        public static async Task SendStatistics(
            this IHubContext<SignalRHub> hubContext,
            StatisticsService statisticsService,
            CancellationToken stoppingToken)
        {
            await hubContext
                .Clients
                .All
                .SendAsync(
                    StatisticsViewModel.SubscriberChannel,
                    statisticsService.GetStatistics(),
                    stoppingToken
                );
        }
        
        public static async Task SendHomePageStats(
            this IHubContext<SignalRHub> hubContext,
            StatisticsService statisticsService,
            CancellationToken stoppingToken)
        {
            await hubContext
                .Clients
                .All
                .SendAsync(
                    HomePageViewModel.SubscriberChannel,
                    statisticsService.GetStatisticsByOrigin(),
                    stoppingToken
                );
        }
    }
}