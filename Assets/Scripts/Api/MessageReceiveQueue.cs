using System;
using System.Collections;
using System.Collections.Concurrent;
using Immerse.BfhClient.Api.Messages;
using Newtonsoft.Json.Linq;

namespace Immerse.BfhClient.Api
{
    /// <summary>
    /// Utility interface to enable keeping <see cref="MessageReceiveQueue{TMessage}"/>s of different types in a
    /// collection.
    /// </summary>
    public interface IMessageReceiveQueue
    {
        public IEnumerator CheckReceivedMessagesRoutine();
        public void DeserializeAndEnqueueMessage(JToken serializedMessage);
    }

    /// <summary>
    /// Provides a thread-safe queue for messages received from the server, and an event that is triggered when a
    /// message is received.
    /// </summary>
    public class MessageReceiveQueue<TMessage> : IMessageReceiveQueue
        where TMessage : IReceivableMessage
    {
        public event Action<TMessage> ReceivedMessage;

        private readonly ConcurrentQueue<TMessage> _queue = new();

        /// <summary>
        /// Continuously checks for received messages on the queue.
        /// When a message is received, calls all subscribers to <see cref="ReceivedMessage"/> with the message.
        /// Yields back control after each check, so that it can be used as a Unity Coroutine.
        /// </summary>
        public IEnumerator CheckReceivedMessagesRoutine()
        {
            while (true)
            {
                if (_queue.TryDequeue(out var message))
                {
                    ReceivedMessage?.Invoke(message);
                }

                yield return null;
            }
        }

        /// <summary>
        /// Attempts to deserialize the given message to the message type held by this queue.
        /// If deserialization succeeds, adds the message to the queue.
        /// </summary>
        /// <exception cref="ArgumentException">
        /// If the given message could not be deserialized to message type of the queue.
        /// </exception>
        public void DeserializeAndEnqueueMessage(JToken serializedMessage) {
            var message = serializedMessage.ToObject<TMessage>();
            if (message == null)
            {
                throw new ArgumentException($"Failed to deserialize message \"{serializedMessage}\"");
            }

            _queue.Enqueue(message);
        }
    }
}
