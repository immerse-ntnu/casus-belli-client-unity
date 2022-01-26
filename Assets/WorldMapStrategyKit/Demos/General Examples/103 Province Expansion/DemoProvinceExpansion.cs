using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoProvinceExpansion : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle labelStyle, labelStyleShadow, buttonStyle, sliderStyle, sliderThumbStyle;
		private int principalProvinceIndex = -1;
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
			map.OnProvinceClick += (int provinceIndex, int regionIndex, int buttonIndex) =>
			{
				if (autoExpand)
					return;
				if (buttonIndex == 0)
					SelectPrincipalProvince(provinceIndex);
				else
				{
					var region = map.provinces[provinceIndex].regions[regionIndex];
					ExpandProvince(region);
				}
			};
		}

		// Update is called once per frame
		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			// Check whether a province or city is selected, then show a label with the entity name and its neighbours (new in V4.1!)
			if (map.provinceHighlighted != null || map.provinceHighlighted != null)
			{
				string text;
				if (map.provinceHighlighted != null)
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
				// text face
				GUI.Label(new Rect(x, y, 0, 10), text, labelStyle);
			}

			var rect = new Rect(10, 10, 500, 20);
			if (!autoExpand)
			{
				GUI.Box(rect, "");
				GUI.Label(rect,
					"  Left click to select principal province. Right click another province to merge.");
			}

			// buttons background color
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			// Add button to toggle Earth texture
			if (autoExpand)
			{
				var provinceName =
					principalProvinceIndex >= 0 && principalProvinceIndex < map.provinces.Length
						? map.provinces[principalProvinceIndex].name
						: "";
				if (GUI.Button(new Rect(10, 10, 300, 30), "  Stop Expansion of " + provinceName,
					buttonStyle))
					autoExpand = false;
			}
			else
			{
				if (GUI.Button(new Rect(10, 40, 180, 30), "  Automate Expansion", buttonStyle))
				{
					autoExpand = true;
					StartCoroutine(StartExpansion());
				}
				if (GUI.Button(new Rect(10, 75, 180, 30), "  Transfer Provinces Demo", buttonStyle))
					StartCoroutine(TransferProvinces());
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

		private void SelectPrincipalProvince(int newProvinceIndex)
		{
			principalProvinceIndex = newProvinceIndex;
			ColorEmpire();
		}

		private void ColorEmpire()
		{
			map.HideProvinceSurfaces();
			map.ToggleProvinceSurface(principalProvinceIndex, true, Color.green);
		}

		private void ExpandProvince(Region region)
		{
			if (principalProvinceIndex < 0 || region == null)
				return;

			// Store principal province id which is immutable - province index will change because the provinces array is modified after one province merges another (one of them disappears).
			var provinceUniqueId = map.provinces[principalProvinceIndex].uniqueId;
			var provinceName = region.entity.name;

			if (map.ProvinceTransferProvinceRegion(principalProvinceIndex, region, true))
			{
				// Restore principal id before provinces array changed
				SelectPrincipalProvince(map.GetProvinceIndex(provinceUniqueId));
				Debug.Log("Province " +
				          provinceName +
				          " conquered by " +
				          map.provinces[principalProvinceIndex].name +
				          "!");
			}
		}

		private IEnumerator StartExpansion()
		{
			// Iterates all neighbour of current selected province and conquers it
			if (principalProvinceIndex < 0 || principalProvinceIndex >= map.provinces.Length)
				principalProvinceIndex = 0;
			// Take a neighbour region and conquer it
			var regionFound = true;
			paused = false;
			Debug.Log("*** AUTOEXPANSION STARTED ***");
			while (regionFound)
			{
				regionFound = false;
				if (principalProvinceIndex < 0 || principalProvinceIndex >= map.provinces.Length)
					continue;
				var province = map.provinces[principalProvinceIndex];
				if (province.regions == null)
					map.ReadProvincePackedString(province);
				if (province.regions == null)
					continue;
				for (var r = 0; r < province.regions.Count; r++)
				{
					var region = province.regions[r];
					for (var n = 0; n < region.neighbours.Count; n++)
					{
						if (!autoExpand)
							yield break;
						var otherRegion = region.neighbours[n];
						var color = map.GetRegionColor(otherRegion);
						if (color != Color.green)
						{
							ExpandProvince(otherRegion);
							regionFound = true;
							do
								yield return new WaitForSeconds(0.2f);
							while (paused);
							break; // need to restart search since current province regions have changed
						}
					}
				}
			}
			autoExpand = false;
			Debug.Log("*** AUTOEXPANSION FINISHED ***");
		}

		private IEnumerator TransferProvinces()
		{
			// Reset map
			map.ReloadData();
			map.Redraw();

			// Transfer some German provinces to Poland
			var countryIndex = map.GetCountryIndex("Poland");

			// Step 1: Focus on area of provinces
			map.showProvinces = true;
			map.drawAllProvinces = true;
			map.FlyToProvince("Germany", "Brandenburg", 1f, 0.04f);
			yield return new WaitForSeconds(1f);

			// Step 2: Mark provinces
			var provincesToTransfer = new string[]
			{
				"Brandenburg",
				"Mecklenburg-Vorpommern",
				"Sachsen-Anhalt",
				"Sachsen",
				"Thüringen"
			};
			foreach (var provinceName in provincesToTransfer)
			{
				var provinceIndex = map.GetProvinceIndex("Germany", provinceName);
				map.BlinkProvince(provinceIndex, Color.white, Color.red, 2f, 0.15f);
				var lma = map.AddLine(new Vector2[]
				{
					map.provinces[provinceIndex].center,
					map.countries[countryIndex].center
				}, Color.yellow, 1f, 0.15f);
				lma.dashInterval = 0.0001f;
				lma.dashAnimationDuration = 0.3f;
				lma.drawingDuration = 2.5f;
				lma.autoFadeAfter = 0.5f;
				lma.fadeOutDuration = 0f;
			}
			yield return new WaitForSeconds(3f);

			// Step 3: transfer some German provinces to Poland
			foreach (var provinceName in provincesToTransfer)
			{
				var province = map.GetProvince(provinceName, "Germany");
				if (!map.CountryTransferProvinceRegion(countryIndex, province.mainRegion, false))
					Debug.LogError("Could not transfer province " + provinceName + " to Poland.");
			}
			map.Redraw();

			// End

			map.FlyToCountry("Poland", 1f, 0.04f);
			map.BlinkCountry("Poland", Color.white, Color.green, 2f, 0.15f);

			Debug.Log("Done.");
		}
	}
}