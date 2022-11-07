using System.Collections.Generic;
using JetBrains.Annotations;

namespace Immerse.BfhClient.Api.GameTypes
{
    /// <summary>
    /// Valid values for a player unit's type.
    /// </summary>
    public static class UnitType
    {
        /// <summary>
        /// A land unit that gets a +1 modifier in battle.
        /// </summary>
        public const string Footman = "footman";

        /// <summary>
        /// A land unit that moves 2 areas at a time.
        /// </summary>
        public const string Horse = "horse";

        /// <summary>
        /// A unit that can move into sea areas and coastal areas.
        /// </summary>
        public const string Ship = "ship";

        /// <summary>
        /// A land unit that instantly conquers neutral castles, and gets a +1 modifier in attacks on castles.
        /// </summary>
        public const string Catapult = "catapult";
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

    /// <summary>
    /// An order submitted by a player for one of their units in a given round.
    /// </summary>
    public readonly struct Order
    {
        /// <summary>
        /// The type of order submitted. Restricted by unit type and area.
        /// Can only be of the constants defined in <see cref="OrderType"/>.
        /// </summary>
        [NotNull] public readonly string Type;

        /// <summary>
        /// The player submitting the order.
        /// </summary>
        [NotNull] public readonly string Player;

        /// <summary>
        /// Name of the area where the order is placed.
        /// </summary>
        [NotNull] public readonly string From;

        /// <summary>
        /// For move and support orders: name of destination area.
        /// </summary>
        [CanBeNull] public readonly string To;

        /// <summary>
        /// For move orders: name of DangerZone the order tries to pass through, if any.
        /// </summary>
        [CanBeNull] public readonly string Via;

        /// <summary>
        /// For build orders: type of unit to build.
        /// Can only be of the constants defined in <see cref="UnitType"/>.
        /// </summary>
        [CanBeNull] public readonly string Build;
    }

    /// <summary>
    /// Results of a battle from conflicting move orders, an attempt to conquer a neutral area,
    /// or an attempt to cross a danger zone.
    /// </summary>
    public readonly struct Battle
    {
        /// <summary>
        /// The dice and modifier results of the battle.
        /// If length is one, the battle was a neutral conquer attempt.
        /// If length is more than one, the battle was between players.
        /// </summary>
        [NotNull] public readonly List<Result> Results;

        /// <summary>
        /// In case of danger zone crossing: name of the danger zone.
        /// </summary>
        [CanBeNull] public readonly string DangerZone;
    }

    /// <summary>
    /// Dice and modifier result for a battle.
    /// </summary>
    public readonly struct Result
    {
        /// <summary>
        /// The sum of the dice roll and modifiers.
        /// </summary>
        public readonly int Total;

        /// <summary>
        /// The modifiers comprising the result, including the dice roll.
        /// </summary>
        [NotNull] public readonly List<Modifier> Parts;

        /// <summary>
        /// If result of a move order to the battle: the move order in question.
        /// </summary>
        [CanBeNull] public readonly Order? Move;

        /// <summary>
        /// If result of a defending unit in an area: the name of the area.
        /// </summary>
        [CanBeNull] public readonly string DefenderArea;
    }

    /// <summary>
    /// Valid values for a result modifier's type.
    /// </summary>
    public static class ModifierType
    {
        /// <summary>
        /// Bonus from a random dice roll.
        /// </summary>
        public const string Dice = "dice";

        /// <summary>
        /// Bonus for the type of unit.
        /// </summary>
        public const string Unit = "unit";

        /// <summary>
        /// Penalty for attacking a neutral or defended forested area.
        /// </summary>
        public const string Forest = "forest";

        /// <summary>
        /// Penalty for attacking a neutral or defended castle area.
        /// </summary>
        public const string Castle = "castle";

        /// <summary>
        /// Penalty for attacking across a river, from the sea, or across a transport.
        /// </summary>
        public const string Water = "water";

        /// <summary>
        /// Bonus for attacking across a danger zone and surviving.
        /// </summary>
        public const string Surprise = "surprise";

        /// <summary>
        /// Bonus from supporting player in a battle.
        /// </summary>
        public const string Support = "support";
    }

    /// <summary>
    /// A typed number that adds to a player's result in a battle.
    /// </summary>
    public readonly struct Modifier
    {
        /// <summary>
        /// The source of the modifier.
        /// Can only be of the constants defined in <see cref="ModifierType"/>.
        /// </summary>
        [NotNull] public readonly string Type;

        /// <summary>
        /// The positive or negative number that modifies the result total.
        /// </summary>
        public readonly int Value;

        /// <summary>
        /// If modifier was from a support: the supporting player.
        /// </summary>
        [CanBeNull] public readonly string SupportingPlayer;
    }
}
