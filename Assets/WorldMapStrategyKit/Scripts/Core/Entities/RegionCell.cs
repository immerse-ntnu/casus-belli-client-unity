using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	/// <summary>
	/// Represents a cell in a region
	/// </summary>
	public struct RegionCell
	{
		public int entityIndex;
		public Region entityRegion;
		public Region cellRegion;
	}
}