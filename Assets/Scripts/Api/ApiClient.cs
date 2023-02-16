using System;
using System.Collections.Generic;
using System.Net.WebSockets;
using System.Threading;
using System.Threading.Tasks;
using Immerse.BfhClient.Api.GameTypes;
using Immerse.BfhClient.Api.Messages;

namespace Immerse.BfhClient.Api
{
    /// <summary>
    /// WebSocket client that connects to the game server.
    /// Provides methods for sending and receiving game and lobby messages.
    /// Sending and receiving are no-ops until <see cref="ApiClient.Connect"/> is called.
    /// </summary>
    public class ApiClient
    {
        private readonly ClientWebSocket _connection;

        private MessageSender _messageSender;
        private MessageReceiver _messageReceiver;

        public ApiClient()
        {
            _connection = new ClientWebSocket();
        }

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

        /// <summary>
        /// Checks if the server has sent an <see cref="Messages.ErrorMessage"/>.
        /// If it has, returns true and puts the message in the given out parameter.
        /// Otherwise returns false.
        /// </summary>
        public bool TryReceiveErrorMessage(out ErrorMessage message)
        {
            // ReSharper disable once InvertIf
            if (_messageReceiver is null)
            {
                message = default;
                return false;
            }

            return _messageReceiver.ErrorMessages.TryDequeue(out message);
        }

        /// <summary>
        /// Checks if the server has sent a <see cref="PlayerStatusMessage"/>.
        /// If it has, returns true and puts the message in the given out parameter.
        /// Otherwise returns false.
        /// </summary>
        public bool TryReceivePlayerStatusMessage(out PlayerStatusMessage message)
        {
            // ReSharper disable once InvertIf
            if (_messageReceiver is null)
            {
                message = default;
                return false;
            }

            return _messageReceiver.PlayerStatusMessages.TryDequeue(out message);
        }

        /// <summary>
        /// Checks if the server has sent a <see cref="LobbyJoinedMessage"/>.
        /// If it has, returns true and puts the message in the given out parameter.
        /// Otherwise returns false.
        /// </summary>
        public bool TryReceiveLobbyJoinedMessage(out LobbyJoinedMessage message)
        {
            // ReSharper disable once InvertIf
            if (_messageReceiver is null)
            {
                message = default;
                return false;
            }

            return _messageReceiver.LobbyJoinedMessages.TryDequeue(out message);
        }

        /// <summary>
        /// Checks if the server has sent a <see cref="SupportRequestMessage"/>.
        /// If it has, returns true and puts the message in the given out parameter.
        /// Otherwise returns false.
        /// </summary>
        public bool TryReceiveSupportRequestMessage(out SupportRequestMessage message)
        {
            // ReSharper disable once InvertIf
            if (_messageReceiver is null)
            {
                message = default;
                return false;
            }

            return _messageReceiver.SupportRequestMessages.TryDequeue(out message);
        }

        /// <summary>
        /// Checks if the server has sent an <see cref="OrderRequestMessage"/>.
        /// If it has, returns true and puts the message in the given out parameter.
        /// Otherwise returns false.
        /// </summary>
        public bool TryReceiveOrderRequestMessage(out OrderRequestMessage message)
        {
            // ReSharper disable once InvertIf
            if (_messageReceiver is null)
            {
                message = default;
                return false;
            }

            return _messageReceiver.OrderRequestMessages.TryDequeue(out message);
        }

        /// <summary>
        /// Checks if the server has sent an <see cref="OrdersReceivedMessage"/>.
        /// If it has, returns true and puts the message in the given out parameter.
        /// Otherwise returns false.
        /// </summary>
        public bool TryReceiveOrdersReceivedMessage(out OrdersReceivedMessage message)
        {
            // ReSharper disable once InvertIf
            if (_messageReceiver is null)
            {
                message = default;
                return false;
            }

            return _messageReceiver.OrdersReceivedMessages.TryDequeue(out message);
        }

        /// <summary>
        /// Checks if the server has sent an <see cref="OrdersConfirmationMessage"/>.
        /// If it has, returns true and puts the message in the given out parameter.
        /// Otherwise returns false.
        /// </summary>
        public bool TryReceiveOrdersConfirmationMessage(out OrdersConfirmationMessage message)
        {
            // ReSharper disable once InvertIf
            if (_messageReceiver is null)
            {
                message = default;
                return false;
            }

            return _messageReceiver.OrdersConfirmationMessages.TryDequeue(out message);
        }

        /// <summary>
        /// Checks if the server has sent a <see cref="BattleResultsMessage"/>.
        /// If it has, returns true and puts the message in the given out parameter.
        /// Otherwise returns false.
        /// </summary>
        public bool TryReceiveBattleResultsMessage(out BattleResultsMessage message)
        {
            // ReSharper disable once InvertIf
            if (_messageReceiver is null)
            {
                message = default;
                return false;
            }

            return _messageReceiver.BattleResultsMessages.TryDequeue(out message);
        }

        /// <summary>
        /// Checks if the server has sent a <see cref="WinnerMessage"/>.
        /// If it has, returns true and puts the message in the given out parameter.
        /// Otherwise returns false.
        /// </summary>
        public bool TryReceiveWinnerMessage(out WinnerMessage message)
        {
            // ReSharper disable once InvertIf
            if (_messageReceiver is null)
            {
                message = default;
                return false;
            }

            return _messageReceiver.WinnerMessages.TryDequeue(out message);
        }
    }
}
