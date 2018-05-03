using System;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.SignalR.Client;
using Microsoft.Extensions.DependencyInjection;
using Omi.Education.Services.Notification.Enums;
using Omi.Education.Services.Notification.Hubs;
using Omi.Education.Services.Notification.Hubs.Models;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Omi.Education.Services.Notification.Services
{
    public class NotificationService : INotificationService
    {
        public List<ClientInfo> clientInfos { get; set; }
        internal static List<ClientInfo> _List = new List<ClientInfo>();
        internal static List<CacheInfo> _cacheList = new List<CacheInfo>();
        private List<ConnectionInfo> _connectionList = new List<ConnectionInfo>();
        private ConnectionInfo _baseconnectionHub;
        public bool _isTerminal = false;
        // public string selfUrl = "http://localhost:10596/notificationhub";
        public string selfUrl = "http://tliix-frank.azurewebsites.net/notificationhub";

        public NotificationService(string baseUrl, IApplicationBuilder app)
        {
            var serviceScope = app.ApplicationServices.GetRequiredService<IServiceProvider>().CreateScope();
            hub = serviceScope.ServiceProvider.GetService<IHubContext<NotificationHub>>();
            if (!String.IsNullOrEmpty(baseUrl))
            {
                // _connectionList.Add(ConnectionSelfAsync().ContinueWith(t => t.Result).Result);
                _baseconnectionHub = ConnectionAsync(baseUrl).ContinueWith(t => t.Result).Result;
            }
            else
            {
                _baseconnectionHub = new ConnectionInfo()
                {
                    ConnectionType = ConnectionType.Base,
                    HubToken = PublicMethod.GetToken(),
                    Url = "",
                };
                _isTerminal = true;
            }
        }

        private IHubContext<NotificationHub> hub { get; set; }

        #region Send Method
        public Task Send(string content, string connectionId = null)
        {
            SendContent value = PublicMethod.JsonDeSerialize<SendContent>(content);
            if (value == null)
                return Task.CompletedTask;
            value.Deliver = String.IsNullOrEmpty(value.Deliver) ? PublicMethod.CombinationString(_baseconnectionHub.HubToken, selfUrl) : value.Deliver;
            string newContent = PublicMethod.JsonSerialize(value);
            string[] info = PublicMethod.SplitString(value.Deliver);

            CacheInfo cache = String.IsNullOrEmpty(connectionId) ? null : HubSelector(value.Target);
            List<ClientInfo> clients = ClientSelector(info[0], value.Target);
            if (clients.Count > 0 && !String.IsNullOrEmpty(value.Target))
            {
                foreach (ClientInfo client in clients)
                {
                    switch (client.ConnectionType)
                    {
                        case ConnectionType.Client:
                        case ConnectionType.Hub:
                        case ConnectionType.Service:
                            sendSignal(newContent, client);
                            break;
                        case ConnectionType.Group:
                            sendGroup(newContent, client, connectionId);
                            break;
                    }
                }
            }
            else if (cache != null)
            {
                if (cache.ClientToken != value.From)
                {
                    ConnectionInfo connection = SubConnection(cache).Result;
                    connection.HubConnection.InvokeAsync("Send", newContent);
                }
            }
            else if (!String.IsNullOrEmpty(value.Target)) { }
            else
            {
                broadcast(newContent, connectionId);
            }

            if (_cacheList.Where(x => x.ClientToken == value.From).FirstOrDefault() == null &&
                _List.Where(x => x.ClientToken == value.From).FirstOrDefault() == null &&
                _List.Where(x => x.ClientToken == info[0]).FirstOrDefault() == null)
            {
                _cacheList.Add(new CacheInfo()
                {
                    ClientToken = value.From,
                        Token = info[0],
                        Url = info[1] + "?connectionType=SubHub"
                });
            }

            return Task.CompletedTask;
        }

        private Task broadcast(string content, string connectionId = null)
        {
            List<ClientInfo> broList = _List.Where(x => x.ClientId != connectionId).ToList();
            foreach (ClientInfo item in broList)
            {
                hub.Clients.Client(item.ClientId).SendAsync("Send", content);
            }
            if (!_isTerminal && !String.IsNullOrEmpty(connectionId))
            {
                _baseconnectionHub.HubConnection.InvokeAsync("Send", content);
            }
            return Task.CompletedTask;
        }

        private Task sendGroup(string content, ClientInfo target, string connectionId = null)
        {
            foreach (ClientInfo item in target.Clients.Where(x => x.ClientId != connectionId))
            {
                hub.Clients.Client(item.ClientId).SendAsync("Send", content);
            }
            if (!_isTerminal && !String.IsNullOrEmpty(connectionId))
            {
                _baseconnectionHub.HubConnection.InvokeAsync("Send", content);
            }
            return Task.CompletedTask;
        }

        private Task sendSignal(string content, ClientInfo target)
        {
            hub.Clients.Client(target.ClientId).SendAsync("Send", content);
            return Task.CompletedTask;
        }

        private CacheInfo HubSelector(string token)
        {
            CacheInfo cache = _cacheList.Where(x => x.ClientToken == token).FirstOrDefault();
            return cache;
        }

        private List<ClientInfo> ClientSelector(string hubToken, string token)
        {
            List<ClientInfo> target;
            if (String.IsNullOrEmpty(token))
            {
                target = _List.Where(x => x.ClientToken != hubToken & x.ConnectionType != ConnectionType.Self).ToList();
                return target;
            }
            else
            {
                target = _List.Where(x => x.ClientToken != hubToken & x.ConnectionType == ConnectionType.Hub).ToList();
                if (target.Count > 0)
                {
                    return target;
                }
                target = _List.Where(x => x.ClientToken == token).ToList();
                return target;
            }
        }
        #endregion

        #region Client Method
        internal ClientInfo GetClient(string token)
        {
            ClientInfo client = _List.Where(x => x.ClientToken == token).FirstOrDefault();
            return client;
        }

        internal Task<ClientInfo> CreateClient(string connectionId, ConnectionType type, string token = null)
        {
            var task = Task.Run(() =>
            {
            ClientInfo client = new ClientInfo(connectionId)
            {
            ClientId = connectionId,
            ClientToken = String.IsNullOrEmpty(token) ? PublicMethod.GetToken() : token,
            ConnectionType = type,
            Clients = new List<ClientInfo>()
                };
                _List.Add(client);
                return client;
            });

            return task;
        }

        internal Task RemoveClient(string connectionId)
        {
            _List.Remove(_List.Where(x => x.ClientId == connectionId).FirstOrDefault());
            return Task.CompletedTask;
        }
        #endregion

        #region Subscribe Method
        internal Task Subscribe(string connectionId, string groupToken = null)
        {
            ReplyStatus status;
            ClientInfo group = GetClient(groupToken);
            if (group == null)
            {
                group = CreateClient("", ConnectionType.Group).ContinueWith(t => t.Result).Result;
            }
            group.ClientToken = String.IsNullOrEmpty(groupToken) ? group.ClientToken : groupToken;
            try
            {
                ClientInfo client = _List.Where(x => x.ClientId == connectionId).FirstOrDefault();
                group.Clients.Add(client);
                status = ReplyStatus.Success;
            }
            catch (System.Exception)
            {
                status = ReplyStatus.Error;
            }
            return ReplyAsync(connectionId, status, ReplyMethodName.Subscribe, group.ClientToken);
        }

        internal Task DeSubscribe(String connectionId)
        {
            ClientInfo client = _List.Where(x => x.ClientId == connectionId).FirstOrDefault();
            List<ClientInfo> groups = _List.Where(x => x.ConnectionType == ConnectionType.Group).ToList();
            if (groups.Count > 0)
            {
                foreach (ClientInfo group in groups)
                {
                    if (group.Clients.Contains(client))
                    {
                        group.Clients.Remove(client);
                    }
                    if (group.Clients.Count == 0)
                    {
                        _List.Remove(group);
                    }
                }
            }
            return Task.CompletedTask;
        }

        internal Task DeSubscribe(String connectionId, string groupName)
        {
            ReplyStatus status = ReplyStatus.Error;
            ClientInfo client = _List.Where(x => x.ClientId == connectionId).FirstOrDefault();
            ClientInfo group = _List.Where(x => x.ConnectionType == ConnectionType.Group && x.ClientToken == groupName).FirstOrDefault();
            if (group != null)
            {
                group.Clients.Remove(client);
                if (group.Clients.Count == 0)
                {
                    _List.Remove(group);
                }
                status = ReplyStatus.Success;
            }
            return ReplyAsync(connectionId, status, ReplyMethodName.DeSubscribe, groupName);
        }
        #endregion

        private async Task<ConnectionInfo> SubConnection(CacheInfo cache)
        {
            ConnectionInfo connection = _connectionList.Where(x => x.Url == cache.Url).FirstOrDefault();
            if (connection != null)
                return connection;
            connection = await ConnectionAsync(cache.Url, ConnectionType.SubHub);
            _connectionList.Add(connection);
            return connection;
        }

        internal Task ReplyAsync(string ClientId, ReplyStatus status, ReplyMethodName methodName, string content)
        {
            string replyContent = PublicMethod.JsonSerialize<ReplyContent>(CreatReplyContent(status, methodName, content));
            return hub.Clients.Client(ClientId).SendAsync("Reply", replyContent);
        }

        internal ReplyContent CreatReplyContent(ReplyStatus status, ReplyMethodName methodName, string content)
        {
            ReplyContent replyModel = new ReplyContent()
            {
                ReplyStatus = status,
                ReplyMethodName = methodName,
                Content = content
            };
            return replyModel;
        }

        private async Task<ConnectionInfo> ConnectionAsync(string baseUrl, ConnectionType type = ConnectionType.Base)
        {
            ConnectionInfo connectionObject = new ConnectionInfo()
            {
            Url = baseUrl,
            ConnectionType = type
            };
            while (true)
            {
                connectionObject.HubConnection = new HubConnectionBuilder()
                    .WithUrl(baseUrl)
                    .WithConsoleLogger()
                    .Build();

                try
                {
                    connectionObject.HubConnection.On<string>("Reply", (Result) =>
                    {
                        ReplyContent result = PublicMethod.JsonDeSerialize<ReplyContent>(Result);
                        switch (result.ReplyMethodName)
                        {
                            case ReplyMethodName.Connection:
                                connectionObject.HubToken = result.Content;
                                break;
                            case ReplyMethodName.Subscribe:
                                break;
                            default:
                                break;
                        }
                    });
                    connectionObject.HubConnection.On<string>("Send", content =>
                    {
                        if (!_isTerminal)
                        {
                            _connectionList.Where(x => x.ConnectionType == ConnectionType.Self).FirstOrDefault().HubConnection.InvokeAsync("Send", content, true);
                        }
                    });
                    connectionObject.HubConnection.Closed += (e) =>
                    {
                        if (connectionObject.ConnectionType == ConnectionType.SubHub)
                        {
                            _connectionList.Remove(connectionObject);

                        }
                        DisposeConnection(connectionObject).GetAwaiter().GetResult();
                        ReConnection(connectionObject).GetAwaiter().GetResult();
                    };
                    if (type == ConnectionType.SubHub)
                    {
                        // connectionObject.HubConnection.Connected += () =>
                        // {
                        //     var task = Task.Run(() =>
                        //     {
                        //         DateTime timer = DateTime.Now;
                        //         while (DateTime.Now.Subtract(timer).Minutes < 3)
                        //         {
                        //             Task.Delay(10000).GetAwaiter().GetResult();
                        //         }
                        //         DisposeConnection(connectionObject).GetAwaiter().GetResult();
                        //     });
                        //     return Task.Run(() => { });
                        // };
                    }

                    await connectionObject.HubConnection.StartAsync();
                    while (String.IsNullOrEmpty(connectionObject.HubToken))
                    {
                        Task.Delay(100).GetAwaiter().GetResult();
                    }
                    return connectionObject;
                }
                catch (Exception)
                {
                    await Task.Delay(5000);
                }
            }
        }

        private async Task DisposeConnection(ConnectionInfo connection)
        {
            await connection.HubConnection.DisposeAsync();
        }

        private async Task ReConnection(ConnectionInfo connection)
        {
            ConnectionInfo newConnection = await ConnectionAsync(connection.Url);
            _connectionList.Remove(connection);
            _connectionList.Add(newConnection);
        }
    }
}