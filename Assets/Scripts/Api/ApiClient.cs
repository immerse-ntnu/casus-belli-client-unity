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
    /// </summary>
    /// <remarks>
    /// Implementation based on https://www.patrykgalach.com/2019/11/11/implementing-websocket-in-unity/.
    /// </remarks>
    public class ApiClient
    {
        private readonly Uri _serverUri;
        private readonly ClientWebSocket _connection;

        private readonly MessageSender _messageSender;
        private readonly MessageReceiver _messageReceiver;

        public ApiClient(string serverUri)
        {
            _serverUri = new Uri(serverUri);
            _connection = new ClientWebSocket();
            _messageSender = new MessageSender(_connection);
            _messageReceiver = new MessageReceiver(_connection);
        }

        /// <summary>
        /// Connects the API client to the server at its URI.
        /// Must be called before any of the send or receive methods.
        /// </summary>
        public async Task Connect()
        {
            await _connection.ConnectAsync(_serverUri, CancellationToken.None);
        }

        /// <summary>
        /// Sends a <see cref="Immerse.BfhClient.Api.Messages.SelectGameIDMsg"/> to the server.
        /// </summary>
        public void SendSelectGameIDMessage(string gameID)
        {
            _messageSender.SendMessage(MessageID.SelectGameID, new SelectGameIDMsg(gameID));
        }

        /// <summary>
        /// Sends a <see cref="Immerse.BfhClient.Api.Messages.ReadyMsg"/> to the server.
        /// </summary>
        public void SendReadyMessage(bool ready)
        {
            _messageSender.SendMessage(MessageID.Ready, new ReadyMsg(ready));
        }

        /// <summary>
        /// Sends a <see cref="Immerse.BfhClient.Api.Messages.StartGameMsg"/> to the server.
        /// </summary>
        public void SendStartGameMessage()
        {
            _messageSender.SendMessage(MessageID.StartGame, new StartGameMsg());
        }

        /// <summary>
        /// Sends a <see cref="Immerse.BfhClient.Api.Messages.SubmitOrdersMsg"/> to the server.
        /// </summary>
        public void SendSubmitOrdersMessage(List<Order> orders)
        {
            _messageSender.SendMessage(MessageID.SubmitOrders, new SubmitOrdersMsg(orders));
        }

        /// <summary>
        /// Sends a <see cref="Immerse.BfhClient.Api.Messages.GiveSupportMsg"/> to the server.
        /// </summary>
        public void SendGiveSupportMessage(string supportingArea, string supportedPlayer)
        {
            _messageSender.SendMessage(MessageID.GiveSupport, new GiveSupportMsg(supportingArea, supportedPlayer));
        }

        /// <summary>
        /// Sends a <see cref="Immerse.BfhClient.Api.Messages.WinterVoteMsg"/> to the server.
        /// </summary>
        public void SendWinterVoteMessage(string player)
        {
            _messageSender.SendMessage(MessageID.WinterVote, new WinterVoteMsg(player));
        }

        /// <summary>
        /// Sends a <see cref="Immerse.BfhClient.Api.Messages.SwordMsg"/> to the server.
        /// </summary>
        public void SendSwordMessage(string area, int battleIndex)
        {
            _messageSender.SendMessage(MessageID.Sword, new SwordMsg(area, battleIndex));
        }

        /// <summary>
        /// Sends a <see cref="Immerse.BfhClient.Api.Messages.RavenMsg"/> to the server.
        /// </summary>
        public void SendRavenMessage(string player)
        {
            _messageSender.SendMessage(MessageID.Raven, new RavenMsg(player));
        }

        /// <summary>
        /// Waits for the server to send an <see cref="Immerse.BfhClient.Api.Messages.ErrorMsg"/>.
        /// </summary>
        public ErrorMsg AwaitErrorMessage()
        {
            return _messageReceiver.ErrorMessages.Take();
        }

        /// <summary>
        /// Waits for the server to send a <see cref="Immerse.BfhClient.Api.Messages.PlayerStatusMsg"/>.
        /// </summary>
        public PlayerStatusMsg AwaitPlayerStatusMessage()
        {
            return _messageReceiver.PlayerStatusMessages.Take();
        }

        /// <summary>
        /// Waits for the server to send a <see cref="Immerse.BfhClient.Api.Messages.LobbyJoinedMsg"/>.
        /// </summary>
        public LobbyJoinedMsg AwaitLobbyJoinedMessage()
        {
            return _messageReceiver.LobbyJoinedMessages.Take();
        }

        /// <summary>
        /// Waits for the server to send a <see cref="Immerse.BfhClient.Api.Messages.SupportRequestMsg"/>.
        /// </summary>
        public SupportRequestMsg AwaitSupportRequestMessage()
        {
            return _messageReceiver.SupportRequestMessages.Take();
        }

        /// <summary>
        /// Waits for the server to send an <see cref="Immerse.BfhClient.Api.Messages.OrderRequestMsg"/>.
        /// </summary>
        public OrderRequestMsg AwaitOrderRequestMessage()
        {
            return _messageReceiver.OrderRequestMessages.Take();
        }

        /// <summary>
        /// Waits for the server to send an <see cref="Immerse.BfhClient.Api.Messages.OrdersReceivedMsg"/>.
        /// </summary>
        public OrdersReceivedMsg AwaitOrdersReceivedMessage()
        {
            return _messageReceiver.OrdersReceivedMessages.Take();
        }

        /// <summary>
        /// Waits for the server to send an <see cref="Immerse.BfhClient.Api.Messages.OrdersConfirmationMsg"/>.
        /// </summary>
        public OrdersConfirmationMsg AwaitOrdersConfirmationMessage()
        {
            return _messageReceiver.OrdersConfirmationMessages.Take();
        }

        /// <summary>
        /// Waits for the server to send a <see cref="Immerse.BfhClient.Api.Messages.BattleResultsMsg"/>.
        /// </summary>
        public BattleResultsMsg AwaitBattleResultsMessage()
        {
            return _messageReceiver.BattleResultsMessages.Take();
        }

        /// <summary>
        /// Waits for the server to send a <see cref="Immerse.BfhClient.Api.Messages.WinnerMsg"/>.
        /// </summary>
        public WinnerMsg AwaitWinnerMessage()
        {
            return _messageReceiver.WinnerMessages.Take();
        }
    }
}
