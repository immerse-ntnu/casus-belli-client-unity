using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoCountryPathFinding : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle labelStyle, labelStyleShadow;
		private int selectStage;
		private int startCountryIndex = -1;

		private void Start()
		{
			// UI Setup - non-important, only for this demo
			labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleLeft;
			labelStyle.normal.textColor = Color.white;
			labelStyleShadow = new GUIStyle(labelStyle);
			labelStyleShadow.normal.textColor = Color.black;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			// WMSK setup
			map = WMSK.instance;
			map.OnCountryClick += HandleOnCountryClick;
			map.OnCountryEnter += HandleOnCountryEnter;

			// Extra: sample code to enable travelling between 2 disconnected countries Indonesia <-> Australia
			// Allow to travel from Indonesia -> Australia
			var indonesia = map.GetCountry("Indonesia");
			var newNeighbours = new List<int>(indonesia.neighbours);
			newNeighbours.Add(map.GetCountryIndex("Australia"));
			map.GetCountry("Indonesia").neighbours = newNeighbours.ToArray();

			// Do the same in reverse direction Australia -> Indonesia
			var australia = map.GetCountry("Australia");
			newNeighbours = new List<int>(australia.neighbours);
			newNeighbours.Add(map.GetCountryIndex("Indonesia"));
			map.GetCountry("Australia").neighbours = newNeighbours.ToArray();
		}

		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			string msg;
			if (selectStage == 0)
				msg = "Select starting country";
			else
				msg = "Move over other country to show countries path";
			GUI.Label(new Rect(11, 11, 300, 20), msg, labelStyleShadow);
			GUI.Label(new Rect(10, 10, 300, 20), msg, labelStyle);
		}

		private void HandleOnCountryEnter(int destinationCountryIndex, int regionIndex)
		{
			if (startCountryIndex >= 0 && startCountryIndex != destinationCountryIndex)
			{
				// Clear existing path
				Refresh();
				// Find a country path between starting country and destination country
				var countriesInPath = map.FindRoute(map.GetCountry(startCountryIndex),
					map.GetCountry(destinationCountryIndex));
				// If a path has been found, paint it!
				if (countriesInPath != null)
					countriesInPath.ForEach(countryIndex =>
						map.ToggleCountrySurface(countryIndex, true, Color.grey));
				else // Otherwise, show it's not possible to reach that country.
					Debug.Log(map.countries[destinationCountryIndex].name +
					          " is not reachable from " +
					          map.countries[startCountryIndex].name +
					          "! You may need to adjust the neighbours property of some countries to enable crossing.");
			}
		}

		private void HandleOnCountryClick(int countryIndex, int regionIndex, int buttonIndex)
		{
			startCountryIndex = countryIndex;
			selectStage = 1;
			Refresh();
		}

		private void Refresh()
		{
			map.HideCountrySurfaces();
			map.ToggleCountrySurface(startCountryIndex, true, Color.blue);
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.A))
				ShowPath();
		}

		private void ShowPath()
		{
			map.pathFindingEnableCustomRouteMatrix = true;
			map.PathFindingCustomRouteMatrixReset();
			map.OnPathFindingCrossPosition += Map_OnPathFindingCrossPosition;

			var fromLocation = map.GetCountry("Russia").center;
			var toLocation = map.GetCountry("Czech Republic").center;

			var path = map.FindRoute(fromLocation, toLocation, TERRAIN_CAPABILITY.OnlyGround,
				maxSearchCost: 1000000, maxSearchSteps: 100000);

			if (path != null)
				map.AddLine(path.ToArray(), Color.red, 0, 0.5f);
		}

		private float Map_OnPathFindingCrossPosition(Vector2 position)
		{
			var countryIndex = map.GetCountryIndex(position);
			var ukraine = map.GetCountryIndex("Ukraine");
			var slovakia = map.GetCountryIndex("Slovakia");
			if (countryIndex == ukraine || countryIndex == slovakia)
				return 1; // basic cost
			return 2; // increased cost through other countries
		}
	}
}