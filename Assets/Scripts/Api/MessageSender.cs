using System;
using System.Collections.Concurrent;
using System.Net.WebSockets;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json.Linq;

namespace Immerse.BfhClient.Api
{
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

        public void SendMessage(string messageType, object message)
        {
            var jsonMessage = new JObject(new JProperty(messageType, message)).ToString();
            var byteMessage = (ArraySegment<byte>)_encoding.GetBytes(jsonMessage);
            _sendQueue.Add(byteMessage);
        }

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
