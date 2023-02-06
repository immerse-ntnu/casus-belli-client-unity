using System.Collections.Generic;
using Immerse.BfhClient.Api.GameTypes;
using JetBrains.Annotations;

namespace Immerse.BfhClient.Api.Messages
{
    /// <summary>
    /// Message sent from server when asking a supporting player who to support in an embattled area.
    /// </summary>
    public readonly struct SupportRequestMsg
    {
        /// <summary>
        /// The area from which support is asked, where the asked player should have a support order.
        /// </summary>
        [NotNull] public readonly string SupportingArea;

        /// <summary>
        /// List of possible players to support in the battle.
        /// </summary>
        [NotNull] public readonly List<string> SupportablePlayers;
    }

    /// <summary>
    /// Message sent from server to client to signal that client should submit orders.
    /// </summary>
    public readonly struct OrderRequestMsg
    { }

    /// <summary>
    /// Message sent from server to all clients when valid orders are received from all players.
    /// </summary>
    public readonly struct OrdersReceivedMsg
    {
        [NotNull] public readonly Dictionary<string, List<Order>> PlayerOrders;
    }

    /// <summary>
    /// Message sent from server to all clients when valid orders are received from a player.
    /// Used to show who the server is waiting for.
    /// </summary>
    public readonly struct OrdersConfirmationMsg
    {
        [NotNull] public readonly string Player;
    }

    /// <summary>
    /// Message sent from server to all clients when a battle result is calculated.
    /// </summary>
    public readonly struct BattleResultsMsg
    {
        [NotNull] public readonly List<Battle> Battles;
    }

    /// <summary>
    /// Message sent from server to all clients when the game is won.
    /// </summary>
    public readonly struct WinnerMsg
    {
        /// <summary>
        /// Player tag of the game's winner.
        /// </summary>
        [NotNull] public readonly string Winner;
    }

    /// <summary>
    /// Message sent from client when submitting orders.
    /// </summary>
    public readonly struct SubmitOrdersMsg
    {
        /// <summary>
        /// List of submitted orders.
        /// </summary>
        [NotNull] public readonly List<Order> Orders;

        public SubmitOrdersMsg(List<Order> orders)
        {
            Orders = orders;
        }
    }

    /// <summary>
    /// Message sent from client when declaring who to support with their support order.
    /// Forwarded by server to all clients to show who were given support.
    /// </summary>
    public readonly struct GiveSupportMsg
    {
        /// <summary>
        /// Name of the area in which the support order is placed.
        /// </summary>
        [NotNull] public readonly string SupportingArea;

        /// <summary>
        /// ID of the player in the destination area to support.
        /// Null if none were supported.
        /// </summary>
        [CanBeNull] public readonly string SupportedPlayer;

        public GiveSupportMsg(string supportingArea, string supportedPlayer)
        {
            SupportingArea = supportingArea;
            SupportedPlayer = supportedPlayer;
        }
    }

    /// <summary>
    /// Message passed from the client during winter council voting.
    /// Used for the throne expansion.
    /// </summary>
    public readonly struct WinterVoteMsg
    {
        /// <summary>
        /// ID of the player that the submitting player votes for.
        /// </summary>
        [NotNull] public readonly string Player;

        public WinterVoteMsg(string player)
        {
            Player = player;
        }
    }

    /// <summary>
    /// Message passed from the client with the swordMsg to declare where they want to use it.
    /// Used for the throne expansion.
    /// </summary>
    public readonly struct SwordMsg
    {
        /// <summary>
        /// Name of the area in which the player wants to use the sword in battle.
        /// </summary>
        [NotNull] public readonly string Area;

        /// <summary>
        /// Index of the battle in which to use the sword, in case of several battles in the area.
        /// </summary>
        public readonly int BattleIndex;

        public SwordMsg(string area, int battleIndex)
        {
            Area = area;
            BattleIndex = battleIndex;
        }
    }

    /// <summary>
    /// Message passed from the client with the ravenMsg when they want to spy on another player's orders.
    /// Used for the throne expansion.
    /// </summary>
    public readonly struct RavenMsg
    {
        /// <summary>
        /// ID of the player on whom to spy.
        /// </summary>
        [NotNull] public readonly string Player;

        public RavenMsg(string player)
        {
            Player = player;
        }
    }
}
