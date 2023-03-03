using System;
using System.Collections.Concurrent;
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
    /// <para>Handles receiving messages from the WebSocket connection to the game server.</para>
    /// <para>
    /// Spawns a thread that continuously listens for received messages on its given WebSocket connection.
    /// Initializes a <see cref="ConcurrentQueue{T}"/> for each message type that the client expects to receive from
    /// the server.
    /// When a message is received, it is deserialized and put into the appropriate collection according to its type.
    /// </para>
    /// </summary>
    /// <remarks>
    /// Implementation based on https://www.patrykgalach.com/2019/11/11/implementing-websocket-in-unity/.
    /// </remarks>
    internal class MessageReceiver
    {
        private readonly ClientWebSocket _connection;
        private readonly Thread _receiveThread;

        private readonly Dictionary<string, IMessageReceiveQueue> _queuesById = new();
        private readonly Dictionary<Type, IMessageReceiveQueue> _queuesByType = new();

        public MessageReceiver(ClientWebSocket connection)
        {
            _connection = connection;
            _receiveThread = new Thread(ReceiveMessagesIntoQueues);
        }

        /// <summary>
        /// Registers the given message type, with the corresponding message ID, as a message that the client expects to
        /// receive from the server.
        /// </summary>
        public void RegisterReceivableMessage<TMessage>(string messageID)
            where TMessage : IReceivableMessage
        {
            var queue = new MessageReceiveQueue<TMessage>();
            _queuesById.Add(messageID, queue);
            _queuesByType.Add(typeof(TMessage), queue);
        }

        /// <summary>
        /// Checks for received messages of each message type.
        /// If a message has been received, calls the message handlers for that type.
        /// </summary>
        public void TriggerReceivedMessageEvents()
        {
            foreach (var queue in _queuesById.Values)
            {
                queue.TriggerEventIfMessageReceived();
            }
        }

        /// <summary>
        /// Registers the given method to be called whenever the server sends a message of the given type.
        /// </summary>
        public void RegisterMessageHandler<TMessage>(Action<TMessage> messageHandler)
            where TMessage : IReceivableMessage
        {
            var queue = GetReceiveQueueByType<TMessage>();
            queue.ReceivedMessage += messageHandler;
        }

        /// <summary>
        /// Deregisters the given message handler method.
        /// Should be called when a message handler is disposed, to properly remove all references to it.
        /// </summary>
        public void DeregisterMessageHandler<TMessage>(Action<TMessage> messageHandler)
            where TMessage : IReceivableMessage
        {
            var queue = GetReceiveQueueByType<TMessage>();
            queue.ReceivedMessage -= messageHandler;
        }

        /// <summary>
        /// Utility method to get the message queue corresponding to the given type from <see cref="_queuesByType"/>.
        /// </summary>
        /// <exception cref="ArgumentException">If no queue was found for the given type.</exception>
        private MessageReceiveQueue<TMessage> GetReceiveQueueByType<TMessage>()
            where TMessage : IReceivableMessage
        {
            IMessageReceiveQueue queue;
            if (!_queuesByType.TryGetValue(typeof(TMessage), out queue))
            {
                throw new ArgumentException($"Unrecognized message type: '{typeof(TMessage)}'");
            }

            return (MessageReceiveQueue<TMessage>)queue;
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
        /// then calls on the appropriate message queue to further deserialize and enqueue the wrapped message object.
        /// </summary>
        /// <exception cref="ArgumentException">If no message queue was found for the message's ID.</exception>
        private void DeserializeIntoQueue(string messageString)
        {
            var messageWithID = JObject.Parse(messageString);

            // The wrapping JSON object is expected to have only a single field, with the message ID as key and the
            // serialized message as its value
            var firstMessageProperty = messageWithID.Properties().First();
            var messageID = firstMessageProperty.Name;
            var serializedMessage = firstMessageProperty.Value;

            if (_queuesById.TryGetValue(messageID, out var queue))
            {
                queue.DeserializeAndEnqueue(serializedMessage);
            }
            else throw new ArgumentException($"Unrecognized message type received from server: '{messageID}'");
        }
    }
}
