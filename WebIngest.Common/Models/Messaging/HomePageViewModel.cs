using System.Collections.Generic;

namespace WebIngest.Common.Models.Messaging
{
    public class HomePageViewModel
    {
        public const string SubscriberChannel = "ReceiveHomePage";
        public const string RefreshChannel = "RefreshHomePage";

        public IEnumerable<StatisticsViewModel> OriginStatistics { get; set; }
    }
}