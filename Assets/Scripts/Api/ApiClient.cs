using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Immerse.BfhClient.Api.GameTypes;
using Immerse.BfhClient.Api.Messages;
using Newtonsoft.Json.Linq;

namespace Immerse.BfhClient.Api
{
    /// <summary>
    /// WebSocket client that connects to the game server.
    /// Provides methods for sending and receiving game and lobby messages.
    /// </summary>
    /// <remarks>
    /// Based on https://www.patrykgalach.com/2019/11/11/implementing-websocket-in-unity/.
    /// </remarks>
    public class ApiClient
    {
        private readonly Uri _serverUri;
        private readonly ClientWebSocket _connection;
        private readonly UTF8Encoding _encoding;

        private Thread _sendThread;
        private Thread _receiveThread;

        private readonly BlockingCollection<ArraySegment<byte>> _sendQueue;

        private ConcurrentQueue<ErrorMsg> _errorMessages;
        private ConcurrentQueue<PlayerStatusMsg> _playerStatusMessages;
        private ConcurrentQueue<LobbyJoinedMsg> _lobbyJoinedMessages;
        private ConcurrentQueue<SupportRequestMsg> _supportRequestMessages;
        private ConcurrentQueue<OrderRequestMsg> _orderRequestMessages;
        private ConcurrentQueue<OrdersReceivedMsg> _ordersReceivedMessages;
        private ConcurrentQueue<OrdersConfirmationMsg> _ordersConfirmationMessages;
        private ConcurrentQueue<BattleResultsMsg> _battleResultsMessages;
        private ConcurrentQueue<WinnerMsg> _winnerMessages;

        public ApiClient(string serverUri)
        {
            _serverUri = new Uri(serverUri);
            _connection = new ClientWebSocket();
            _encoding = new UTF8Encoding();

            _sendQueue = new BlockingCollection<ArraySegment<byte>>();

            _errorMessages = new ConcurrentQueue<ErrorMsg>();
            _playerStatusMessages = new ConcurrentQueue<PlayerStatusMsg>();
            _lobbyJoinedMessages = new ConcurrentQueue<LobbyJoinedMsg>();
            _supportRequestMessages = new ConcurrentQueue<SupportRequestMsg>();
            _orderRequestMessages = new ConcurrentQueue<OrderRequestMsg>();
            _ordersReceivedMessages = new ConcurrentQueue<OrdersReceivedMsg>();
            _ordersConfirmationMessages = new ConcurrentQueue<OrdersConfirmationMsg>();
            _battleResultsMessages = new ConcurrentQueue<BattleResultsMsg>();
            _winnerMessages = new ConcurrentQueue<WinnerMsg>();

            _sendThread = new Thread(SendFromQueue);
        }

        public async Task Connect()
        {
            await _connection.ConnectAsync(_serverUri, CancellationToken.None);
        }

        private async void SendFromQueue()
        {
            while (true)
            {
                while (!_sendQueue.IsCompleted)
                {
                    var message = _sendQueue.Take();

                    await _connection.SendAsync(
                        message,
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
        }

        private void Send(string messageType, object message)
        {
            var jsonMessage = new JObject(new JProperty(messageType, message)).ToString();
            var byteMessage = (ArraySegment<byte>) _encoding.GetBytes(jsonMessage);
            _sendQueue.Add(byteMessage);
        }

        public void SendSelectGameIDMessage(string gameID)
        {
            Send(MessageType.SelectGameID, new SelectGameIDMsg(gameID));
        }

        public void SendReadyMessage(bool ready)
        {
            Send(MessageType.Ready, new ReadyMsg(ready));
        }

        public void SendStartGameMessage()
        {
            Send(MessageType.StartGame, new StartGameMsg());
        }

        public void SendSubmitOrdersMessage(List<Order> orders)
        {
            Send(MessageType.SubmitOrders, new SubmitOrdersMsg(orders));
        }

        public void SendGiveSupportMessage(string supportingArea, string supportedPlayer)
        {
            Send(MessageType.GiveSupport, new GiveSupportMsg(supportingArea, supportedPlayer));
        }

        public void SendWinterVoteMessage(string player)
        {
            Send(MessageType.WinterVote, new WinterVoteMsg(player));
        }

        public void SendSwordMessage(string area, int battleIndex)
        {
            Send(MessageType.Sword, new SwordMsg(area, battleIndex));
        }

        public void SendRavenMessage(string player)
        {
            Send(MessageType.Raven, new RavenMsg(player));
        }
    }
}