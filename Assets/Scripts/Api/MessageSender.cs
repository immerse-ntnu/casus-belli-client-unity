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
        private readonly BlockingCollection<ArraySegment<byte>> _sendQueue;
        private readonly UTF8Encoding _encoding;

        public MessageSender(ClientWebSocket connection)
        {
            _connection = connection;
            _encoding = new UTF8Encoding();
            _sendQueue = new BlockingCollection<ArraySegment<byte>>();
            _sendThread = new Thread(SendMessagesFromQueue);
        }

        /// <summary>
        /// Serializes the given message to JSON, wrapped in the given message type.
        /// </summary>
        public void SendMessage(string messageType, object message)
        {
            var jsonMessage = new JObject(new JProperty(messageType, message)).ToString();
            var byteMessage = (ArraySegment<byte>)_encoding.GetBytes(jsonMessage);
            _sendQueue.Add(byteMessage);
        }

        /// <summary>
        /// Continuously takes messages from the send queue, and sends them to the server.
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
}
