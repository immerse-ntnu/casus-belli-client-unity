using UnityEngine;
using System.Collections;

namespace WorldMapStrategyKit
{
	public enum CITY_CLASS : byte
	{
		CITY = 1,
		REGION_CAPITAL = 2,
		COUNTRY_CAPITAL = 4
	}

	public partial class City : IExtendableAttribute
	{
		/// <summary>
		/// An unique identifier useful to persist data between sessions. Used by serialization.
		/// </summary>
		public int uniqueId { get; set; }

		/// <summary>
		/// Use this property to add/retrieve custom attributes for this country
		/// </summary>
		public JSONObject attrib { get; set; }

		public string name;
		public string province;
		public int countryIndex;
		public Vector2 unity2DLocation;
		public int population;
		public CITY_CLASS cityClass;

		/// <summary>
		/// Set by DrawCities method.
		/// </summary>
		public bool isVisible;

		public City(string name, string province, int countryIndex, int population, Vector2 location,
			CITY_CLASS cityClass, int uniqueId)
		{
			this.name = name;
			this.province = province;
			this.countryIndex = countryIndex;
			this.population = population;
			unity2DLocation = location;
			this.cityClass = cityClass;
			this.uniqueId = uniqueId;
			attrib = new JSONObject();
		}

		public City Clone()
		{
			var c = new City(name, province, countryIndex, population, unity2DLocation, cityClass,
				uniqueId);
			c.attrib = new JSONObject();
			c.attrib.Absorb(attrib);
			return c;
		}
	}
}