using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.UI;

namespace WorldMapStrategyKit
{
	public class UIPanelDemoViewport : MonoBehaviour
	{
		public GameObject canvas;
		public Text countryName;
		public Text provinceName;
		public Text cityName;
		public Text population;

		private WMSK map;
		private GUIStyle labelStyle;

		private void Start()
		{
			// Get a reference to the World Map API:
			map = WMSK.instance;

			// UI Setup - non-important, only for this demo
			labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleLeft;
			labelStyle.normal.textColor = Color.white;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			/* Register events: this is optionally but allows your scripts to be informed instantly as the mouse enters or exits a country, province or city */
			map.OnCityEnter += OnCityEnter;
			map.OnCityExit += OnCityExit;
		}

		private void OnGUI()
		{
			GUI.Label(new Rect(10, 10, 500, 30), "Move mouse over a city to show data.", labelStyle);
		}

		private void OnCityEnter(int cityIndex)
		{
			var city = map.GetCity(cityIndex);
			ShowCityInfo(city);
		}

		private void OnCityExit(int cityIndex)
		{
			HidePanel();
		}

		private void ShowCityInfo(City city)
		{
			if (city == null)
				return;

			// Update text labels
			cityName.text = city.name;
			population.text = city.population.ToString();
			countryName.text = map.GetCityCountryName(city);
			provinceName.text = map.GetCityProvinceName(city);

			// Reposition UI Panel over the viewport
			var worldPos = map.Map2DToWorldPosition(map.cursorLocation, 1f);
			canvas.transform.position = worldPos;
			canvas.transform.rotation = map.renderViewport.transform.rotation;
			canvas.SetActive(true);
		}

		private void HidePanel()
		{
			// Move panel out of screen
			canvas.SetActive(false);
		}
	}
}