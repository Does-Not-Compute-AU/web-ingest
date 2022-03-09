namespace WebIngest.Common.Models.Messaging
{
    public class NotificationViewModel
    {
        public const string HubUrl = "/hubs/signalr";
        public const string SubscriberChannel = "ReceiveNotifications";
        
        public string Notification { get; set; }
    }
}