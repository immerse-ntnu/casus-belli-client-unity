using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoStandAlone : MonoBehaviour
	{
		private WMSK map;
		public Texture2D gradient;
		public GameObject lineStartCap;
		public GameObject lineEndCap;
		private GUIStyle labelStyle, labelStyleShadow, buttonStyle, sliderStyle, sliderThumbStyle;
		private ColorPicker colorPicker;
		private bool changingFrontiersColor;
		private bool minimizeState = false;
		private float zoomLevel = 1.0f;
		private bool avoidGUI;

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
			colorPicker = gameObject.GetComponent<ColorPicker>();
			sliderStyle = new GUIStyle();
			sliderStyle.normal.background = Texture2D.whiteTexture;
			sliderStyle.fixedHeight = 4.0f;
			sliderThumbStyle = new GUIStyle();
			sliderThumbStyle.normal.background = Resources.Load<Texture2D>("GUI/thumb");
			sliderThumbStyle.overflow = new RectOffset(0, 0, 8, 0);
			sliderThumbStyle.fixedWidth = 20.0f;
			sliderThumbStyle.fixedHeight = 12.0f;

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

			map.OnDragStart += () => { Debug.Log("Drag starts."); };
			map.OnDragEnd += () => { Debug.Log("Drag ends."); };
		}

		private void OnGUI()
		{
			if (avoidGUI)
				return;

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
				else if (map.countryHighlighted != null)
				{
					text = map.countryHighlighted.name + " (" + map.countryHighlighted.continent + ")";
					var neighbours = map.CountryNeighboursOfCurrentRegion();
					if (neighbours.Count > 0)
						text += "\n" + EntityListToString<Country>(neighbours);
				}
				else
					text = "";
				float x, y;
				if (minimizeState)
				{
					x = Screen.width - 130;
					y = Screen.height - 140;
				}
				else
				{
					x = Screen.width / 2.0f;
					y = Screen.height - 40;
				}
				// shadow
				GUI.Label(new Rect(x - 1, y - 1, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 1, y + 2, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 2, y + 3, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 3, y + 4, 0, 10), text, labelStyleShadow);
				// texst face
				GUI.Label(new Rect(x, y, 0, 10), text, labelStyle);
			}

			// Assorted options to show/hide frontiers, cities, Earth and enable country highlighting
			GUI.Box(new Rect(0, 0, 150, 200), "");
			map.showFrontiers =
				GUI.Toggle(new Rect(10, 20, 150, 30), map.showFrontiers, "Toggle Frontiers");
			map.showEarth = GUI.Toggle(new Rect(10, 50, 150, 30), map.showEarth, "Toggle Earth");
			map.showCities = GUI.Toggle(new Rect(10, 80, 150, 30), map.showCities, "Toggle Cities");
			map.showCountryNames =
				GUI.Toggle(new Rect(10, 110, 150, 30), map.showCountryNames, "Toggle Labels");
			map.enableCountryHighlight = GUI.Toggle(new Rect(10, 140, 170, 30), map.enableCountryHighlight,
				"Enable Highlight");

			// buttons background color
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			// Add button to toggle Earth texture
			if (GUI.Button(new Rect(10, 170, 160, 30), "  Change Earth style", buttonStyle))
				map.earthStyle = (EARTH_STYLE)(((int)map.earthStyle + 1) % 4);

			// Add buttons to show the color picker and change colors for the frontiers or fill
			if (GUI.Button(new Rect(10, 210, 160, 30), "  Change Frontiers Color", buttonStyle))
			{
				colorPicker.showPicker = true;
				changingFrontiersColor = true;
			}
			if (GUI.Button(new Rect(10, 250, 160, 30), "  Change Fill Color", buttonStyle))
			{
				colorPicker.showPicker = true;
				changingFrontiersColor = false;
			}
			if (colorPicker.showPicker)
			{
				if (changingFrontiersColor)
					map.frontiersColor = colorPicker.setColor;
				else
					map.fillColor = colorPicker.setColor;
			}

			// Add a button which demonstrates the navigateTo functionality -- pass the name of a country
			// For a list of countries and their names, check map.Countries collection.
			if (GUI.Button(new Rect(10, 290, 180, 30), "  Fly to Australia (Country)", buttonStyle))
				FlyToCountry("Australia");
			if (GUI.Button(new Rect(10, 325, 180, 30), "  Fly to Mexico (Country)", buttonStyle))
				FlyToCountry("Mexico");
			if (GUI.Button(new Rect(10, 360, 180, 30), "  Fly to New York (City)", buttonStyle))
				FlyToCity("New York", "United States of America");
			if (GUI.Button(new Rect(10, 395, 180, 30), "  Fly to Madrid (City)", buttonStyle))
				FlyToCity("Madrid", "Spain");

			// Slider to show the new set zoom level API in V4.1
			GUI.Button(new Rect(10, 430, 85, 30), "  Zoom Level", buttonStyle);
			var prevZoomLevel = zoomLevel;
			GUI.backgroundColor = Color.white;
			zoomLevel = GUI.HorizontalSlider(new Rect(100, 445, 80, 85), zoomLevel, 0, 1, sliderStyle,
				sliderThumbStyle);
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);
			if (zoomLevel != prevZoomLevel)
			{
				prevZoomLevel = zoomLevel;
				map.SetZoomLevel(zoomLevel);
			}

			if (GUI.Button(new Rect(10, 475, 180, 30), "  Alpha Texture Fill", buttonStyle))
				TransparentTextureFill();

			// Add a button to colorize countries
			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 20, 180, 30),
				"  Colorize Europe", buttonStyle))
				for (var countryIndex = 0; countryIndex < map.countries.Length; countryIndex++)
					if (map.countries[countryIndex].continent.Equals("Europe"))
					{
						var color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f),
							Random.Range(0.0f, 1.0f), 0.5f);
						map.ToggleCountrySurface(countryIndex, true, color);
					}

			// Colorize random country and fly to it
			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 60, 180, 30),
				"  Colorize Random", buttonStyle))
			{
				var countryIndex = Random.Range(0, map.countries.Length);
				var color = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f),
					Random.Range(0.0f, 1.0f));
				map.ToggleCountrySurface(countryIndex, true, color);
				map.BlinkCountry(countryIndex, Color.green, Color.black, 0.8f, 0.2f);
			}

			// Add a button to colorize countries
			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 100, 180, 30),
				"  Colorize Continents", buttonStyle))
			{
				var continentColors = new Dictionary<string, Color>();
				for (var countryIndex = 0; countryIndex < map.countries.Length; countryIndex++)
				{
					var country = map.countries[countryIndex];
					Color continentColor;
					if (continentColors.ContainsKey(country.continent))
						continentColor = continentColors[country.continent];
					else
					{
						continentColor = new Color(Random.Range(0.0f, 1.0f), Random.Range(0.0f, 1.0f),
							Random.Range(0.0f, 1.0f), 0.5f);
						continentColors.Add(country.continent, continentColor);
					}
					map.ToggleCountrySurface(countryIndex, true, continentColor);
				}
			}

			// Button to clear colorized countries
			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 140, 180, 30),
				"  Reset countries", buttonStyle))
				map.HideCountrySurfaces();

			// Tickers sample
			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 180, 180, 30),
				"  Tickers Sample", buttonStyle))
				TickerSample();

			// Decorator sample
			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 220, 180, 30),
				"  Texture Sample", buttonStyle))
				TextureSample();

			// Moving the Earth sample
			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 260, 180, 30),
				"  Toggle Minimize", buttonStyle))
				ToggleMinimize();

			// Add marker sample (Sprite)
			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 300, 180, 30),
				"  Add Marker (2D Sprite)", buttonStyle))
				AddMarkerSpriteOnRandomCity();

			// Add marker sample (Game Object)
			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 340, 180, 30),
				"  Add Marker (Game Object)", buttonStyle))
				AddMarker3DObjectOnRandomCity();

			// Add marker sample (Text)
			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 380, 180, 30),
				"  Add Marker (Text)", buttonStyle))
				AddMarkerTextOnRandomPlace();

			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 420, 180, 30),
				"  Add Trajectories", buttonStyle))
				AddTrajectories();

			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 460, 180, 30),
				"  One Country Borders", buttonStyle))
				ShowCountryBorders();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.G)) // press G to show/hide GUI buttons
				avoidGUI = !avoidGUI;
		}

		// Utility functions called from OnGUI:
		private void FlyToCountry(int countryIndex)
		{
			// Get zoom level for the extents of the country
			var zoomLevel = map.GetCountryRegionZoomExtents(countryIndex);
			map.FlyToCountry(countryIndex, 2.0f, zoomLevel);
			map.BlinkCountry(countryIndex, Color.green, Color.black, 4.0f, 2.5f);
		}

		private void FlyToCountry(string countryName)
		{
			var countryIndex = map.GetCountryIndex(countryName);
			FlyToCountry(countryIndex);
		}

		private void FlyToCity(string cityName, string countryName)
		{
			map.FlyToCity(cityName, countryName, 2.0f, 0.05f);
		}

		// Sample code to show how tickers work
		private void TickerSample()
		{
			map.ticker.ResetTickerBands();

			// Configure 1st ticker band: a red band in the northern hemisphere
			var tickerBand = map.ticker.tickerBands[0];
			tickerBand.verticalOffset = -0.3f;
			tickerBand.backgroundColor = new Color(1, 0, 0, 0.9f);
			tickerBand.scrollSpeed = 0; // static band
			tickerBand.visible = true;
			tickerBand.autoHide = true;

			// Prepare a static, blinking, text for the red band
			var tickerText = new TickerText(0, "WARNING!!");
			tickerText.horizontalOffset = -0.35f;
			tickerText.textColor = Color.yellow;
			tickerText.blinkInterval = 0.2f;
			tickerText.duration = 10.0f;

			// Draw it!
			map.ticker.AddTickerText(tickerText);

			// Configure second ticker band (below the red band)
			tickerBand = map.ticker.tickerBands[1];
			tickerBand.verticalOffset = -0.4f;
			tickerBand.verticalSize = 0.05f;
			tickerBand.backgroundColor = new Color(0, 0, 1, 0.9f);
			tickerBand.visible = true;
			tickerBand.autoHide = true;
			tickerBand.scrollSpeed = -0.15f;

			// Prepare a ticker text
			tickerText = new TickerText(1, "INCOMING MISSILE DETECTED!!");
			tickerText.textColor = Color.white;

			// Draw it!
			map.ticker.AddTickerText(tickerText);
		}

		// Sample code to show how to use decorators to assign a texsture
		private void TextureSample()
		{
			// 1st way (best): assign a flag texture to USA using direct API - this texture will get cleared when you call HideCountrySurfaces()
			var texture = Resources.Load<Texture2D>("Flags/flagUSA");
			var countryIndex = map.GetCountryIndex("United States of America");
			map.ToggleCountrySurface(countryIndex, true, Color.white, texture);

			// 2nd way: assign a flag texture to Brazil using decorator - the texture will stay when you call HideCountrySurfaces()
			var countryName = "Brazil";
			var decorator = new CountryDecorator();
			decorator.isColorized = true;
			decorator.texture = Resources.Load<Texture2D>("Flags/flagBrazil");
			decorator.textureOffset = Misc.Vector2down * 2.4f;
			map.decorator.SetCountryDecorator(0, countryName, decorator);

			Debug.Log("USA flag added with direct API.");
			Debug.Log("Brazil flag added with decorator (persistent texture).");
		}

		// The map can be moved and scaled at will
		private void ToggleMinimize()
		{
			minimizeState = !minimizeState;

			Camera.main.transform.position = Misc.Vector3back * 1.1f;
			Camera.main.transform.rotation = Quaternion.Euler(Misc.Vector3zero);
			if (minimizeState)
			{
				map.earthStyle = EARTH_STYLE.Alternate2;
				map.earthColor = Color.black;
				map.longitudeStepping = 4;
				map.latitudeStepping = 40;
				map.showCities = false;
				map.showCountryNames = false;
				map.showFrontiers = false;
				map.imaginaryLinesColor = new Color(0.06f, 0.23f, 0.398f);
				map.fitWindowWidth = false;
				map.fitWindowHeight = false;
				map.gameObject.transform.localScale = new Vector3(0.4f, 0.2f, 1f);
				map.gameObject.transform.position = new Vector3(0.9f, -0.5f, 0);
			}
			else
			{
				map.gameObject.transform.localScale = new Vector3(200, 100, 1f);
				map.CenterMap();
				map.earthStyle = EARTH_STYLE.Natural;
				map.longitudeStepping = 15;
				map.latitudeStepping = 15;
				map.showCities = true;
				map.showCountryNames = true;
				map.showFrontiers = true;
				map.imaginaryLinesColor = new Color(0.16f, 0.33f, 0.498f);
				map.fitWindowWidth = true;
				map.fitWindowHeight = true;
			}
		}

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

		/// <summary>
		/// Illustrates how to add custom markers over the map using the AddMarker API.
		/// In this example a building prefab is added to a random city (see comments for other options).
		/// </summary>
		private void AddMarkerSpriteOnRandomCity()
		{
			// Every marker is put on a plane-coordinate (in the range of -0.5..0.5 on both x and y)
			Vector2 planeLocation;

			// Add a marker on a random city
			var city = map.cities[Random.Range(0, map.cities.Length)];
			planeLocation = city.unity2DLocation;

			// or... choose a city by its name:
			//		int cityIndex = map.GetCityIndex("Moscow");
			//		planeLocation = map.cities[cityIndex].unity2DLocation;

			// or... use the centroid of a country
			//		int countryIndex = map.GetCountryIndex("Greece");
			//		planeLocation = map.countries[countryIndex].center;

			// or... use a custom location lat/lon. Example put the building over New York:
			//		map.calc.fromLatDec = 40.71f;	// 40.71 decimal degrees north
			//		map.calc.fromLonDec = -74.00f;	// 74.00 decimal degrees to the west
			//		map.calc.fromUnit = UNIT_TYPE.DecimalDegrees;
			//		map.calc.Convert();
			//		planeLocation = map.calc.toPlaneLocation;

			// Send the prefab to the AddMarker API setting a scale of 0.02f (this depends on your marker scales)
			var star = Instantiate(Resources.Load<GameObject>("Sprites/StarSprite"));
			map.AddMarker2DSprite(star, planeLocation, 0.02f);

			// Fly to the destination and see the building created
			map.FlyToLocation(planeLocation);

			// Optionally add a blinking effect to the marker
			MarkerBlinker.AddTo(star, 3, 0.2f);
		}

		/// <summary>
		/// Illustrates how to add custom markers over the map using the AddMarker API.
		/// In this example a building prefab is added to a random city (see comments for other options).
		/// </summary>
		private void AddMarker3DObjectOnRandomCity()
		{
			// Every marker is put on a plane-coordinate (in the range of -0.5..0.5 on both x and y)
			Vector2 planeLocation;

			// Add a marker on a random city
			var city = map.cities[Random.Range(0, map.cities.Length)];
			planeLocation = city.unity2DLocation;

			// or... choose a city by its name:
			//		int cityIndex = map.GetCityIndex("Moscow");
			//		planeLocation = map.cities[cityIndex].unity2DLocation;

			// or... use the centroid of a country
			//		int countryIndex = map.GetCountryIndex("Greece");
			//		planeLocation = map.countries[countryIndex].center;

			// or... use a custom location lat/lon. Example put the building over New York:
			//		map.calc.fromLatDec = 40.71f;	// 40.71 decimal degrees north
			//		map.calc.fromLonDec = -74.00f;	// 74.00 decimal degrees to the west
			//		map.calc.fromUnit = UNIT_TYPE.DecimalDegrees;
			//		map.calc.Convert();
			//		planeLocation = map.calc.toPlaneLocation;

			// Send the prefab to the AddMarker API setting a scale of 0.1f (this depends on your marker scales)
			var tower = Instantiate(Resources.Load<GameObject>("Tower/tower"));

			map.AddMarker3DObject(tower, planeLocation, 1f);

			// Fly to the destination and see the building created
			map.FlyToLocation(planeLocation, 2f, 0.2f);

			// Optionally add a blinking effect to the marker
			MarkerBlinker.AddTo(tower, 3, 0.2f);
		}

		/// <summary>
		/// Illustrates how to add custom markers over the map using the AddMarker API.
		/// In this example a building prefab is added to a random city (see comments for other options).
		/// </summary>
		private void AddMarkerTextOnRandomPlace()
		{
			// Every marker is put on a plane-coordinate (in the range of -0.5..0.5 on both x and y)
			var planeLocation = new Vector2(Random.Range(-0.5f, 0.5f), Random.Range(-0.5f, 0.5f));

			// Add the text
			var tm = map.AddMarker2DText("Random Caption", planeLocation);
			tm.color = Color.yellow;

			// Fly to the destination and see the building created
			map.FlyToLocation(planeLocation, 2f, 0.1f);

			// Optionally add a blinking effect to the marker
			MarkerBlinker.AddTo(tm.gameObject, 3, 0.2f);
		}

		/// <summary>
		/// Example of how to add custom lines to the map
		/// Similar to the AddMarker functionality, you need two spherical coordinates and then call AddLine
		/// </summary>
		private void AddTrajectories()
		{
			map.SetZoomLevel(1f, 1f);

			// In this example we will add random lines from 5 cities to another cities (see AddMaker example above for other options to get locations)

			for (var line = 0; line < 5; line++)
			{
				// Get two random cities
				var city1 = Random.Range(0, map.cities.Length);
				var city2 = Random.Range(0, map.cities.Length);

				// Get their sphere-coordinates
				var start = map.cities[city1].unity2DLocation;
				var end = map.cities[city2].unity2DLocation;

				// Add line with random color, speeds and elevation
				var color = new Color(Random.Range(0.5f, 1), Random.Range(0.5f, 1), Random.Range(0.5f, 1));
				var elevation = Random.Range(0, 0.1f);
				var lineWidth = 0.1f;
				var lma = map.AddLine(start, end, color, elevation, lineWidth);

				// Additional line effects
				lma.drawingDuration = 4.0f;
				lma.autoFadeAfter =
					2.0f; // line stays for 2 seconds, then fades out - set this to zero to avoid line removal

				// Add custom end caps
				lma.startCap = lineStartCap;
				lma.startCapOffset = 0.5f;
				lma.startCapScale = new Vector3(1f, 1f, 1f);
				lma.endCap = lineEndCap;
				lma.endCapOffset = 0.5f;
				lma.endCapScale = new Vector3(1f, 1f, 1f);
			}
		}

		/// <summary>
		/// Renders only the borders of a given country
		/// </summary>
		private void ShowCountryBorders()
		{
			var india = map.GetCountryIndex("India");
			map.earthStyle = EARTH_STYLE.SolidColor;
			map.earthColor = Color.black;
			map.showCities = false;
			map.showCountryNames = false;
			map.RegionErase(map.countries[india].regions, Color.black, true);
			map.Redraw(true);
			var zoomLevel = map.GetCountryRegionZoomExtents(india);
			map.FlyToCountry(india, 0, zoomLevel + 0.02f);
		}

		/// <summary>
		/// Uses a texture with a gradient from pure red to transparent
		/// </summary>
		private void TransparentTextureFill()
		{
			var china = map.GetCountryIndex("China");
			map.ToggleCountryMainRegionSurface(china, true, gradient);
			var zoomLevel = map.GetCountryRegionZoomExtents(china);
			map.FlyToCountry(china, 0, zoomLevel + 0.02f);
		}
	}
}