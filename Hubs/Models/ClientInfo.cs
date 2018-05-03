
using System.Collections.Generic;
using Omi.Education.Services.Notification.Enums;

namespace Omi.Education.Services.Notification.Hubs.Models
{
    public class ClientInfo
    {
        private string _clientId = null;
        public ClientInfo(string clientId)
        {
            _clientId = clientId;
        }

        public string ClientId { get { return _clientId; } set { _clientId = value; } }
        public string ClientToken { get; set; }
        public ConnectionType ConnectionType { get; set; }
        public List<ClientInfo> Clients { get; set; }
    }
}