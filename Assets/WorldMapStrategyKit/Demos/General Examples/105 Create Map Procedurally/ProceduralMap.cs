using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class ProceduralMap : MonoBehaviour
	{
		private WMSK map;

		private void Start()
		{
			// 1) Get a reference to the WMSK API
			map = WMSK.instance;

			// 2) Remove any existing map; to prevent loading geodata files at start, check the toggle DontLoadGeodataAtStart in WMSK inspector
			map.ClearAll();

			// 3) Create country based on points
			Vector2[] points =
			{
				new(0.05f, 0.12f),
				new(0.07f, 0.18f),
				new(0.23f, 0.25f),
				new(0.3f, 0.2f),
				new(0.31f, 0.11f),
				new(0.28f, 0.14f),
				new(0.25f, 0.09f),
				new(0.2f, 0.1f)
			};
			var countryIndex = CreateCountry("My country", points);

			//CreateProvince("My Province", countryIndex, points);

			// 4) Draw the map
			map.Redraw();

			// Optional: Fill the new country with a color
			var countryColor = new Color(0.698f, 0.396f, 0.094f);
			map.ToggleCountrySurface(countryIndex, true, countryColor);
		}

		/// <summary>
		/// Creates a country with a name and list of points.
		/// </summary>
		/// <returns>The country index.</returns>
		private int CreateCountry(string name, Vector2[] points)
		{
			// 1) Initialize a new country
			var country = new Country(name, "Continent", 1);

			// 2) Define the land region for this country with a list of points with coordinates from -0.5, -0.5 (bottom/left edge of map) to 0.5, 0.5 (top/right edge of map)
			// Note: the list of points should be expressed in clock-wise order
			var region = new Region(country, 0);

			region.UpdatePointsAndRect(points);

			// 3) Add the region to the country (a country can have multiple regions, like islands)
			country.regions.Add(region);

			// 4) Add the new country to the map
			var countryIndex = map.CountryAdd(country);

			return countryIndex;
		}

		/// <summary>
		/// Creates a province with a name and list of points.
		/// </summary>
		/// <returns>The country index.</returns>
		private int CreateProvince(string name, int countryIndex, Vector2[] points)
		{
			// 1) Initialize a new province
			var province = new Province(name, countryIndex, 0);

			// 2) Define the land region for this country with a list of points with coordinates from -0.5, -0.5 (bottom/left edge of map) to 0.5, 0.5 (top/right edge of map)
			// Note: the list of points should be expressed in clock-wise order
			var region = new Region(province, 0);

			region.UpdatePointsAndRect(points);

			// 3) Add the region to the province (a province can also have multiple regions, like islands)
			province.regions = new List<Region>();
			province.regions.Add(region);

			// 4) Add the new country to the map
			var provinceIndex = map.ProvinceAdd(province);
			return provinceIndex;
		}
	}
}