using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Immerse.BfhClient.Api.GameTypes;
using Immerse.BfhClient.Api.Messages;
using Newtonsoft.Json.Linq;
using UnityEngine;

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

        private readonly ConcurrentQueue<ErrorMsg> _errorMessages;
        private readonly ConcurrentQueue<PlayerStatusMsg> _playerStatusMessages;
        private readonly ConcurrentQueue<LobbyJoinedMsg> _lobbyJoinedMessages;
        private readonly ConcurrentQueue<SupportRequestMsg> _supportRequestMessages;
        private readonly ConcurrentQueue<OrderRequestMsg> _orderRequestMessages;
        private readonly ConcurrentQueue<OrdersReceivedMsg> _ordersReceivedMessages;
        private readonly ConcurrentQueue<OrdersConfirmationMsg> _ordersConfirmationMessages;
        private readonly ConcurrentQueue<BattleResultsMsg> _battleResultsMessages;
        private readonly ConcurrentQueue<WinnerMsg> _winnerMessages;

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

            _sendThread = new Thread(SendMessagesFromQueue);
            _receiveThread = new Thread(ReceiveMessagesIntoQueues);
        }

        public async Task Connect()
        {
            await _connection.ConnectAsync(_serverUri, CancellationToken.None);
        }

        private async void SendMessagesFromQueue()
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

        private void SendMessage(string messageType, object message)
        {
            var jsonMessage = new JObject(new JProperty(messageType, message)).ToString();
            var byteMessage = (ArraySegment<byte>) _encoding.GetBytes(jsonMessage);
            _sendQueue.Add(byteMessage);
        }

        public void SendSelectGameIDMessage(string gameID)
        {
            SendMessage(MessageType.SelectGameID, new SelectGameIDMsg(gameID));
        }

        public void SendReadyMessage(bool ready)
        {
            SendMessage(MessageType.Ready, new ReadyMsg(ready));
        }

        public void SendStartGameMessage()
        {
            SendMessage(MessageType.StartGame, new StartGameMsg());
        }

        public void SendSubmitOrdersMessage(List<Order> orders)
        {
            SendMessage(MessageType.SubmitOrders, new SubmitOrdersMsg(orders));
        }

        public void SendGiveSupportMessage(string supportingArea, string supportedPlayer)
        {
            SendMessage(MessageType.GiveSupport, new GiveSupportMsg(supportingArea, supportedPlayer));
        }

        public void SendWinterVoteMessage(string player)
        {
            SendMessage(MessageType.WinterVote, new WinterVoteMsg(player));
        }

        public void SendSwordMessage(string area, int battleIndex)
        {
            SendMessage(MessageType.Sword, new SwordMsg(area, battleIndex));
        }

        public void SendRavenMessage(string player)
        {
            SendMessage(MessageType.Raven, new RavenMsg(player));
        }

        private async void ReceiveMessagesIntoQueues()
        {
            while (_connection.State == WebSocketState.Open)
            {
                var memoryStream = new MemoryStream();
                var isTextMessage = true;

                while (true)
                {
                    var buffer = new ArraySegment<byte>(new byte[4 * 1024]);

                    var chunkResult = await _connection.ReceiveAsync(buffer, CancellationToken.None);
                    if (chunkResult.MessageType == WebSocketMessageType.Text)
                    {
                        isTextMessage = false;
                        break;
                    }

                    memoryStream.Write(buffer.Array!, buffer.Offset, chunkResult.Count);

                    if (chunkResult.EndOfMessage)
                    {
                        break;
                    }
                }

                if (!isTextMessage)
                {
                    break;
                }

                memoryStream.Seek(0, SeekOrigin.Begin);

                using var reader = new StreamReader(memoryStream, Encoding.UTF8);
                var messageString = reader.ReadToEnd();

                try
                {
                    DeserializeIntoQueue(messageString);
                }
                catch (Exception exception)
                {
                    Debug.Log($"Failed to deserialize received message: {exception.Message}");
                }
            }

            Debug.Log("WebSocket connection to server closed.");
        }

        private void DeserializeIntoQueue(string messageString)
        {
            var messageWithMessageType = JObject.Parse(messageString);

            string messageType = null;
            JToken serializedMessage = null;
            foreach (var property in messageWithMessageType.Properties())
            {
                messageType = property.Name;
                serializedMessage = property.Value;
                break;
            }

            if (messageType == null || serializedMessage == null)
            {
                throw new ArgumentException(
                    $"Expected JSON message to be wrapped in object with message type, but got: {messageString}"
                );
            }

            switch (messageType)
            {
                case MessageType.Error:
                    DeserializeAndEnqueue(serializedMessage, _errorMessages);
                    break;
                case MessageType.PlayerStatus:
                    DeserializeAndEnqueue(serializedMessage, _playerStatusMessages);
                    break;
                case MessageType.LobbyJoined:
                    DeserializeAndEnqueue(serializedMessage, _lobbyJoinedMessages);
                    break;
                case MessageType.SupportRequest:
                    DeserializeAndEnqueue(serializedMessage, _supportRequestMessages);
                    break;
                case MessageType.OrderRequest:
                    DeserializeAndEnqueue(serializedMessage, _orderRequestMessages);
                    break;
                case MessageType.OrdersReceived:
                    DeserializeAndEnqueue(serializedMessage, _ordersReceivedMessages);
                    break;
                case MessageType.OrdersConfirmation:
                    DeserializeAndEnqueue(serializedMessage, _ordersConfirmationMessages);
                    break;
                case MessageType.BattleResults:
                    DeserializeAndEnqueue(serializedMessage, _battleResultsMessages);
                    break;
                case MessageType.Winner:
                    DeserializeAndEnqueue(serializedMessage, _winnerMessages);
                    break;
                default:
                    Debug.LogError($"Unrecognized message type received from server: {messageType}");
                    break;
            }
        }

        private static void DeserializeAndEnqueue<TMessage>(JToken serializedMessage, ConcurrentQueue<TMessage> queue)
        {
            var message = serializedMessage.ToObject<TMessage>();
            if (message == null)
            {
                throw new ArgumentException($"Failed to deserialize message \"{serializedMessage}\"");
            }

            queue.Enqueue(message);
        }
    }
}