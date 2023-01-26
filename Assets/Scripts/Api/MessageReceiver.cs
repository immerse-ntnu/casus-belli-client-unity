using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Immerse.BfhClient.Api.Messages;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Immerse.BfhClient.Api
{
    public class MessageReceiver
    {
        private readonly ClientWebSocket _connection;
        private readonly Thread _receiveThread;

        public readonly BlockingCollection<ErrorMsg> ErrorMessages;
        public readonly BlockingCollection<PlayerStatusMsg> PlayerStatusMessages;
        public readonly BlockingCollection<LobbyJoinedMsg> LobbyJoinedMessages;
        public readonly BlockingCollection<SupportRequestMsg> SupportRequestMessages;
        public readonly BlockingCollection<OrderRequestMsg> OrderRequestMessages;
        public readonly BlockingCollection<OrdersReceivedMsg> OrdersReceivedMessages;
        public readonly BlockingCollection<OrdersConfirmationMsg> OrdersConfirmationMessages;
        public readonly BlockingCollection<BattleResultsMsg> BattleResultsMessages;
        public readonly BlockingCollection<WinnerMsg> WinnerMessages;

        public MessageReceiver(ClientWebSocket connection)
        {
            _connection = connection;

            ErrorMessages = new BlockingCollection<ErrorMsg>();
            PlayerStatusMessages = new BlockingCollection<PlayerStatusMsg>();
            LobbyJoinedMessages = new BlockingCollection<LobbyJoinedMsg>();
            SupportRequestMessages = new BlockingCollection<SupportRequestMsg>();
            OrderRequestMessages = new BlockingCollection<OrderRequestMsg>();
            OrdersReceivedMessages = new BlockingCollection<OrdersReceivedMsg>();
            OrdersConfirmationMessages = new BlockingCollection<OrdersConfirmationMsg>();
            BattleResultsMessages = new BlockingCollection<BattleResultsMsg>();
            WinnerMessages = new BlockingCollection<WinnerMsg>();

            _receiveThread = new Thread(ReceiveMessagesIntoQueues);
        }

        private async void ReceiveMessagesIntoQueues()
        {
            while (true)
            {
                if (_connection.State != WebSocketState.Open)
                {
                    Task.Delay(50).Wait();
                    continue;
                }

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
                    DeserializeAndEnqueue(serializedMessage, ErrorMessages);
                    break;
                case MessageType.PlayerStatus:
                    DeserializeAndEnqueue(serializedMessage, PlayerStatusMessages);
                    break;
                case MessageType.LobbyJoined:
                    DeserializeAndEnqueue(serializedMessage, LobbyJoinedMessages);
                    break;
                case MessageType.SupportRequest:
                    DeserializeAndEnqueue(serializedMessage, SupportRequestMessages);
                    break;
                case MessageType.OrderRequest:
                    DeserializeAndEnqueue(serializedMessage, OrderRequestMessages);
                    break;
                case MessageType.OrdersReceived:
                    DeserializeAndEnqueue(serializedMessage, OrdersReceivedMessages);
                    break;
                case MessageType.OrdersConfirmation:
                    DeserializeAndEnqueue(serializedMessage, OrdersConfirmationMessages);
                    break;
                case MessageType.BattleResults:
                    DeserializeAndEnqueue(serializedMessage, BattleResultsMessages);
                    break;
                case MessageType.Winner:
                    DeserializeAndEnqueue(serializedMessage, WinnerMessages);
                    break;
                default:
                    Debug.LogError($"Unrecognized message type received from server: {messageType}");
                    break;
            }
        }

        private static void DeserializeAndEnqueue<TMessage>(
            JToken serializedMessage,
            BlockingCollection<TMessage> queue
        ) {
            var message = serializedMessage.ToObject<TMessage>();
            if (message == null)
            {
                throw new ArgumentException($"Failed to deserialize message \"{serializedMessage}\"");
            }

            queue.Add(message);
        }
    }
}
