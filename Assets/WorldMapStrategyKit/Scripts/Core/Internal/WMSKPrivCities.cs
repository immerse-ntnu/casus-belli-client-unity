// World Map Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using UnityEngine;
using System;
using System.IO;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using System.Globalization;

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		private const string CITY_ATTRIB_DEFAULT_FILENAME = "citiesAttrib";

		// resources & gameobjects
		private Material citiesNormalMat, citiesRegionCapitalMat, citiesCountryCapitalMat;
		private GameObject citiesLayer;

		// internal cache
		private City[] visibleCities;
		private bool citiesAreSortedByPopulation;

		/// <summary>
		/// City look up dictionary. Used internally for fast searching of city objects.
		/// </summary>
		private Dictionary<City, int> _cityLookup;

		private int lastCityLookupCount = -1;

		private Dictionary<City, int> cityLookup
		{
			get
			{
				if (_cityLookup != null && cities.Length == lastCityLookupCount)
					return _cityLookup;
				if (_cityLookup == null)
					_cityLookup = new Dictionary<City, int>();
				else
					_cityLookup.Clear();
				if (cities != null)
				{
					var cityCount = cities.Length;
					for (var k = 0; k < cityCount; k++)
						_cityLookup[_cities[k]] = k;
				}
				lastCityLookupCount = _cityLookup.Count;
				return _cityLookup;
			}
		}

		private List<City> tmpCities;

		public Vector3 currentCityScale;

		#region IO stuff

		private void ReadCitiesPackedString()
		{
			ReadCitiesPackedString("cities10");
		}

		private void ReadCitiesPackedString(string filename)
		{
			var cityCatalogFileName = _geodataResourcesPath + "/" + filename;
			var ta = Resources.Load<TextAsset>(cityCatalogFileName);
			if (ta != null)
			{
				SetCityGeoData(ta.text);
				ReloadCitiesAttributes();
			}
		}

		private void ReloadCitiesAttributes()
		{
			var ta = Resources.Load<TextAsset>(_geodataResourcesPath + "/" + _cityAttributeFile);
			if (ta == null)
				return;
			SetCitiesAttributes(ta.text);
		}

		#endregion

		#region Drawing stuff

		private void CheckCityIcons()
		{
			if (_citySpot == null)
				_citySpot = Resources.Load<GameObject>("WMSK/Prefabs/CitySpot");
			if (_citySpotCapitalRegion == null)
				_citySpotCapitalRegion = Resources.Load<GameObject>("WMSK/Prefabs/CityCapitalRegionSpot");
			if (_citySpotCapitalCountry == null)
				_citySpotCapitalCountry =
					Resources.Load<GameObject>("WMSK/Prefabs/CityCapitalCountrySpot");
		}

		private int SortByPopulationDesc(City c1, City c2)
		{
			if (c1.population < c2.population)
				return 1;
			if (c1.population > c2.population)
				return -1;
			return 0;
		}

		/// <summary>
		/// Redraws the cities. This is automatically called by Redraw(). Used internally by the Map Editor. You should not need to call this method directly.
		/// </summary>
		public void DrawCities()
		{
			if (!_showCities || !gameObject.activeInHierarchy)
				return;

			CheckCityIcons();

			// Create cities layer
			var t = transform.Find("Cities");
			if (t != null)
				DestroyImmediate(t.gameObject);
			citiesLayer = new GameObject("Cities");
			citiesLayer.transform.SetParent(transform, false);
			citiesLayer.transform.localPosition = Misc.Vector3back * 0.001f;
			citiesLayer.layer = gameObject.layer;

			// Create cityclass parents
			var countryCapitals = new GameObject("Country Capitals");
			countryCapitals.transform.SetParent(citiesLayer.transform, false);
			var regionCapitals = new GameObject("Region Capitals");
			regionCapitals.transform.SetParent(citiesLayer.transform, false);
			var normalCities = new GameObject("Normal Cities");
			normalCities.transform.SetParent(citiesLayer.transform, false);

			if (cities == null)
				return;

			if (disposalManager != null)
			{
				disposalManager.MarkForDisposal(citiesLayer);
				disposalManager
					.MarkForDisposal(countryCapitals); // countryCapitals.hideFlags = HideFlags.DontSave;
				disposalManager
					.MarkForDisposal(regionCapitals); // regionCapitals.hideFlags = HideFlags.DontSave;
				disposalManager
					.MarkForDisposal(normalCities); // normalCities.hideFlags = HideFlags.DontSave;
			}

			if (!citiesAreSortedByPopulation && _maxCitiesPerCountry > 0)
			{
				Array.Sort(_cities, SortByPopulationDesc);
				citiesAreSortedByPopulation = true;
			}

			// Draw city marks
			numCitiesDrawn = 0;
			var minPopulation = _minPopulation * 1000;
			var visibleCount = 0;
			var maxCitiesPerCountry = _maxCitiesPerCountry > 0 ? _maxCitiesPerCountry : 999999;
			if (_maxCitiesPerCountry > 0)
				for (var k = 0; k < _countries.Length; k++)
					_countries[k].visibleCities = 0;
			var cityCount = cities.Length;
			for (var k = 0; k < cityCount; k++)
			{
				var city = _cities[k];
				var countryIndex = city.countryIndex;
				if (countryIndex < 0 || countryIndex >= _countries.Length)
				{
					city.isVisible = false;
					continue;
				}
				var country = _countries[countryIndex];
				if (country.hidden)
				{
					city.isVisible = false;
					continue;
				}
				var isCapital = ((int)city.cityClass & _cityClassAlwaysShow) != 0;
				if (isCapital)
					city.isVisible = true;
				else
					city.isVisible = country.visibleCities < maxCitiesPerCountry &&
					                 (minPopulation == 0 || city.population >= minPopulation);
				if (city.isVisible)
				{
					if (!isCapital)
						country.visibleCities++;
					GameObject cityObj, cityParent;
					Material mat;
					switch (city.cityClass)
					{
						case CITY_CLASS.COUNTRY_CAPITAL:
							cityObj = Instantiate(_citySpotCapitalCountry);
							mat = citiesCountryCapitalMat;
							cityParent = countryCapitals;
							break;
						case CITY_CLASS.REGION_CAPITAL:
							cityObj = Instantiate(_citySpotCapitalRegion);
							mat = citiesRegionCapitalMat;
							cityParent = regionCapitals;
							break;
						default:
							cityObj = Instantiate(_citySpot);
							mat = citiesNormalMat;
							cityParent = normalCities;
							break;
					}
					cityObj.name = k.ToString();
					if (disposalManager != null)
						disposalManager
							.MarkForDisposal(cityObj); // cityObj.hideFlags = HideFlags.DontSave;
					cityObj.hideFlags |= HideFlags.HideInHierarchy;
					cityObj.layer = citiesLayer.layer;
					cityObj.transform.SetParent(cityParent.transform, false);
					cityObj.transform.localPosition = city.unity2DLocation;
					var rr = cityObj.GetComponent<Renderer>();
					if (rr != null)
						rr.sharedMaterial = mat;

					numCitiesDrawn++;
					visibleCount++;
				}
			}

			// Cache visible cities (this is faster than iterate through the entire collection)
			if (visibleCities == null || visibleCities.Length != visibleCount)
				visibleCities = new City[visibleCount];
			for (var k = 0; k < cityCount; k++)
			{
				var city = _cities[k];
				if (city.isVisible)
					visibleCities[--visibleCount] = city;
			}

			// Toggle cities layer visibility according to settings
			citiesLayer.SetActive(_showCities);
			ScaleCities();
		}

		public string GetCityHierarchyName(int cityIndex)
		{
			if (cityIndex < 0 || cityIndex >= cities.Length)
				return "";
			switch (cities[cityIndex].cityClass)
			{
				case CITY_CLASS.COUNTRY_CAPITAL:
					return "Cities/Country Capitals/" + cityIndex.ToString();
				case CITY_CLASS.REGION_CAPITAL:
					return "Cities/Region Capitals/" + cityIndex.ToString();
				default:
					return "Cities/Normal Cities/" + cityIndex.ToString();
			}
		}

		private void ScaleCities()
		{
			if (!gameObject.activeInHierarchy)
				return;
			var cityScaler = citiesLayer.GetComponent<CityScaler>() ??
			                 citiesLayer.AddComponent<CityScaler>();
			cityScaler.map = this;
			if (_showCities)
				cityScaler.ScaleCities();
		}

		private void HighlightCity(int cityIndex)
		{
			_cityHighlightedIndex = cityIndex;
			_cityHighlighted = cities[cityIndex];

			// Raise event
			if (OnCityEnter != null)
				OnCityEnter(_cityHighlightedIndex);
		}

		private void HideCityHighlight()
		{
			if (_cityHighlightedIndex < 0)
				return;

			// Raise event
			if (OnCityExit != null)
				OnCityExit(_cityHighlightedIndex);
			_cityHighlighted = null;
			_cityHighlightedIndex = -1;
		}

		#endregion

		#region Internal API

		/// <summary>
		/// Returns any city near the point specified in local coordinates.
		/// </summary>
		/// <returns>The city near point.</returns>
		/// <param name="localPoint">Local point.</param>
		private int GetCityNearPoint(Vector2 localPoint) => GetCityNearPoint(localPoint, -1);

		/// <summary>
		/// Returns any city near the point specified for a given country in local coordinates.
		/// </summary>
		/// <returns>The city near point.</returns>
		/// <param name="localPoint">Local point.</param>
		/// <param name="countryIndex">Country index.</param>
		private int GetCityNearPoint(Vector2 localPoint, int countryIndex)
		{
			if (visibleCities == null)
				return -1;
			if (Application.isPlaying)
			{
				//												float hitPrecission = CITY_HIT_PRECISION * _cityIconSize * _cityHitTestPrecision;
				var rl = localPoint.x - currentCityScale.x; // hitPrecission;
				var rr = localPoint.x + currentCityScale.x; // hitPrecission;
				var rt = localPoint.y + currentCityScale.y; // hitPrecission;
				var rb = localPoint.y - currentCityScale.y; // hitPrecission;
				for (var c = 0; c < visibleCities.Length; c++)
				{
					var city = visibleCities[c];
					if (countryIndex < 0 || city.countryIndex == countryIndex)
					{
						var cityLoc = city.unity2DLocation;
						if (cityLoc.x > rl && cityLoc.x < rr && cityLoc.y > rb && cityLoc.y < rt)
							return GetCityIndex(city);
					}
				}
			}
			else
			{
				// Use alternate method since city scale is different in Scene View
				var minDist = float.MaxValue;
				City candidate = null;
				for (var c = 0; c < visibleCities.Length; c++)
				{
					var city = visibleCities[c];
					if (countryIndex < 0 || city.countryIndex == countryIndex)
					{
						var cityLoc = city.unity2DLocation;
						var dist = (cityLoc.x - localPoint.x) * (cityLoc.x - localPoint.x) +
						           (cityLoc.y - localPoint.y) * (cityLoc.y - localPoint.y);
						if (dist < minDist)
						{
							minDist = dist;
							candidate = city;
						}
					}
				}
				if (candidate != null)
					return GetCityIndex(candidate);
			}
			return -1;
		}

		/// <summary>
		/// Returns the file name corresponding to the current city data file
		/// </summary>
		public string GetCityFileName() => "cities10.txt";

		#endregion
	}
}