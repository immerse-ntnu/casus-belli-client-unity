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
    /// <see cref="MessageID"/> constants, and
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
    /// <see cref="MessageID.SupportRequest"/> is the message ID for <see cref="SupportRequestMsg"/>.
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
    public static class MessageID
    {
        /// <summary>
        /// Message ID for <see cref="ErrorMsg"/>.
        /// </summary>
        public const string Error = "error";

        /// <summary>
        /// Message ID for <see cref="PlayerStatusMsg"/>.
        /// </summary>
        public const string PlayerStatus = "playerStatus";

        /// <summary>
        /// Message ID for <see cref="LobbyJoinedMsg"/>.
        /// </summary>
        public const string LobbyJoined = "lobbyJoined";

        /// <summary>
        /// Message ID for <see cref="SelectGameIDMsg"/>.
        /// </summary>
        public const string SelectGameID = "selectGameId";

        /// <summary>
        /// Message ID for <see cref="ReadyMsg"/>.
        /// </summary>
        public const string Ready = "ready";

        /// <summary>
        /// Message ID for <see cref="StartGameMsg"/>.
        /// </summary>
        public const string StartGame = "startGame";

        /// <summary>
        /// Message ID for <see cref="SupportRequestMsg"/>.
        /// </summary>
        public const string SupportRequest = "supportRequest";

        /// <summary>
        /// Message ID for <see cref="OrderRequestMsg"/>.
        /// </summary>
        public const string OrderRequest = "orderRequest";

        /// <summary>
        /// Message ID for <see cref="OrdersReceivedMsg"/>.
        /// </summary>
        public const string OrdersReceived = "ordersReceived";

        /// <summary>
        /// Message ID for <see cref="OrdersConfirmationMsg"/>.
        /// </summary>
        public const string OrdersConfirmation = "ordersConfirmation";

        /// <summary>
        /// Message ID for <see cref="BattleResultsMsg"/>.
        /// </summary>
        public const string BattleResults = "battleResults";

        /// <summary>
        /// Message ID for <see cref="WinnerMsg"/>.
        /// </summary>
        public const string Winner = "winner";

        /// <summary>
        /// Message ID for <see cref="SubmitOrdersMsg"/>.
        /// </summary>
        public const string SubmitOrders = "submitOrders";

        /// <summary>
        /// Message ID for <see cref="GiveSupportMsg"/>.
        /// </summary>
        public const string GiveSupport = "giveSupport";

        /// <summary>
        /// Message ID for <see cref="WinterVoteMsg"/>.
        /// </summary>
        public const string WinterVote = "winterVote";

        /// <summary>
        /// Message ID for <see cref="SwordMsg"/>.
        /// </summary>
        public const string Sword = "swordMsg";

        /// <summary>
        /// Message ID for <see cref="RavenMsg"/>.
        /// </summary>
        public const string Raven = "raven";
    }
}
