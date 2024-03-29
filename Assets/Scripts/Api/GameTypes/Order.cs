using JetBrains.Annotations;
using Newtonsoft.Json;

namespace Immerse.BfhClient.Api.GameTypes
{
    /// <summary>
    /// An order submitted by a player for one of their units in a given round.
    /// </summary>
    public struct Order
    {
        /// <summary>
        /// The type of order submitted. Restricted by unit type and region.
        /// Can only be of the constants defined in <see cref="OrderType"/>.
        /// </summary>
        [JsonProperty("type", Required = Required.Always)]
        [NotNull]
        public string Type;

        /// <summary>
        /// The player submitting the order.
        /// </summary>
        [JsonProperty("player", Required = Required.Always)]
        [NotNull]
        public string Player;

        /// <summary>
        /// Name of the region where the order is placed.
        /// </summary>
        [JsonProperty("origin", Required = Required.Always)]
        [NotNull]
        public string Origin;

        /// <summary>
        /// For move and support orders: name of destination region.
        /// </summary>
        [JsonProperty("destination")]
        [CanBeNull]
        public string Destination;

        /// <summary>
        /// For move orders with horse units: optional name of second destination region to move to if the first
        /// destination was reached.
        /// </summary>
        [JsonProperty("secondDestination")]
        [CanBeNull]
        public string SecondDestination;

        /// <summary>
        /// For move orders: name of DangerZone the order tries to pass through, if any.
        /// </summary>
        [JsonProperty("via")]
        [CanBeNull]
        public string Via;

        /// <summary>
        /// For build orders: type of unit to build.
        /// Can only be of the constants defined in <see cref="UnitType"/>.
        /// </summary>
        [JsonProperty("build")]
        [CanBeNull]
        public string Build;
    }

    /// <summary>
    /// Valid values for a player-submitted order's type.
    /// </summary>
    public static class OrderType
    {
        /// <summary>
        /// An order for a unit to move from one area to another.
        /// Includes internal moves in winter.
        /// </summary>
        public const string Move = "move";

        /// <summary>
        /// An order for a unit to support battle in an adjacent area.
        /// </summary>
        public const string Support = "support";

        /// <summary>
        /// For ship unit at sea: an order to transport a land unit across the sea.
        /// </summary>
        public const string Transport = "transport";

        /// <summary>
        /// For land unit in unconquered castle area: an order to besiege the castle.
        /// </summary>
        public const string Besiege = "besiege";

        /// <summary>
        /// For player-controlled area in winter: an order for what type of unit to build in the area.
        /// </summary>
        public const string Build = "build";
    }
}
