using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Immerse.BfhClient.Api
{
    /// <summary>
    /// <para>Handles sending messages through the WebSocket connection to the game server.</para>
    /// <para>
    /// Spawns a thread that continuously listens for messages to send on an internal queue.
    /// Provides a <see cref="SendMessage"/> method that places a message into the queue, which is then picked up by
    /// the sending thread to send to the server.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Implementation based on https://www.patrykgalach.com/2019/11/11/implementing-websocket-in-unity/.
    /// </remarks>
    public class MessageSender
    {
        private readonly ClientWebSocket _connection;
        private readonly Thread _sendThread;
        private readonly BlockingCollection<SerializableMessage> _sendQueue;
        private readonly UTF8Encoding _encoding;

        public MessageSender(ClientWebSocket connection)
        {
            _connection = connection;
            _encoding = new UTF8Encoding();
            _sendQueue = new BlockingCollection<SerializableMessage>();
            _sendThread = new Thread(SendMessagesFromQueue);
        }

        /// <summary>
        /// Puts the given message in the <see cref="MessageSender"/>'s send queue to be serialized and passed to the
        /// game server.
        /// </summary>
        public void SendMessage(string messageType, object message)
        {
            _sendQueue.Add(new SerializableMessage(messageType, message));
        }

        /// <summary>
        /// Continuously takes messages from the send queue, serializes them and sends them to the server.
        /// </summary>
        private async void SendMessagesFromQueue()
        {
            while (true)
            {
                if (_connection.State != WebSocketState.Open)
                {
                    Task.Delay(50).Wait();
                    continue;
                }

                while (!_sendQueue.IsCompleted)
                {
                    var message = _sendQueue.Take();

                    var jsonMessage = new JObject(
                        new JProperty(message.MessageType, message.WrappedMessage)
                    ).ToString();

                    var byteMessage = _encoding.GetBytes(jsonMessage);

                    await _connection.SendAsync(
                        byteMessage,
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
        }
    }

    /// <summary>
    /// A message object along with its type, for passing to the <see cref="MessageSender"/>'s send queue.
    /// </summary>
    internal struct SerializableMessage
    {
        public readonly string MessageType;
        public readonly object WrappedMessage;

        public SerializableMessage(string messageType, object wrappedMessage)
        {
            MessageType = messageType;
            WrappedMessage = wrappedMessage;
        }
    }
}
