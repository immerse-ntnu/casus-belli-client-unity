using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoFoW : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle labelStyle, labelStyleShadow, buttonStyle;
		private bool enableClearFogOnClick = true, enableClearFogCountryOnClick;

		private void Start()
		{
			// Get a reference to the World Map API:
			map = WMSK.instance;

			// UI Setup - non-important, only for this demo
			labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.white;
			labelStyleShadow = new GUIStyle(labelStyle);
			labelStyleShadow.normal.textColor = Color.black;
			buttonStyle = new GUIStyle(labelStyle);
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.normal.background = Texture2D.whiteTexture;
			buttonStyle.normal.textColor = Color.white;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			/* Register events: this is optionally but allows your scripts to be informed instantly as the mouse enters or exits a country, province or city */
			map.OnCityEnter += (int cityIndex) => Debug.Log("Entered city " + map.cities[cityIndex].name);
			map.OnCityExit += (int cityIndex) => Debug.Log("Exited city " + map.cities[cityIndex].name);
			map.OnCityClick += (int cityIndex, int buttonIndex) =>
				Debug.Log("Clicked city " + map.cities[cityIndex].name);
			map.OnCountryEnter += (int countryIndex, int regionIndex) =>
				Debug.Log("Entered country " + map.countries[countryIndex].name);
			map.OnCountryExit += (int countryIndex, int regionIndex) =>
				Debug.Log("Exited country " + map.countries[countryIndex].name);
			map.OnCountryClick += (int countryIndex, int regionIndex, int buttonIndex) =>
				Debug.Log("Clicked country " + map.countries[countryIndex].name);
			map.OnProvinceEnter += (int provinceIndex, int regionIndex) =>
				Debug.Log("Entered province " + map.provinces[provinceIndex].name);
			map.OnProvinceExit += (int provinceIndex, int regionIndex) =>
				Debug.Log("Exited province " + map.provinces[provinceIndex].name);
			map.OnProvinceClick += (int provinceIndex, int regionIndex, int buttonIndex) =>
				Debug.Log("Clicked province " + map.provinces[provinceIndex].name);
			map.OnClick += (float x, float y, int buttonIndex) => MakeClick(x, y);
			map.CenterMap();

			map.gridColumns = 300;
			map.gridRows = 300;
			map.showGrid = true;

			// Example: how to clear a group of cells inside a country
//												map.OnClick += (float x, float y, int buttonIndex) => { 
//																List<int> cells = map.GetCellsInCountry(map.GetCountryIndex("Spain"));
//																map.FogOfWarSetCells(cells, 0.1f);
//												};
		}

		// Update is called once per frame
		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			// Assorted options to show/hide frontiers, cities, Earth and enable country highlighting
			GUI.Box(new Rect(0, 0, 185, 140), "");

			var prev = enableClearFogOnClick;
			enableClearFogOnClick = GUI.Toggle(new Rect(10, 20, 150, 30), enableClearFogOnClick,
				"Toggle Clear Fog");
			if (enableClearFogOnClick != prev && enableClearFogOnClick)
				enableClearFogCountryOnClick = false;

			prev = enableClearFogCountryOnClick;
			enableClearFogCountryOnClick = GUI.Toggle(new Rect(10, 50, 150, 30),
				enableClearFogCountryOnClick, "Toggle Country Fog");
			if (enableClearFogCountryOnClick != prev && enableClearFogCountryOnClick)
				enableClearFogOnClick = false;

			// buttons background color
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			// Add button to toggle Earth texture
			if (GUI.Button(new Rect(10, 90, 160, 30), "  Reset Fog of War", buttonStyle))
				map.FogOfWarClear(true);
		}

		private void MakeClick(float x, float y)
		{
			if (enableClearFogOnClick) // Smoothly clear fog at position
				map.FogOfWarIncrement(x, y, -0.25f, 0.075f);
			else if (enableClearFogCountryOnClick)
			{
				// Get country index at position
				var position = new Vector2(x, y);
				var countryIndex = map.GetCountryIndex(position);
				if (countryIndex >= 0)
				{
					// Get fog alpha at position and toggle its state
					var alpha = map.FogOfWarGet(x, y);
					if (alpha > 0) // Fog is visible, clear it on entire country
						map.FogOfWarSetCountry(countryIndex, 0);
					else // Fog is clear, show it on entire country
						map.FogOfWarSetCountry(countryIndex, 1);
				}
			}
		}
	}
}