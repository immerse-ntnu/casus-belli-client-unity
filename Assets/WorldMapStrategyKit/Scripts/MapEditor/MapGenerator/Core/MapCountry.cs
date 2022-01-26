using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit.MapGenerator
{
	public class MapCountry : MapEntity
	{
		public string name { get; set; }
		public MapRegion region { get; set; }
		public Vector2 capitalCenter;
		public List<MapProvince> provinces;
		public Color fillColor = Color.gray;
		public bool visible { get; set; }
		public bool hasCapital { get; set; }

		public MapCountry(string name)
		{
			this.name = name;
			visible = true;
			provinces = new List<MapProvince>();
		}

		public MapCountry() : this("") { }
	}
}