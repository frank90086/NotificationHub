using Omi.Education.Services.Notification.Enums;

namespace Omi.Education.Services.Notification.Hubs.Models
{
    public class ReplyContent
    {
        public ReplyStatus ReplyStatus { get; set; }
        public ReplyMethodName ReplyMethodName { get; set; }
        public string Content { get; set; }
    }
}