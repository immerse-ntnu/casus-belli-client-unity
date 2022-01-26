using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoCountryExpansion : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle labelStyle, labelStyleShadow, buttonStyle, sliderStyle, sliderThumbStyle;
		private int principalCountryIndex = -1;
		private bool autoExpand;
		private bool paused;

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

			// Listen to map events
			map.OnClick += (float x, float y, int buttonIndex) =>
			{
				if (autoExpand)
					return;
				if (buttonIndex == 0)
				{
					var countryIndex = map.GetCountryIndex(new Vector2(x, y));
					SelectPrincipalCountry(countryIndex);
				}
				else
				{
					var region = map.GetCountryRegion(new Vector2(x, y));
					ExpandCountry(region);
				}
			};
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
				x = Screen.width / 2.0f;
				y = Screen.height - 40;

				// shadow
				GUI.Label(new Rect(x - 1, y - 1, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 1, y + 2, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 2, y + 3, 0, 10), text, labelStyleShadow);
				GUI.Label(new Rect(x + 3, y + 4, 0, 10), text, labelStyleShadow);
				// text face
				GUI.Label(new Rect(x, y, 0, 10), text, labelStyle);
			}

			var rect = new Rect(10, 10, 500, 20);
			if (!autoExpand)
			{
				GUI.Box(rect, "");
				GUI.Label(rect,
					"  Left click to select principal country. Right click another country to merge.");
			}

			// buttons background color
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			// Add button to toggle Earth texture
			if (autoExpand)
			{
				var countryName =
					principalCountryIndex >= 0 && principalCountryIndex < map.countries.Length
						? map.countries[principalCountryIndex].name
						: "";
				if (GUI.Button(new Rect(10, 10, 300, 30), "  Stop Expansion of " + countryName,
					buttonStyle))
					autoExpand = false;
			}
			else
			{
				if (GUI.Button(new Rect(10, 40, 160, 30), "  Automate Expansion", buttonStyle))
				{
					autoExpand = true;
					StartCoroutine(StartExpansion());
				}
				if (GUI.Button(new Rect(10, 75, 160, 30), "  Transfer Country Demo", buttonStyle))
					StartCoroutine(TransferCountry());
				if (GUI.Button(new Rect(10, 110, 80, 30), "  Reset Map", buttonStyle))
				{
					map.ReloadData();
					map.Redraw();
				}
			}
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.Space))
				paused = !paused;
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

		private void SelectPrincipalCountry(int newCountryIndex)
		{
			principalCountryIndex = newCountryIndex;
			ColorEmpire();
		}

		private void ColorEmpire()
		{
			map.HideCountrySurfaces();
			map.ToggleCountrySurface(principalCountryIndex, true, Color.green);
		}

		private void ExpandCountry(Region region)
		{
			if (principalCountryIndex < 0 || region == null)
				return;

			// Store principal country id which is immutable - country index will change because the countries array is modified after one country merges another (one of them disappears).
			var countryUniqueId = map.countries[principalCountryIndex].uniqueId;
			var countryName = region.entity.name;

			if (map.CountryTransferCountryRegion(principalCountryIndex, region, true))
			{
				// Restore principal id before countries array changed
				SelectPrincipalCountry(map.GetCountryIndex(countryUniqueId));
				Debug.Log("Country " +
				          countryName +
				          " conquered by " +
				          map.countries[principalCountryIndex].name +
				          "!");
			}
		}

		private IEnumerator StartExpansion()
		{
			// Iterates all neighbour of current selected country and conquers it
			if (principalCountryIndex < 0 || principalCountryIndex >= map.countries.Length)
				principalCountryIndex = 0;
			// Take a neighbour region and conquer it
			var regionFound = true;
			paused = false;
			Debug.Log("*** AUTOEXPANSION STARTED ***");
			while (regionFound)
			{
				regionFound = false;
				var country = map.countries[principalCountryIndex];
				for (var r = 0; r < country.regions.Count; r++)
				{
					var region = country.regions[r];
					for (var n = 0; n < region.neighbours.Count; n++)
					{
						if (!autoExpand)
							yield break;
						var otherRegion = region.neighbours[n];
						var color = map.GetRegionColor(otherRegion);
						if (color != Color.green)
						{
							ExpandCountry(otherRegion);
							regionFound = true;
							do
								yield return new WaitForSeconds(0.2f);
							while (paused);
							break; // need to restart search since current country regions have changed
						}
					}
				}
			}
			autoExpand = false;
			Debug.Log("*** AUTOEXPANSION FINISHED ***");
		}

		private IEnumerator TransferCountry()
		{
			// Reset map
			map.ReloadData();
			map.Redraw();

			// Countries in action
			var targetCountry = "Czech Republic";
			var sourceCountry = "Slovakia";
			var targetCountryIndex = map.GetCountryIndex(targetCountry);
			var sourceCountryIndex = map.GetCountryIndex(sourceCountry);
			if (sourceCountryIndex < 0 || targetCountryIndex < 0)
			{
				Debug.Log("Countries not found.");
				yield break;
			}

			// Step 1: Mark countries
			map.FlyToCountry(sourceCountry, 1f, 0.05f);
			map.BlinkCountry(sourceCountry, Color.white, Color.red, 2f, 0.15f);
			yield return new WaitForSeconds(1f);

			// Step 2: Add animated line
			var lma = map.AddLine(new Vector2[]
			{
				map.countries[sourceCountryIndex].center,
				map.countries[targetCountryIndex].center
			}, Color.yellow, 0f, 0.15f);
			lma.dashInterval = 0.0005f;
			lma.dashAnimationDuration = 0.3f;
			lma.drawingDuration = 2.5f;
			lma.autoFadeAfter = 0.5f;
			lma.fadeOutDuration = 0f;
			yield return new WaitForSeconds(3f);

			// Step 3: Transfer Slovakia to Czech Republic
			var sourceRegion = map.GetCountry("Slovakia").mainRegion;
			if (!map.CountryTransferCountryRegion(targetCountryIndex, sourceRegion, false))
			{
				Debug.LogError("Country could not be transferred.");
				yield break;
			}

			// Step 4: rename Czech Republic to Czechoslovakia
			if (!map.CountryRename("Czech Republic", "Czechoslovakia"))
				Debug.LogError("Country could not be renamed.");

			// Step 5: refresh any change on screen and highlight the new country
			map.Redraw();
			map.BlinkCountry("Czechoslovakia", Color.white, Color.blue, 2f, 0.15f);
		}
	}
}