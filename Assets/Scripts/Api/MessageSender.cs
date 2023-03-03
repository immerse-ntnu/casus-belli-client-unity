using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Immerse.BfhClient.Api.Messages;
using Newtonsoft.Json.Linq;

namespace Immerse.BfhClient.Api
{
    /// <summary>
    /// <para>Handles sending messages through the WebSocket connection to the game server.</para>
    /// <para>
    /// Spawns a thread that continuously listens for messages to send on an internal queue.
    /// Provides a <see cref="SendMessage{TMessage}"/> method that places a message into the queue, which is then picked
    /// up by the sending thread to send to the server.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Implementation based on https://www.patrykgalach.com/2019/11/11/implementing-websocket-in-unity/.
    /// </remarks>
    internal class MessageSender
    {
        private readonly ClientWebSocket _connection;
        private readonly Thread _sendThread;
        private readonly BlockingCollection<ISendableMessage> _sendQueue = new();
        private readonly Dictionary<Type, string> _messageIdMap = new();

        public MessageSender(ClientWebSocket connection)
        {
            _connection = connection;
            _sendThread = new Thread(SendMessagesFromQueue);
        }

        /// <summary>
        /// Registers the given message type, with the corresponding message ID, as a message that the client expects to
        /// be able to send to the server.
        /// </summary>
        public void RegisterSendableMessage<TMessage>(string messageId)
            where TMessage : ISendableMessage
        {
            _messageIdMap.Add(typeof(TMessage), messageId);
        }

        /// <summary>
        /// Puts the given message in the <see cref="MessageSender"/>'s send queue to be serialized and passed to the
        /// game server.
        /// </summary>
        public void SendMessage<TMessage>(TMessage message)
            where TMessage : ISendableMessage
        {
            _sendQueue.Add(message);
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
                    var serializedMessage = SerializeToJson(message);

                    await _connection.SendAsync(
                        serializedMessage,
                        WebSocketMessageType.Text,
                        true,
                        CancellationToken.None
                    );
                }
            }
        }

        /// <summary>
        /// Serializes the given message to JSON, wrapping it with the appropriate message ID according to its type.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If a message ID could not be found for the type of the message. Likely because the message type has not been
        /// registered with <see cref="RegisterSendableMessage{TMessage}"/>.
        /// </exception>
        private byte[] SerializeToJson(ISendableMessage message)
        {
            string messageId;
            if (!_messageIdMap.TryGetValue(message.GetType(), out messageId))
            {
                throw new ArgumentException($"Unrecognized type of message object: '{message.GetType()}'");
            }

            var messageJson = new JObject(new JProperty(messageId, message));
            var messageString = messageJson.ToString();
            var messageBytes = Encoding.UTF8.GetBytes(messageString);
            return messageBytes;
        }
    }
}
