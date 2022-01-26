using System;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class Country : AdminEntity
	{
		/// <summary>
		/// Continent name.
		/// </summary>
		public string continent;

		/// <summary>
		/// List of provinces that belongs to this country.
		/// </summary>
		public Province[] provinces;

		/// <summary>
		/// The index of the capital city
		/// </summary>
		public int capitalCityIndex = -1;

		private int[] _neighbours;

		/// <summary>
		/// Custom array of countries that could be reached from this country. Useful for Country path-finding.
		/// It defaults to natural neighbours of the country but you can modify its contents and add your own potential destinations per country.
		/// </summary>
		public override int[] neighbours
		{
			get
			{
				if (_neighbours == null)
				{
					var cc = 0;
					var nn = new List<Country>();
					if (regions != null)
					{
						regions.ForEach(r =>
						{
							if (r != null && r.neighbours != null)
								r.neighbours.ForEach(n =>
									{
										if (n != null)
										{
											var otherCountry = (Country)n.entity;
											if (!nn.Contains(otherCountry))
												nn.Add(otherCountry);
										}
									}
								);
						});
						cc = nn.Count;
					}
					_neighbours = new int[cc];
					for (var k = 0; k < cc; k++)
						_neighbours[k] = WMSK.instance.GetCountryIndex(nn[k]);
				}
				return _neighbours;
			}
			set => _neighbours = value;
		}

		/// <summary>
		/// True for a country acting as a provinces pool created by CreateCountryProvincesPool function.
		/// The effect of this field is that all transfer operations will ignore its borders which results in a faster operation
		/// </summary>
		public bool isPool;

		// Standardized codes
		public string fips10_4 = "";
		public string iso_a2 = "";
		public string iso_a3 = "";
		public string iso_n3 = "";

		/// <summary>
		/// If provinces will be shown for this country
		/// </summary>
		public bool showProvinces = true;

		/// <summary>
		/// If province highlighting is enabled for this country
		/// </summary>
		public bool allowProvincesHighlight = true;

		/// <summary>
		/// Current number of visible cities of this country
		/// </summary>
		[NonSerialized] public int visibleCities;

		/// <summary>
		/// Creates a new country
		/// </summary>
		/// <param name="name">Name.</param>
		/// <param name="continent">Continent.</param>
		/// <param name="uniqueId">For user created countries, use a number between 1-999 to uniquely identify this country.</param>
		public Country(string name, string continent, int uniqueId)
		{
			this.name = name;
			this.continent = continent;
			regions = new List<Region>();
			this.uniqueId = uniqueId;
			attrib = new JSONObject();
			mainRegionIndex = -1;
		}

		public Country Clone()
		{
			var c = new Country(name, continent, uniqueId);
			c.center = center;
			c.regions = new List<Region>(regions.Count);
			for (var k = 0; k < regions.Count; k++)
				c.regions.Add(regions[k].Clone());
			c.customLabel = customLabel;
			c.labelColor = labelColor;
			c.labelColorOverride = labelColorOverride;
			c.labelFontOverride = labelFontOverride;
			c.labelVisible = labelVisible;
			c.labelOffset = labelOffset;
			c.labelRotation = labelRotation;
			c.provinces = provinces;
			c.hidden = hidden;
			c.attrib = new JSONObject();
			c.attrib.Absorb(attrib);
			c.regionsRect2D = regionsRect2D;
			return c;
		}
	}
}