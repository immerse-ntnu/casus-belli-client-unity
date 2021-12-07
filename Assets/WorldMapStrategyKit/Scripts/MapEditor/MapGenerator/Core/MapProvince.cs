using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using WorldMapStrategyKit.MapGenerator.Geom;

namespace WorldMapStrategyKit
{
	public partial class MapProvince : MapEntity
	{
		public string name { get; set; }

		/// <summary>
		/// The country to which this province belongs to.
		/// </summary>
		public int countryIndex = -1;

		public bool ignoreTerritories;

		public MapRegion region { get; set; }

		public Vector2 center;

		public bool visible { get; set; }

		public bool hasCapital { get; set; }

		public Color color;

		/// <summary>
		/// Optional value that can be set with CellSetTag. You can later get the cell quickly using CellGetWithTag method.
		/// </summary>
		public int tag;

		public int row, column;

		public MapProvince(string name, Vector2 center)
		{
			this.name = name;
			this.center = center;
			visible = true;
		}

		public MapProvince(Vector2 center) : this("", center) { }

		public MapProvince() : this("", Vector2.zero) { }

		public MapProvince(string name) : this(name, Vector2.zero) { }
	}
}