using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class DemoProvinces : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle labelStyle, labelStyleShadow, buttonStyle;

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
			map.OnProvinceEnter += (int provinceIndex, int regionIndex) =>
				Debug.Log("Entered province " + map.provinces[provinceIndex].name);
			map.OnProvinceExit += (int provinceIndex, int regionIndex) =>
				Debug.Log("Exited province " + map.provinces[provinceIndex].name);
			map.OnProvinceClick += (int provinceIndex, int regionIndex, int buttonIndex) =>
				Debug.Log("Clicked province " + map.provinces[provinceIndex].name);

			map.FlyToCountry("France", 3, 0.05f);
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.F))
				ColorizeFrance();
			else if (Input.GetKeyDown(KeyCode.W))
				ColorizeProvinces(true);
			else if (Input.GetKeyDown(KeyCode.R))
				ColorizeProvinces(false);
		}

		// Update is called once per frame
		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			// Check whether a country or city is selected, then show a label with the entity name and its neighbours (new in V4.1!)
			if (map.countryHighlighted != null ||
			    map.cityHighlighted != null ||
			    map.provinceHighlighted != null)
			{
				string text;
				if (map.cityHighlighted != null)
				{
					if (!map.cityHighlighted.name.Equals(map.cityHighlighted
						.province)) // show city name + province & country name
						text = "City: " +
						       map.cityHighlighted.name +
						       " (" +
						       map.cityHighlighted.province +
						       ", " +
						       map.countries[map.cityHighlighted.countryIndex].name +
						       ")";
					else // show city name + country name (city is a capital with same name as province)
						text = "City: " +
						       map.cityHighlighted.name +
						       " (" +
						       map.countries[map.cityHighlighted.countryIndex].name +
						       ")";
				}
				else if (map.provinceHighlighted != null)
				{
					text = map.provinceHighlighted.name + ", " + map.countryHighlighted.name;
					var neighbours = map.ProvinceNeighboursOfCurrentRegion();
					if (neighbours.Count > 0)
						text += "\n" + EntityListToString<Province>(neighbours);
				}
				else
					text = "";
				float x, y;
				x = Screen.width / 2.0f;
				y = Screen.height - 40;

				// shadow
				GUI.Label(new Rect(x - 1, y - 1, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 1, y + 2, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 2, y + 3, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 3, y + 4, 0, 10), text, labelStyleShadow);
				// texst face
				GUI.Label(new Rect(x, y, 0, 10), text, labelStyle);
			}

			// buttons background color
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			// Add a button to colorize countries
			if (GUI.Button(new Rect(10, 20, 180, 30), "  Colorize France", buttonStyle))
				ColorizeFrance();

			if (GUI.Button(new Rect(10, 60, 180, 30), "  Colorize World", buttonStyle))
				ColorizeProvinces(true);

			if (GUI.Button(new Rect(10, 100, 180, 30), "  Reset Provinces", buttonStyle))
				ColorizeProvinces(false);

			if (GUI.Button(new Rect(10, 140, 180, 30), "  Province Border Points", buttonStyle))
				ShowBorderPoints();
		}

		// Utility functions called from OnGUI:
		private string EntityListToString<T>(List<T> entities)
		{
			var sb = new StringBuilder("Neighbours: ");
			for (var k = 0; k < entities.Count; k++)
			{
				if (k > 0)
					sb.Append(", ");
				sb.Append(((IAdminEntity)entities[k]).name);
			}
			return sb.ToString();
		}

		private void ColorizeFrance()
		{
			var country = map.GetCountry("France");
			for (var p = 0; p < country.provinces.Length; p++)
			{
				var color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f),
					Random.Range(0.0f, 1.0f));
				var provinceIndex = map.GetProvinceIndex(country.provinces[p]);
				map.ToggleProvinceSurface(provinceIndex, true, color);
			}
		}

		private void ColorizeProvinces(bool visible)
		{
			for (var p = 0; p < map.provinces.Length; p++)
			{
				var color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f),
					Random.Range(0.0f, 1.0f));
				map.ToggleProvinceSurface(p, visible, color);
			}
		}

		private void ShowBorderPoints()
		{
			var cadizIndex = map.GetProvinceIndex("Spain", "Cádiz");
			var sevilleIndex = map.GetProvinceIndex("Spain", "Sevilla");
			var points = map.GetProvinceBorderPoints(cadizIndex, sevilleIndex);
			points.ForEach((point) => AddBallAtPosition(point));
			if (points.Count > 0)
				map.FlyToLocation(points[0], 2, 0.01f);
		}

		private void AddBallAtPosition(Vector2 pos)
		{
			var ball = GameObject.CreatePrimitive(PrimitiveType.Sphere);
			ball.GetComponent<Renderer>().sharedMaterial.color = Color.yellow;
			map.AddMarker3DObject(ball, pos, 0.03f);
		}
	}
}