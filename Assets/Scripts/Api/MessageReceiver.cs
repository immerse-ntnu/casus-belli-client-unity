using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Immerse.BfhClient.Api.Messages;
using Newtonsoft.Json.Linq;
using UnityEngine;

namespace Immerse.BfhClient.Api
{
    /// <summary>
    /// <para>Handles receiving messages from the WebSocket connection to the game server.</para>
    /// <para>
    /// Spawns a thread that continuously listens for received messages on its given WebSocket connection.
    /// Initializes a <see cref="BlockingCollection{T}"/> for each message type that the client expects to receive from
    /// the server.
    /// When a message is received, it is deserialized and put into the appropriate collection according to its type.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Implementation based on https://www.patrykgalach.com/2019/11/11/implementing-websocket-in-unity/.
    /// </remarks>
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

        /// <summary>
        /// Continuously reads incoming messages from the WebSocket connection.
        /// After a message is read to completion, calls <see cref="DeserializeIntoQueue"/> to deserialize and
        /// enqueue the message appropriately.
        /// </summary>
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
                    Debug.Log("Received unexpected non-text message from WebSocket connection");
                    continue;
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
        }

        /// <summary>
        /// Messages received from the server are JSON on the following format:
        /// <code>
        /// {
        ///     "[messageID]": {...message}
        /// }
        /// </code>
        /// This method takes the full message JSON string, deserializes the "wrapping object" to get the message ID,
        /// then calls <see cref="DeserializeAndEnqueue{T}"/> with the wrapped inner message and the appropriate
        /// <see cref="BlockingCollection{T}"/> according to its type.
        /// </summary>
        private void DeserializeIntoQueue(string messageString)
        {
            var messageWithID = JObject.Parse(messageString);

            // The wrapping JSON object is expected to have only a single field, with the message ID as key and the
            // serialized message as its value
            var firstMessageProperty = messageWithID.Properties().First();
            var messageType = firstMessageProperty.Name;
            var serializedMessage = firstMessageProperty.Value;

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

        /// <summary>
        /// Attempts to deserialize the given message to <typeparamref name="TMessage"/>, then adds it to the given
        /// message queue.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the given message could not be deserialized to <typeparamref name="TMessage"/>.
        /// </exception>
        private static void DeserializeAndEnqueue<TMessage>(
            JToken serializedMessage,
            BlockingCollection<TMessage> messageQueue
        ) {
            var message = serializedMessage.ToObject<TMessage>();
            if (message == null)
            {
                throw new ArgumentException($"Failed to deserialize message \"{serializedMessage}\"");
            }

            messageQueue.Add(message);
        }
    }
}
