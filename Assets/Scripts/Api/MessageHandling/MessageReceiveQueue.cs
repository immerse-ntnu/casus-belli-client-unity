using System;
using System.Collections.Concurrent;
using Immerse.BfhClient.Api.Messages;
using Newtonsoft.Json.Linq;

namespace Immerse.BfhClient.Api.MessageHandling
{
    /// <summary>
    /// Utility interface to enable keeping <see cref="MessageReceiveQueue{TMessage}"/>s of different types in a
    /// collection.
    /// </summary>
    internal interface IMessageReceiveQueue
    {
        public void TriggerEventIfMessageReceived();
        public void DeserializeAndEnqueue(JToken serializedMessage);
    }

    /// <summary>
    /// Provides a thread-safe queue for messages received from the server, and an event that is triggered when a
    /// message is received.
    /// </summary>
    internal class MessageReceiveQueue<TMessage> : IMessageReceiveQueue
        where TMessage : IReceivableMessage
    {
        public event Action<TMessage> ReceivedMessage;

        private readonly ConcurrentQueue<TMessage> _queue = new();

        /// <summary>
        /// Checks if a message has been received on the queue. If it has, calls all event subscribers with the message.
        /// </summary>
        public void TriggerEventIfMessageReceived()
        {
            if (_queue.TryDequeue(out var message))
            {
                ReceivedMessage?.Invoke(message);
            }
        }

        /// <summary>
        /// Attempts to deserialize the given message to the message type held by this queue.
        /// If deserialization succeeds, adds the message to the queue.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the given message could not be deserialized to message type of the queue.
        /// </exception>
        public void DeserializeAndEnqueue(JToken serializedMessage) {
            var message = serializedMessage.ToObject<TMessage>();
            if (message == null)
            {
                throw new ArgumentException($"Failed to deserialize message \"{serializedMessage}\"");
            }

            _queue.Enqueue(message);
        }
    }
}
