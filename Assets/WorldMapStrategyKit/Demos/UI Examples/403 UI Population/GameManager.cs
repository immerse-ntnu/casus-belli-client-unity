using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace WorldMapStrategyKit
{
	public class GameManager : MonoBehaviour
	{
		public Dropdown countriesDropdown;
		public Dropdown provincesDropdown;

		private WMSK map;

		private void Start()
		{
			// Grab a reference to WMSK API
			map = WMSK.instance;

			// Fill dropdown with country names
			PopulateCountries();
		}

		/// <summary>
		/// Populate the countries dropdown with names
		/// </summary>
		private void PopulateCountries()
		{
			var countries = new List<string>();
			countries.Add("");
			countries.AddRange(map.GetCountryNames(false, false));

			countriesDropdown.ClearOptions();
			countriesDropdown.AddOptions(countries);

			provincesDropdown.ClearOptions();
		}

		/// <summary>
		/// This event function is called by the country dropdown script. It has been linked on the UI directly.
		/// </summary>
		public void CountrySelected(int index)
		{
			var countryName = countriesDropdown.options[index].text;
			map.BlinkCountry(countryName, Color.red, Color.yellow, 3f, 0.2f);

			PopulateProvinces(countryName);
		}

		/// <summary>
		/// Populate the provinces dropdown with names
		/// </summary>
		private void PopulateProvinces(string countryName)
		{
			var countryIndex = map.GetCountryIndex(countryName);
			var provinces = new List<string>();
			provinces.AddRange(map.GetProvinceNames(countryIndex, false));

			provincesDropdown.ClearOptions();
			provincesDropdown.AddOptions(provinces);
		}

		/// <summary>
		/// This event function is called by the country dropdown script. It has been linked on the UI directly.
		/// </summary>
		public void ProvinceSelected(int index)
		{
			var provinceName = provincesDropdown.options[index].text;
			var countryName = countriesDropdown.options[countriesDropdown.value].text;
			var provinceIndex = map.GetProvinceIndex(countryName, provinceName);
			map.BlinkProvince(provinceIndex, Color.blue, Color.white, 2f, 0.1f);
		}

		/// <summary>
		/// Event function triggered by the "Zoom in" button click
		/// </summary>
		public void ZoomIn()
		{
			// Take the province id
			var provinceName = provincesDropdown.options[provincesDropdown.value].text;
			var countryName = countriesDropdown.options[countriesDropdown.value].text;
			var provinceIndex = map.GetProvinceIndex(countryName, provinceName);

			// Color and zoom in the province
			map.ToggleProvinceSurface(provinceIndex, true, new Color(0, 1, 0, 0.5f));
			map.FlyToProvince(provinceIndex, 2f, 0.1f);
		}
	}
}