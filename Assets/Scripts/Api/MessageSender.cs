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
    internal class MessageSender
    {
        private readonly ClientWebSocket _connection;
        private readonly Thread _sendThread;
        private readonly BlockingCollection<SerializableMessage> _sendQueue;

        public MessageSender(ClientWebSocket connection)
        {
            _connection = connection;
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
                    var message = _sendQueue.Take().SerializeToJson();

                    await _connection.SendAsync(
                        message,
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
        }
    }

    /// <summary>
    /// A message object along with its ID, for passing to the <see cref="MessageSender"/>'s send queue.
    /// </summary>
    internal readonly struct SerializableMessage
    {
        private readonly string _messageID;
        private readonly object _wrappedMessage;

        public SerializableMessage(string messageID, object wrappedMessage)
        {
            _messageID = messageID;
            _wrappedMessage = wrappedMessage;
        }

        /// <summary>
        /// <para>
        /// Serializes the message to JSON, and returns the JSON object in UTF8-encoded byte format.
        /// </para>
        ///
        /// <para>
        /// Wraps the message object in an outer object with its message ID (as expected by the server), like this:
        /// <code>
        /// {
        ///     "[messageID]": {...message}
        /// }
        /// </code>
        /// </para>
        /// </summary>
        public byte[] SerializeToJson()
        {
            var messageJson = new JObject(new JProperty(_messageID, _wrappedMessage));
            var messageString = messageJson.ToString();
            var messageBytes = Encoding.UTF8.GetBytes(messageString);
            return messageBytes;
        }
    }
}
