using System;
using System.Collections.Generic;
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
    /// Handles receiving messages from the WebSocket connection to the game server.
    /// </summary>
    internal class MessageReceiver
    {
        private readonly ClientWebSocket _connection;
        private Thread _receiveThread;

        private readonly Dictionary<string, IMessageReceiveQueue> _messageQueuesById = new();
        private readonly Dictionary<Type, IMessageReceiveQueue> _messageQueuesByType = new();

        /// <summary>
        /// Queues where messages are placed as they are received from the server, one for each message type.
        /// </summary>
        public IEnumerable<IMessageReceiveQueue> MessageQueues => _messageQueuesById.Values;

        public MessageReceiver(ClientWebSocket connection)
        {
            _connection = connection;
        }

        /// <summary>
        /// Spawns a thread that continuously listens for messages on the WebSocket connection.
        /// </summary>
        public void StartReceivingMessages()
        {
            _receiveThread = new Thread(ReceiveMessagesIntoQueues);
        }

        /// <summary>
        /// Aborts the message receiving thread.
        /// </summary>
        public void StopReceivingMessages()
        {
            _receiveThread.Abort();
            _receiveThread = null;
        }

        /// <summary>
        /// Registers the given message type, with the corresponding message ID, as a message that the client expects to
        /// receive from the server.
        /// </summary>
        public void RegisterReceivableMessage<TMessage>(string messageId)
            where TMessage : IReceivableMessage
        {
            var queue = new MessageReceiveQueue<TMessage>();
            _messageQueuesById.Add(messageId, queue);
            _messageQueuesByType.Add(typeof(TMessage), queue);
        }

        /// <summary>
        /// Gets the message queue corresponding to the given message type.
        /// </summary>
        /// <exception cref="ArgumentException">If no queue was found for the given type.</exception>
        public MessageReceiveQueue<TMessage> GetMessageQueueByType<TMessage>()
            where TMessage : IReceivableMessage
        {
            IMessageReceiveQueue queue;
            if (!_messageQueuesByType.TryGetValue(typeof(TMessage), out queue))
            {
                throw new ArgumentException($"Unrecognized message type: '{typeof(TMessage)}'");
            }

            return (MessageReceiveQueue<TMessage>)queue;
        }

        /// <summary>
        /// Continuously reads incoming messages from the WebSocket connection.
        /// After a message is read to completion, calls <see cref="DeserializeAndEnqueueMessage"/> to deserialize and
        /// enqueue the message appropriately.
        /// </summary>
        /// <remarks>
        /// Implementation based on https://www.patrykgalach.com/2019/11/11/implementing-websocket-in-unity/.
        /// </remarks>
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
                var messageString = await reader.ReadToEndAsync();

                try
                {
                    DeserializeAndEnqueueMessage(messageString);
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
        ///     "[messageId]": {...message}
        /// }
        /// </code>
        /// This method takes the full message JSON string, deserializes the "wrapping object" to get the message ID,
        /// then calls on the appropriate message queue to further deserialize and enqueue the wrapped message object.
        /// </summary>
        /// <exception cref="ArgumentException">If no message queue was found for the message's ID.</exception>
        private void DeserializeAndEnqueueMessage(string messageString)
        {
            var messageWithId = JObject.Parse(messageString);

            // The wrapping JSON object is expected to have only a single field, with the message ID as key and the
            // serialized message as its value
            var firstMessageProperty = messageWithId.Properties().First();
            var messageId = firstMessageProperty.Name;
            var serializedMessage = firstMessageProperty.Value;

            if (_messageQueuesById.TryGetValue(messageId, out var queue))
            {
                queue.DeserializeAndEnqueueMessage(serializedMessage);
            }
            else throw new ArgumentException($"Unrecognized message type received from server: '{messageId}'");
        }
    }
}
