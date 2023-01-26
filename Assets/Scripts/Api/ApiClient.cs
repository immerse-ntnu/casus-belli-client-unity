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
    /// Based on https://www.patrykgalach.com/2019/11/11/implementing-websocket-in-unity/.
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

        public async Task Connect()
        {
            await _connection.ConnectAsync(_serverUri, CancellationToken.None);
        }

        public void SendSelectGameIDMessage(string gameID)
        {
            _messageSender.SendMessage(MessageType.SelectGameID, new SelectGameIDMsg(gameID));
        }

        public void SendReadyMessage(bool ready)
        {
            _messageSender.SendMessage(MessageType.Ready, new ReadyMsg(ready));
        }

        public void SendStartGameMessage()
        {
            _messageSender.SendMessage(MessageType.StartGame, new StartGameMsg());
        }

        public void SendSubmitOrdersMessage(List<Order> orders)
        {
            _messageSender.SendMessage(MessageType.SubmitOrders, new SubmitOrdersMsg(orders));
        }

        public void SendGiveSupportMessage(string supportingArea, string supportedPlayer)
        {
            _messageSender.SendMessage(MessageType.GiveSupport, new GiveSupportMsg(supportingArea, supportedPlayer));
        }

        public void SendWinterVoteMessage(string player)
        {
            _messageSender.SendMessage(MessageType.WinterVote, new WinterVoteMsg(player));
        }

        public void SendSwordMessage(string area, int battleIndex)
        {
            _messageSender.SendMessage(MessageType.Sword, new SwordMsg(area, battleIndex));
        }

        public void SendRavenMessage(string player)
        {
            _messageSender.SendMessage(MessageType.Raven, new RavenMsg(player));
        }

        public ErrorMsg AwaitErrorMessage()
        {
            return _messageReceiver.ErrorMessages.Take();
        }

        public PlayerStatusMsg AwaitPlayerStatusMessage()
        {
            return _messageReceiver.PlayerStatusMessages.Take();
        }

        public LobbyJoinedMsg AwaitLobbyJoinedMessage()
        {
            return _messageReceiver.LobbyJoinedMessages.Take();
        }

        public SupportRequestMsg AwaitSupportRequestMessage()
        {
            return _messageReceiver.SupportRequestMessages.Take();
        }

        public OrderRequestMsg AwaitOrderRequestMessage()
        {
            return _messageReceiver.OrderRequestMessages.Take();
        }

        public OrdersReceivedMsg AwaitOrdersReceivedMessage()
        {
            return _messageReceiver.OrdersReceivedMessages.Take();
        }

        public OrdersConfirmationMsg AwaitOrdersConfirmationMessage()
        {
            return _messageReceiver.OrdersConfirmationMessages.Take();
        }

        public BattleResultsMsg AwaitBattleResultsMessage()
        {
            return _messageReceiver.BattleResultsMessages.Take();
        }

        public WinnerMsg AwaitWinnerMessage()
        {
            return _messageReceiver.WinnerMessages.Take();
        }
    }
}
