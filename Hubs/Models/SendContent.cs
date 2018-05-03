using Omi.Education.Services.Notification.Enums;

namespace Omi.Education.Services.Notification.Hubs.Models
{
    public class SendContent
    {
        public string Deliver { get; set; }
        public string Target { get; set; }
        public string Message { get; set; }
        public string From { get; set; }
    }
}