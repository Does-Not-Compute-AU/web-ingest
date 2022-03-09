using System;
using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using WebIngest.Common.Models.Messaging;

namespace WebIngest.WebUI.Services.Hubs
{
    public class SignalRService
    {
        private readonly HubConnection _hubConnection;

        public SignalRService(
            NavigationManager navigationManager)
        {
            _hubConnection = new HubConnectionBuilder()
                .WithUrl(navigationManager.ToAbsoluteUri(NotificationViewModel.HubUrl))
                .AddMessagePackProtocol()
                .WithAutomaticReconnect()
                .Build();

            _hubConnection.StartAsync();
        }


        public void NotificationSubscriber(params Action<NotificationViewModel>[] callbacks)
            => ChannelSubscriber(NotificationViewModel.SubscriberChannel, callbacks);


        public void RefreshHomePageStats() =>
            _hubConnection.SendAsync(HomePageViewModel.RefreshChannel);

        public void HomePageSubscriber(params Action<HomePageViewModel>[] callbacks)
            => ChannelSubscriber(HomePageViewModel.SubscriberChannel, callbacks);


        public void RefreshStatistics() =>
            _hubConnection.SendAsync(StatisticsViewModel.RefreshChannel);

        public void StatisticsSubscriber(params Action<StatisticsViewModel>[] callbacks)
            => ChannelSubscriber(StatisticsViewModel.SubscriberChannel, callbacks);


        private void ChannelSubscriber<T>(string channel, params Action<T>[] callbacks)
        {
            foreach (var callback in callbacks)
                _hubConnection.On(channel, callback);
        }
    }
}