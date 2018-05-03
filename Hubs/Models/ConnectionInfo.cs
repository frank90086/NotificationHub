using Microsoft.AspNetCore.SignalR.Client;
using Omi.Education.Services.Notification.Enums;

namespace Omi.Education.Services.Notification.Hubs.Models
{
    public class ConnectionInfo
    {
        public HubConnection HubConnection { get; set; }
        public ConnectionType ConnectionType { get; set; }
        public string Url { get; set; }
        public string HubToken { get; set; }
    }
}