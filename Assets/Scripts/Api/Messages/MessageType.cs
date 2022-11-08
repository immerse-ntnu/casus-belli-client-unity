namespace Immerse.BfhClient.Api.Messages
{
    /// <summary>
    /// <para>
    /// IDs for the types of messages sent between client and server.
    /// Each ID corresponds to a message struct in <see cref="Immerse.BfhClient.Api.Messages"/>,
    /// with the same name but suffixed with "Msg".
    /// </para>
    ///
    /// <para>
    /// Message types are used as keys in the JSON messages to and from the server.
    /// Every message has the following format, where messageID is one of the
    /// <see cref="Immerse.BfhClient.Api.Messages.MessageType"/> constants, and
    /// {...message} is the corresponding "...Msg" struct in <see cref="Immerse.BfhClient.Api.Messages"/>.
    /// <code>
    /// {
    ///     "[messageID]": {...message}
    /// }
    /// </code>
    /// </para>
    /// </summary>
    ///
    /// <example>
    /// <see cref="Immerse.BfhClient.Api.Messages.MessageType.SupportRequest"/> is the message ID for
    /// <see cref="Immerse.BfhClient.Api.Messages.SupportRequestMsg"/>.
    /// The message looks like this when coming from the server:
    /// <code>
    /// {
    ///     "supportRequest": {
    ///         "supportingArea": "Calis",
    ///         "supportablePlayers": ["red", "green"]
    ///     }
    /// }
    /// </code>
    /// </example>
    public static class MessageType
    {
        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.ErrorMsg"/>.
        /// </summary>
        public const string Error = "error";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.PlayerStatusMsg"/>.
        /// </summary>
        public const string PlayerStatus = "playerStatus";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.LobbyJoinedMsg"/>.
        /// </summary>
        public const string LobbyJoined = "lobbyJoined";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.SelectGameIDMsg"/>.
        /// </summary>
        public const string SelectGameID = "selectGameId";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.ReadyMsg"/>.
        /// </summary>
        public const string Ready = "ready";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.StartGameMsg"/>.
        /// </summary>
        public const string StartGame = "startGame";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.SupportRequestMsg"/>.
        /// </summary>
        public const string SupportRequest = "supportRequest";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.OrderRequestMsg"/>.
        /// </summary>
        public const string OrderRequest = "orderRequest";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.OrdersReceivedMsg"/>.
        /// </summary>
        public const string OrdersReceived = "ordersReceived";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.OrdersConfirmationMsg"/>.
        /// </summary>
        public const string OrdersConfirmation = "ordersConfirmation";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.BattleResultsMsg"/>.
        /// </summary>
        public const string BattleResults = "battleResults";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.WinnerMsg"/>.
        /// </summary>
        public const string Winner = "winner";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.SubmitOrdersMsg"/>.
        /// </summary>
        public const string SubmitOrders = "submitOrders";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.GiveSupportMsg"/>.
        /// </summary>
        public const string GiveSupport = "giveSupport";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.WinterVoteMsg"/>.
        /// </summary>
        public const string WinterVote = "winterVote";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.SwordMsg"/>.
        /// </summary>
        public const string Sword = "swordMsg";

        /// <summary>
        /// Message ID for <see cref="Immerse.BfhClient.Api.Messages.RavenMsg"/>.
        /// </summary>
        public const string Raven = "raven";
    }
}
