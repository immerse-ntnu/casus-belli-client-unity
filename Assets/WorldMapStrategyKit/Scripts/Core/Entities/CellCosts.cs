using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public enum CELL_SIDE
	{
		TopLeft = 0,
		Top = 1,
		TopRight = 2,
		BottomRight = 3,
		Bottom = 4,
		BottomLeft = 5
	}

	/// <summary>
	/// Cell costs. Holds crossing cost for the cell.
	/// </summary>
	public struct CellCosts
	{
		/// <summary>
		/// If this cell is over sea/water.
		/// </summary>
		public bool isWater;

		/// <summary>
		/// Used by pathfinding in Cell mode to determine if route can cross a country. Defaults to true.
		/// </summary>
		public bool isBlocked;

		public float[] crossCost;

		/// <summary>
		/// This is the cost for crossing to this cell in the last pathfinding call.
		/// </summary>
		public float lastPathFindingCost;

		/// <summary>
		/// The elevation for the center of the cell.
		/// </summary>
		public float altitude;

		/// <summary>
		/// Used internally to avoid clearing the array of cell costs on each call to FindRoute
		/// </summary>
		public int cachedCallNumber;

		/// <summary>
		/// The cost returned by the call to the event OnPathFindingCrossCell. The returned value is cached here and reused inside the path finding algorithm.
		/// </summary>
		public float cachedEventCostValue;

		/// <summary>
		/// Assigns a crossing cost for a given hexagonal side
		/// </summary>
		/// <param name="side">Side.</param>
		/// <param name="cost">Cost.</param>
		public void SetSideCrossCost(CELL_SIDE side, float cost)
		{
			if (crossCost == null)
				crossCost = new float[6];
			crossCost[(int)side] = cost;
		}

		/// <summary>
		/// Gets the crossing costs for a given hexagonal side
		/// </summary>
		/// <returns>The side cross cost.</returns>
		public float GetSideCrossCost(CELL_SIDE side)
		{
			if (crossCost == null)
				return 0;
			return crossCost[(int)side];
		}

		/// <summary>
		/// Sets the same crossing cost for all sides of the hexagon.
		/// </summary>
		public void SetAllSidesCost(float cost)
		{
			if (crossCost == null)
				crossCost = new float[6];
			for (var k = 0; k < 6; k++)
				crossCost[k] = cost;
		}
	}
}