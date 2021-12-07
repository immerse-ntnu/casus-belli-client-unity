using UnityEngine;
using System;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using System.Threading;
using WorldMapStrategyKit.ClipperLib;

namespace WorldMapStrategyKit
{
	public partial class WMSK_Editor : MonoBehaviour
	{
		/// <summary>
		/// Adjusts all countries frontiers to match the hexagonal grid
		/// </summary>
		public IEnumerator HexifyCountries(HexifyOpContext context)
		{
			var cells = _map.cells;
			if (cells == null)
				yield break;

			// Initialization
			cancelled = false;
			hexifyContext = context;

			if (procCells == null || procCells.Length < cells.Length)
				procCells = new RegionCell[cells.Length];
			for (var k = 0; k < cells.Length; k++)
				procCells[k].entityIndex = -1;

			// Compute area of a single cell; for optimization purposes we'll ignore all regions whose surface is smaller than a 20% the size of a cell
			float minArea = 0;
			Region templateCellRegion = null;
			for (var k = 0; k < cells.Length; k++)
				if (cells[k] != null)
				{
					templateCellRegion = new Region(null, 0);
					templateCellRegion.UpdatePointsAndRect(cells[k].points, true);
					minArea = templateCellRegion.rect2DArea * 0.2f;
					break;
				}

			if (templateCellRegion == null)
				yield break;

			if (hexagonPoints == null || hexagonPoints.Length != 6)
				hexagonPoints = new Vector2[6];
			for (var k = 0; k < 6; k++)
				hexagonPoints[k] = templateCellRegion.points[k] - templateCellRegion.center;

			// Pass 1: remove minor regions
			yield return RemoveSmallRegions(minArea, _map.countries);

			// Pass 2: assign all region centers to each country from biggest country to smallest country
			if (!cancelled)
				yield return AssignRegionCenters(_map.countries);

			// Pass 3: add cells to target countries
			if (!cancelled)
				yield return AddHexagons(_map.countries);

			// Pass 4: merge adjacent regions
			if (!cancelled)
				yield return
					MergeAdjacentRegions(_map.countries);

			// Pass 5: remove cells from other countries
			if (!cancelled)
				yield return RemoveHexagons(_map.countries);

			// Pass 6: update geometry of resulting countries
			if (!cancelled)
				yield return UpdateCountries();

			if (!cancelled)
			{
				_map.OptimizeFrontiers();
				_map.Redraw(true);
			}

			hexifyContext.progress(1f, hexifyContext.title, ""); // hide progress bar
			yield return null;

			if (hexifyContext.finish != null)
				hexifyContext.finish(cancelled);
		}

		private IEnumerator UpdateCountries()
		{
			var _countries = _map.countries;
			for (var k = 0; k < _countries.Length; k++)
			{
				if (k % 10 == 0)
				{
					if (hexifyContext.progress != null)
						if (hexifyContext.progress((float)k / _countries.Length, hexifyContext.title,
							"Pass 6/6: updating countries..."))
						{
							cancelled = true;
							hexifyContext.finish(true);
							yield break;
						}
					yield return null;
				}

				var country = _countries[k];
				_map.CountrySanitize(k, 3, false);
				if (country.regions.Count == 0)
				{
					if (_map.CountryDelete(k, true, false))
					{
						_countries = _map.countries;
						k--;
					}
				}
				else
					_map.RefreshCountryGeometry(country);
			}

			// Update cities and mount points
			var cities = _map.cities;
			var cityCount = cities.Length;
			for (var k = 0; k < cityCount; k++)
			{
				var city = cities[k];
				var countryIndex = _map.GetCountryIndex(city.unity2DLocation);
				if (city.countryIndex != countryIndex)
				{
					city.countryIndex = countryIndex;
					city.province = ""; // clear province since it does not apply anymore
				}
			}
			var mountPointCount = _map.mountPoints.Count;
			for (var k = 0; k < mountPointCount; k++)
			{
				var mp = _map.mountPoints[k];
				var countryIndex = _map.GetCountryIndex(mp.unity2DLocation);
				if (mp.countryIndex != countryIndex)
				{
					mp.countryIndex = countryIndex;
					mp.provinceIndex =
						-1; // same as cities - province cleared in case it's informed since it does not apply anymore
				}
			}
		}
	}
}