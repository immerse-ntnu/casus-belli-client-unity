using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoPathFinding : MonoBehaviour
	{
		private WMSK map;
		private bool enableToggleOwnership, enableClickToMoveTank = true;
		private GameObjectAnimator tank;
		private List<Country> europeanCountries = new();
		private Color player1Color, player2Color;
		private bool canCross = true;
		private List<GameObject> blocks = new();

		private void Start()
		{
			// Get a reference to the World Map API:
			map = WMSK.instance;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			// Get list of European countries
			europeanCountries = new List<Country>();
			for (var k = 0; k < map.countries.Length; k++)
			{
				var country = map.countries[k];
				if (country.continent.Equals("Europe"))
				{
					europeanCountries.Add(country);
					// Distribute countries between 2 players
					if (country.center.x < 0.04f)
						country.attrib["player"] = 1;
					else
						country.attrib["player"] = 2;
				}
			}

			// Colors
			player1Color = new Color(1, 0.5f, 0, 0.65f);
			player2Color = new Color(0, 0.5f, 1, 0.65f);

			// Setup map rect
			map.windowRect = new Rect(-0.0587777f, 0.1964018f, 0.1939751f, 0.1939751f);
			map.SetZoomLevel(0.1939751f);
			map.CenterMap();

			// Paint countries
			PaintCountries();

			// Drop our tester tank
			DropTankOnCity();

			// On map click listener
			map.OnClick += (float x, float y, int buttonIndex) =>
			{
				if (enableToggleOwnership)
					ChangeCountryOwnerShip(x, y);
				else if (enableClickToMoveTank)
					MoveTankWithPathFinding(new Vector2(x, y));
			};

			// Enable custom cost path finding matrix (we'll setup this matrix when moving the unit)
			map.pathFindingEnableCustomRouteMatrix = true;
			UpdatePathFindingMatrixCost();
		}

		private void Update()
		{
			// Increase duration of the unit path by 10 seconds
			if (Input.GetKeyDown(KeyCode.F))
				tank.ChangeDuration(10f);
			// Decrease duration of the unit path by 10 seconds
			if (Input.GetKeyDown(KeyCode.G))
				tank.ChangeDuration(-10f);
		}

		/// <summary>
		/// UI Buttons
		/// </summary>
		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			// Switches between tank move and change country ownership on click
			var prev = enableToggleOwnership;
			enableToggleOwnership = GUI.Toggle(new Rect(10, 20, 200, 30), enableToggleOwnership,
				"Change Ownership on Click");
			if (enableToggleOwnership && prev != enableToggleOwnership)
				enableClickToMoveTank = false;

			prev = enableClickToMoveTank;
			enableClickToMoveTank = GUI.Toggle(new Rect(230, 20, 150, 30), enableClickToMoveTank,
				"Move Tank On Click");
			if (enableClickToMoveTank && prev != enableClickToMoveTank)
				enableToggleOwnership = false;

			// Show debug info
			map.pathFindingVisualizeMatrixCost = GUI.Toggle(new Rect(500, 20, 200, 30),
				map.pathFindingVisualizeMatrixCost, "Debug Matrix Cost");

			// Block / clear frontier between Spain and France
			if (GUI.Button(new Rect(10, 50, 250, 28), "Block/Clear Spain-France Frontier"))
				ToggleFrontierSpainFrance();
		}

		/// <summary>
		/// Creates a tank instance and adds it to specified city
		/// </summary>
		private void DropTankOnCity()
		{
			// Get a random big city
			var cityIndex = map.GetCityIndex("Paris", "France");

			// Get city location
			var cityPosition = map.cities[cityIndex].unity2DLocation;

			var tankGO = Instantiate(Resources.Load<GameObject>("Tank/CompleteTank"));
			tank = tankGO.WMSK_MoveTo(cityPosition);
			tank.autoRotation = true;
			tank.terrainCapability = TERRAIN_CAPABILITY.OnlyGround;

			// Set tank ownership
			tank.attrib["player"] = 1;
		}

		/// <summary>
		/// Moves the tank with path finding.
		/// </summary>
		private void MoveTankWithPathFinding(Vector2 destination)
		{
			var canMove = false;
			canMove = tank.MoveTo(destination, 0.1f);

			if (!canMove)
				Debug.Log("Can't move to destination!");
		}

		private void ChangeCountryOwnerShip(float x, float y)
		{
			var countryIndex = map.GetCountryIndex(new Vector2(x, y));
			if (countryIndex < 0)
				return;
			var country = map.countries[countryIndex];
			if (country.attrib["player"] == 1)
				country.attrib["player"] = 2;
			else
				country.attrib["player"] = 1;
			UpdatePathFindingMatrixCost(); // need to update cost matrix

			PaintCountries();
			map.BlinkCountry(countryIndex, player1Color, player2Color, 1f, 0.1f);
		}

		private void PaintCountries()
		{
			europeanCountries.ForEach((country) =>
			{
				if (country.attrib["player"] == 1)
					map.ToggleCountrySurface(country.name, true, player1Color);
				else
					map.ToggleCountrySurface(country.name, true, player2Color);
			});
		}

		private void UpdatePathFindingMatrixCost()
		{
			// Setup custom route matrix - first we reset it
			map.PathFindingCustomRouteMatrixReset();

			//  Then set a cost of 0 (unbreakable) on those location belonging to a different player to prevent the tank move over those non-controlled zones.
			int tankPlayer = tank.attrib["player"];
			europeanCountries.ForEach((country) =>
			{
				int countryPlayer = country.attrib["player"];
				if (countryPlayer != tankPlayer)
					map.PathFindingCustomRouteMatrixSet(country, 0);
			});
		}

		private void ToggleFrontierSpainFrance()
		{
			var blockedPositions = map.GetCountryFrontierPoints(map.GetCountryIndex("France"),
				map.GetCountryIndex("Spain"), 1);
			canCross = !canCross;
			map.PathFindingCustomRouteMatrixSet(blockedPositions, canCross ? -1 : 0);
			if (canCross)
			{
				for (var k = 0; k < blocks.Count; k++)
					Destroy(blocks[k]);
				blocks.Clear();
			}
			else
			{
				blocks.Clear();
				foreach (var blockPosition in blockedPositions)
				{
					var o = GameObject.CreatePrimitive(PrimitiveType.Cube);
					map.AddMarker3DObject(o, blockPosition, 0.05f);
					blocks.Add(o);
				}
			}
		}
	}
}