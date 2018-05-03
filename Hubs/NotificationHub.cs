using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Omi.Education.Services.Notification.Hubs.Models;
using Omi.Education.Services.Notification.Services;
using Omi.Education.Services.Notification.Enums;

namespace Omi.Education.Services.Notification.Hubs {
    public class NotificationHub : Hub {

        private readonly NotificationService _service;

        public NotificationHub (INotificationService service) {
            _service = service as NotificationService;
        }

        public Task Send (string content) {
            return _service.Send(content, Context.ConnectionId);
        }

        public override Task OnConnectedAsync () {
            var queryString = Context.Connection.GetHttpContext ();
            ConnectionType type = PublicMethod.TypeParse<ConnectionType> (queryString.Request.Query["connectionType"]);
            ClientInfo client = _service.CreateClient (Context.ConnectionId, type).ContinueWith(t => t.Result).Result;
            _service.ReplyAsync (Context.ConnectionId, ReplyStatus.Success, ReplyMethodName.Connection, client.ClientToken);
            return base.OnConnectedAsync ();
        }

        public override Task OnDisconnectedAsync (Exception exception) {
            _service.DeSubscribe(Context.ConnectionId).ContinueWith(t => t);
            _service.RemoveClient(Context.ConnectionId).ContinueWith(t => t);
            return base.OnDisconnectedAsync (exception);
        }

        public Task Subscribe (string groupToken = null) {
            return _service.Subscribe(Context.ConnectionId, groupToken);
        }

        public Task DeSubscribe (string groupName) {
            return _service.DeSubscribe(Context.ConnectionId, groupName);
        }
    }
}