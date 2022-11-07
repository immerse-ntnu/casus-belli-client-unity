using System.Collections.Generic;
using JetBrains.Annotations;

namespace Immerse.BfhClient.Api.GameTypes
{
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
}
