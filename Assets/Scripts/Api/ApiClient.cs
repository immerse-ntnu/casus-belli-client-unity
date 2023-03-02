using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Immerse.BfhClient.Api.GameTypes;
using Immerse.BfhClient.Api.Messages;
using UnityEngine;

namespace Immerse.BfhClient.Api
{
    /// <summary>
    /// WebSocket client that connects to the game server.
    /// Provides methods for sending messages to the server, and events that are triggered when messages are received
    /// from the server.
    /// Sending and receiving are no-ops until <see cref="ApiClient.Connect"/> is called.
    /// </summary>
    public class ApiClient : MonoBehaviour
    {
        public static ApiClient Instance { get; private set; }

        private readonly ClientWebSocket _connection = new();
        private MessageSender _messageSender;
        private MessageReceiver _messageReceiver;

        /// <summary>
        /// Event that is triggered when server sends an <see cref="Messages.ErrorMessage"/>.
        /// </summary>
        public event Action<ErrorMessage> ReceivedErrorMessage;

        /// <summary>
        /// Event that is triggered when server sends a <see cref="Messages.PlayerStatusMessage"/>.
        /// </summary>
        public event Action<PlayerStatusMessage> ReceivedPlayerStatusMessage;

        /// <summary>
        /// Event that is triggered when server sends a <see cref="Messages.LobbyJoinedMessage"/>.
        /// </summary>
        public event Action<LobbyJoinedMessage> ReceivedLobbyJoinedMessage;

        /// <summary>
        /// Event that is triggered when server sends a <see cref="Messages.SupportRequestMessage"/>.
        /// </summary>
        public event Action<SupportRequestMessage> ReceivedSupportRequestMessage;

        /// <summary>
        /// Event that is triggered when server sends an <see cref="Messages.OrderRequestMessage"/>.
        /// </summary>
        public event Action<OrderRequestMessage> ReceivedOrderRequestMessage;

        /// <summary>
        /// Event that is triggered when server sends an <see cref="Messages.OrdersReceivedMessage"/>.
        /// </summary>
        public event Action<OrdersReceivedMessage> ReceivedOrdersReceivedMessage;

        /// <summary>
        /// Event that is triggered when server sends an <see cref="Messages.OrdersConfirmationMessage"/>.
        /// </summary>
        public event Action<OrdersConfirmationMessage> ReceivedOrdersConfirmationMessage;

        /// <summary>
        /// Event that is triggered when server sends a <see cref="Messages.BattleResultsMessage"/>.
        /// </summary>
        public event Action<BattleResultsMessage> ReceivedBattleResultsMessage;

        /// <summary>
        /// Event that is triggered when server sends a <see cref="Messages.WinnerMessage"/>.
        /// </summary>
        public event Action<WinnerMessage> ReceivedWinnerMessage;

        /// <summary>
        /// Connects the API client to a server at the given URI.
        /// Must be called before any of the send or receive methods.
        /// </summary>
        public async Task Connect(Uri serverUri)
        {
            _messageSender = new MessageSender(_connection);
            _messageReceiver = new MessageReceiver(_connection);
            await _connection.ConnectAsync(serverUri, CancellationToken.None);
        }

        /// <summary>
        /// Sends a <see cref="SelectGameIDMessage"/> to the server.
        /// </summary>
        public void SendSelectGameIDMessage(string gameID)
        {
            _messageSender?.SendMessage(MessageID.SelectGameID, new SelectGameIDMessage(gameID));
        }

        /// <summary>
        /// Sends a <see cref="ReadyMessage"/> to the server.
        /// </summary>
        public void SendReadyMessage(bool ready)
        {
            _messageSender?.SendMessage(MessageID.Ready, new ReadyMessage(ready));
        }

        /// <summary>
        /// Sends a <see cref="StartGameMessage"/> to the server.
        /// </summary>
        public void SendStartGameMessage()
        {
            _messageSender?.SendMessage(MessageID.StartGame, new StartGameMessage());
        }

        /// <summary>
        /// Sends a <see cref="SubmitOrdersMessage"/> to the server.
        /// </summary>
        public void SendSubmitOrdersMessage(List<Order> orders)
        {
            _messageSender?.SendMessage(MessageID.SubmitOrders, new SubmitOrdersMessage(orders));
        }

        /// <summary>
        /// Sends a <see cref="GiveSupportMessage"/> to the server.
        /// </summary>
        public void SendGiveSupportMessage(string supportingArea, string supportedPlayer)
        {
            _messageSender?.SendMessage(MessageID.GiveSupport, new GiveSupportMessage(supportingArea, supportedPlayer));
        }

        /// <summary>
        /// Sends a <see cref="WinterVoteMessage"/> to the server.
        /// </summary>
        public void SendWinterVoteMessage(string player)
        {
            _messageSender?.SendMessage(MessageID.WinterVote, new WinterVoteMessage(player));
        }

        /// <summary>
        /// Sends a <see cref="SwordMessage"/> to the server.
        /// </summary>
        public void SendSwordMessage(string area, int battleIndex)
        {
            _messageSender?.SendMessage(MessageID.Sword, new SwordMessage(area, battleIndex));
        }

        /// <summary>
        /// Sends a <see cref="RavenMessage"/> to the server.
        /// </summary>
        public void SendRavenMessage(string player)
        {
            _messageSender?.SendMessage(MessageID.Raven, new RavenMessage(player));
        }

        private void Awake()
        {
            Instance = this;
        }

        private void Update()
        {
            TriggerEventIfMessageReceived(ReceivedErrorMessage, _messageReceiver.ErrorMessages);
            TriggerEventIfMessageReceived(ReceivedPlayerStatusMessage, _messageReceiver.PlayerStatusMessages);
            TriggerEventIfMessageReceived(ReceivedLobbyJoinedMessage, _messageReceiver.LobbyJoinedMessages);
            TriggerEventIfMessageReceived(ReceivedSupportRequestMessage, _messageReceiver.SupportRequestMessages);
            TriggerEventIfMessageReceived(ReceivedOrderRequestMessage, _messageReceiver.OrderRequestMessages);
            TriggerEventIfMessageReceived(ReceivedOrdersReceivedMessage, _messageReceiver.OrdersReceivedMessages);
            TriggerEventIfMessageReceived(ReceivedOrdersConfirmationMessage, _messageReceiver.OrdersConfirmationMessages);
            TriggerEventIfMessageReceived(ReceivedBattleResultsMessage, _messageReceiver.BattleResultsMessages);
            TriggerEventIfMessageReceived(ReceivedWinnerMessage, _messageReceiver.WinnerMessages);
        }

        private static void TriggerEventIfMessageReceived<TMessage>(
            Action<TMessage> receivedMessageEvent, ConcurrentQueue<TMessage> messageQueue
        ) {
            if (messageQueue.TryDequeue(out var message))
            {
                receivedMessageEvent?.Invoke(message);
            }
        }
    }
}
