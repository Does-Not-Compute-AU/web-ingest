namespace WebIngest.Common.Models.Messaging
{
    public class StatisticsViewModel
    {
        public const string RefreshChannel = "RefreshStatistics";
        public const string SubscriberChannel = "ReceiveStatistics";
        
        public long ProcessingJobs { get; set; }
        public long QueuedJobs { get; set; }
        public long FailedJobs { get; set; }
        public long DataPoints { get; set; }
        
        public int? OriginId { get; set; }
        public string OriginName { get; set; }
        public string OriginSchedule { get; set; }
        public string OriginMutex { get; set; }
    }
}