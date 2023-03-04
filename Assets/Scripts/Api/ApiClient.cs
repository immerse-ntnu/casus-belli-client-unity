using System;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Immerse.BfhClient.Api.MessageHandling;
using Immerse.BfhClient.Api.Messages;
using UnityEngine;

namespace Immerse.BfhClient.Api
{
    /// <summary>
    /// WebSocket client that connects to the game server.
    /// Provides methods for sending and receiving messages to and from the server.
    /// </summary>
    public class ApiClient : MonoBehaviour
    {
        /// <summary>
        /// Singleton instance to ensure that the client has only a single WebSocket connection to the server.
        /// </summary>
        public static ApiClient Instance { get; private set; }

        private ClientWebSocket _connection;
        private MessageSender _messageSender;
        private MessageReceiver _messageReceiver;

        /// <summary>
        /// Instantiates the API client singleton when the script is loaded.
        /// Ensures that no other instance overwrites it, and that it is preserved between scene changes.
        /// </summary>
        private void Awake()
        {
            if (Instance is null)
            {
                _connection = new ClientWebSocket();
                _messageSender = new MessageSender(_connection);
                _messageReceiver = new MessageReceiver(_connection);

                RegisterSendableMessages();
                RegisterReceivableMessages();

                Instance = this;
                DontDestroyOnLoad(Instance.gameObject);
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
            }
        }

        /// <summary>
        /// Connects the API client to a server at the given URI, and starts sending and receiving messages.
        /// </summary>
        public async Task Connect(Uri serverUri)
        {
            foreach (var messageQueue in _messageReceiver.MessageQueues)
            {
                StartCoroutine(messageQueue.CheckReceivedMessagesRoutine());
            }

            _messageReceiver.StartReceivingMessages();
            _messageSender.StartSendingMessages();

            await _connection.ConnectAsync(serverUri, CancellationToken.None);
        }

        /// <summary>
        /// Sends the given message to the server.
        /// </summary>
        ///
        /// <typeparam name="TMessage">
        /// Must be registered in <see cref="RegisterSendableMessages"/>, which should be all message types marked with
        /// <see cref="ISendableMessage"/>.
        /// </typeparam>
        public void SendMessage<TMessage>(TMessage message)
            where TMessage : ISendableMessage
        {
            _messageSender.SendQueue.Add(message);
        }

        /// <summary>
        /// Registers the given method to be called whenever the server sends a message of the given type.
        /// </summary>
        ///
        /// <typeparam name="TMessage">
        /// Must be registered in <see cref="RegisterReceivableMessages"/>, which should be all message types marked
        /// with <see cref="IReceivableMessage"/>.
        /// </typeparam>
        public void RegisterMessageHandler<TMessage>(Action<TMessage> messageHandler)
            where TMessage : IReceivableMessage
        {
            var queue = _messageReceiver.GetMessageQueueByType<TMessage>();
            queue.ReceivedMessage += messageHandler;
        }

        /// <summary>
        /// Deregisters the given message handler method.
        /// Should be called when a message handler is disposed, to properly remove all references to it.
        /// </summary>
        ///
        /// <typeparam name="TMessage">
        /// Must be registered in <see cref="RegisterReceivableMessages"/>, which should be all message types marked
        /// with <see cref="IReceivableMessage"/>.
        /// </typeparam>
        public void DeregisterMessageHandler<TMessage>(Action<TMessage> messageHandler)
            where TMessage : IReceivableMessage
        {
            var queue = _messageReceiver.GetMessageQueueByType<TMessage>();
            queue.ReceivedMessage -= messageHandler;
        }

        /// <summary>
        /// Registers all message types that the client expects to be able to send to the server.
        /// </summary>
        private void RegisterSendableMessages()
        {
            _messageSender.RegisterSendableMessage<SelectGameIDMessage>(MessageID.SelectGameID);
            _messageSender.RegisterSendableMessage<ReadyMessage>(MessageID.Ready);
            _messageSender.RegisterSendableMessage<StartGameMessage>(MessageID.StartGame);
            _messageSender.RegisterSendableMessage<SubmitOrdersMessage>(MessageID.SubmitOrders);
            _messageSender.RegisterSendableMessage<GiveSupportMessage>(MessageID.GiveSupport);
            _messageSender.RegisterSendableMessage<WinterVoteMessage>(MessageID.WinterVote);
            _messageSender.RegisterSendableMessage<SwordMessage>(MessageID.Sword);
            _messageSender.RegisterSendableMessage<RavenMessage>(MessageID.Raven);
        }

        /// <summary>
        /// Registers all message types that the client expects to receive from the server.
        /// </summary>
        private void RegisterReceivableMessages()
        {
            _messageReceiver.RegisterReceivableMessage<ErrorMessage>(MessageID.Error);
            _messageReceiver.RegisterReceivableMessage<PlayerStatusMessage>(MessageID.PlayerStatus);
            _messageReceiver.RegisterReceivableMessage<LobbyJoinedMessage>(MessageID.LobbyJoined);
            _messageReceiver.RegisterReceivableMessage<SupportRequestMessage>(MessageID.SupportRequest);
            _messageReceiver.RegisterReceivableMessage<GiveSupportMessage>(MessageID.GiveSupport);
            _messageReceiver.RegisterReceivableMessage<OrderRequestMessage>(MessageID.OrderRequest);
            _messageReceiver.RegisterReceivableMessage<OrdersReceivedMessage>(MessageID.OrdersReceived);
            _messageReceiver.RegisterReceivableMessage<OrdersConfirmationMessage>(MessageID.OrdersConfirmation);
            _messageReceiver.RegisterReceivableMessage<BattleResultsMessage>(MessageID.BattleResults);
            _messageReceiver.RegisterReceivableMessage<WinnerMessage>(MessageID.Winner);
        }
    }
}
