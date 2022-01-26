using UnityEngine;

namespace WorldMapStrategyKit
{
	public class MapCity
	{
		public string name;
		public int provinceIndex;
		public int countryIndex;
		public Vector2 unity2DLocation;
		public int population;
		public CITY_CLASS cityClass;

		public MapCity(string name, int provinceIndex, int countryIndex, int population, Vector2 location,
			CITY_CLASS cityClass)
		{
			this.name = name;
			this.provinceIndex = provinceIndex;
			this.countryIndex = countryIndex;
			this.population = population;
			unity2DLocation = location;
			this.cityClass = cityClass;
		}
	}
}