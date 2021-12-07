using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using System.IO;
using System.Text;

namespace WorldMapStrategyKit
{
	public class GameLogicExample : MonoBehaviour
	{
		public Dropdown countriesDropdown;
		public InputField resourcesInputField;
		public RectTransform infoPanel;
		public Text infoPanelCountryName, infoPanelCountryResources;

		private const string RESOURCE_NAME = "Resources";
		private const string ATTRIBUTES_FILENAME = "CountriesAttributes";

		private WMSK map;
		private List<string> countryNames;

		// Use this for initialization
		private void Start()
		{
			map = WMSK.instance;

			PopulateCountryNames();
			LoadButtonClick();

			map.OnCountryClick += SelectCountryFromList;
			map.OnMouseMove += ShowTooltip;
		}

		private void ShowTooltip(float x, float y)
		{
			var country = map.GetCountry(new Vector2(x, y));
			if (country == null)
			{
				infoPanel.anchoredPosition = new Vector2(-300, 0);
				return;
			}
			infoPanelCountryName.text = country.name;
			infoPanelCountryResources.text = "$" + country.attrib["Resources"];
			infoPanel.anchoredPosition = Input.mousePosition + new Vector3(10, -30);
		}

		private void PopulateCountryNames()
		{
			countryNames = new List<string>();
			countryNames.Add("");
			countryNames.AddRange(map.GetCountryNames(false, false));

			countriesDropdown.ClearOptions();
			countriesDropdown.AddOptions(countryNames);
		}

		private void SelectCountryFromList(int countryIndex, int regionIndex, int buttonIndex)
		{
			var countryName = map.GetCountry(countryIndex).name;
			countriesDropdown.value = countryNames.IndexOf(countryName);
		}

		public void CountrySelected(int index)
		{
			var countryName = countryNames[index];
			map.BlinkCountry(countryName, Color.red, Color.yellow, 2f, 0.3f);

			var country = map.GetCountry(countryName);
			resourcesInputField.text = country.attrib[RESOURCE_NAME];
		}

		public void UpdateButtonClick()
		{
			var countryName = countriesDropdown.captionText.text;
			var country = map.GetCountry(countryName);
			if (country == null)
				return;

			var resourcesValue = resourcesInputField.text;
			country.attrib[RESOURCE_NAME] = resourcesValue;

			var txt = GameObject.Find(countryName + "_ResourcesText");
			if (txt == null)
			{
				var tm = map.AddMarker2DText("$" + resourcesValue, country.center);
				tm.transform.localScale *= 10f;
				tm.gameObject.name = countryName + "_ResourcesText";
			}
			else
				txt.GetComponent<TextMesh>().text = "$" + resourcesValue;
		}

		public void SaveButtonClick()
		{
			var countryAttributes = map.GetCountriesAttributes();
			File.WriteAllText(ATTRIBUTES_FILENAME, countryAttributes, Encoding.UTF8);
		}

		public void LoadButtonClick()
		{
			if (!File.Exists(ATTRIBUTES_FILENAME))
				return;
			var data = File.ReadAllText(ATTRIBUTES_FILENAME);
			map.SetCountriesAttributes(data);
		}
	}
}