using System.Collections.Generic;
using JetBrains.Annotations;

namespace Immerse.BfhClient.Api.Messages
{
    /// <summary>
    /// Message sent from server when an error occurs.
    /// </summary>
    public readonly struct ErrorMessage : IReceivableMessage
    {
        /// <summary>
        /// The error message.
        /// </summary>
        [NotNull] public readonly string Error;
    }

    /// <summary>
    /// Message sent from server to all clients when a player's status changes.
    /// </summary>
    public readonly struct PlayerStatusMessage : IReceivableMessage
    {
        /// <summary>
        /// The user's chosen display name.
        /// </summary>
        [NotNull] public readonly string Username;

        /// <summary>
        /// The user's selected game ID.
        /// Null if not selected yet.
        /// </summary>
        [CanBeNull] public readonly string GameID;

        /// <summary>
        /// Whether the user is ready to start the game.
        /// </summary>
        public readonly bool Ready;
    }

    /// <summary>
    /// Message sent to a player when they join a lobby, to inform them about other players.
    /// </summary>
    public readonly struct LobbyJoinedMessage : IReceivableMessage
    {
        /// <summary>
        /// IDs that the player may select from for this lobby's game.
        /// Returns all game IDs, though some may already be taken by other players in the lobby.
        /// </summary>
        [NotNull] public readonly List<string> GameIDs;

        /// <summary>
        /// Info about each other player in the lobby.
        /// </summary>
        [NotNull] public readonly List<PlayerStatusMessage> PlayerStatuses;
    }

    /// <summary>
    /// Message sent from client when they want to select a game ID.
    /// </summary>
    public readonly struct SelectGameIDMessage : ISendableMessage
    {
        /// <summary>
        /// The ID that the player wants to select for the game.
        /// Will be rejected if already selected by another player.
        /// </summary>
        [NotNull] public readonly string GameID;
    }

    /// <summary>
    /// Message sent from client to mark themselves as ready to start the game.
    /// Requires game ID being selected.
    /// </summary>
    public readonly struct ReadyMessage : ISendableMessage
    {
        /// <summary>
        /// Whether the player is ready to start the game.
        /// </summary>
        public readonly bool Ready;
    }

    /// <summary>
    /// Message sent from a player when the lobby wants to start the game.
    /// Requires that all players are ready.
    /// </summary>
    public readonly struct StartGameMessage : ISendableMessage
    { }
}
