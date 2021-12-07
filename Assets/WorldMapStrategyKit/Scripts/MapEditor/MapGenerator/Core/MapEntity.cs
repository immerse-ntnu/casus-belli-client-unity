using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WorldMapStrategyKit.MapGenerator.Geom;

namespace WorldMapStrategyKit
{
	public interface MapEntity
	{
		string name { get; set; }
		MapRegion region { get; set; }
		bool visible { get; set; }
		bool hasCapital { get; set; }
	}
}