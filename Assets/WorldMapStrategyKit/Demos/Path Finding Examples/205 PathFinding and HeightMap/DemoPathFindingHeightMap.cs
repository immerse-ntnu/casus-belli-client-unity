using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class DemoPathFindingHeightMap : MonoBehaviour
	{
		public Texture2D roadTexture;
		private WMSK map;
		private GUIStyle labelStyle, buttonStyle;
		private int startCellIndex;
		private GameObjectAnimator tank;
		private List<int> path;
		private float pathCost;
		private List<TextMesh> texts = new();

		private void Start()
		{
			// UI Setup - non-important, only for this demo
			labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleLeft;
			labelStyle.normal.textColor = Color.yellow;
			buttonStyle = new GUIStyle(labelStyle);
			buttonStyle.alignment = TextAnchor.MiddleCenter;
			buttonStyle.normal.background = Texture2D.whiteTexture;
			buttonStyle.normal.textColor = Color.black;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			// WMSK setup
			map = WMSK.instance;
			map.OnCellClick += HandleOnCellClick;
			map.OnCellEnter += HandleOnCellEnter;

			// Focus on Berlin
			var city = map.GetCity("Berlin", "Germany");
			map.FlyToCity(city, 1f, 0.1f);

			// Creates a tank and positions it on the center of the hexagonal cell which contains Berlin
			var startCell = map.GetCell(city.unity2DLocation);
			DropTankOnPosition(startCell.center);
			startCellIndex = map.GetCellIndex(startCell);

			// Paint some country costs
			PaintCountries();
		}

		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			var msg = "Select destination cell!";
			GUI.Label(new Rect(10, 10, 300, 20), msg, labelStyle);

			if (map.cellHighlightedIndex >= 0)
			{
				msg = "Current cell: " + map.cellHighlightedIndex;
				GUI.Label(new Rect(10, 30, 300, 20), msg, labelStyle);
			}

			if (GUI.Button(new Rect(10, 60, 100, 20), "Draw Barrier", buttonStyle))
			{
				// Very basic sample of how to draw some lines to create a visual barrier over the edges of some hexagonal cells
				map.AddLine(101133, CELL_SIDE.Top, Color.cyan, 0.1f);
				map.AddLine(101134, CELL_SIDE.TopLeft, Color.cyan, 0.1f);
				map.AddLine(101134, CELL_SIDE.Top, Color.cyan, 0.1f);
				map.AddLine(101134, CELL_SIDE.TopRight, Color.cyan, 0.1f);
				map.AddLine(101135, CELL_SIDE.Top, Color.cyan, 0.1f);
				map.AddLine(101136, CELL_SIDE.TopLeft, Color.cyan, 0.1f);
				map.AddLine(101136, CELL_SIDE.Top, Color.cyan, 0.1f);
				map.AddLine(101136, CELL_SIDE.TopRight, Color.cyan, 0.1f);
				// Set crossing costs for barrier for each edge. Note that here I'm using the hard coded cell numbers for this example. If you change the number of rows or columns of the grid
				// this will obviously fail.
				var cost = 10000;
				map.PathFindingCellSetSideCost(101133, 101645, cost);
				map.PathFindingCellSetSideCost(101134, 101645, cost);
				map.PathFindingCellSetSideCost(101134, 101646, cost);
				map.PathFindingCellSetSideCost(101134, 101647, cost);
				map.PathFindingCellSetSideCost(101135, 101647, cost);
				map.PathFindingCellSetSideCost(101136, 101647, cost);
				map.PathFindingCellSetSideCost(101136, 101648, cost);
				map.PathFindingCellSetSideCost(101136, 101649, cost);
			}

			if (GUI.Button(new Rect(130, 60, 120, 20), "Show Mountains", buttonStyle))
				ShowMountains();

			GUI.Label(new Rect(10, 100, 250, 30), "Non colored terrain cost: 1 point");
			GUI.Label(new Rect(10, 120, 250, 30), "Green movement cost: 2 point");
			GUI.Label(new Rect(10, 140, 250, 30), "Gray movement cost: 3 points");
			GUI.Label(new Rect(10, 160, 250, 30), "Press R to show movement range.");

			if (tank.maxSearchCost > 5 || (int)Time.time % 2 != 0)
				GUI.Label(new Rect(10, 180, 250, 30), "Tank move points: " + tank.maxSearchCost);
			if (tank.maxSearchCost < 5)
				GUI.Label(new Rect(10, 200, 250, 30), "Press M to add more move points.");

			if (GUI.Button(new Rect(270, 60, 120, 20), "  Build Road", buttonStyle))
				BuildRoad();
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.M))
				tank.maxSearchCost += 10;

			if (Input.GetKeyDown(KeyCode.R))
				ShowMoveRange();
		}

		private void HandleOnCellEnter(int destinationCellIndex)
		{
			if (!tank.isMoving && startCellIndex >= 0 && startCellIndex != destinationCellIndex)
			{
				// Clear existing path
				ClearPreviousPath();
				// Find a cell path between starting cell and destination cell, only over ground, at any altitude and with a maximum traversing cost of tank.maxSearchCost
				path = map.FindRoute(map.GetCell(startCellIndex), map.GetCell(destinationCellIndex),
					out pathCost, tank.terrainCapability, tank.maxSearchCost, tank.minAltitude,
					tank.maxAltitude);
				// If a path has been found, paint it!
				if (path != null)
					ShowPathAndCosts(path);
				else // Otherwise, show it's not possible to reach that cell.
					Debug.Log("Cell #" +
					          destinationCellIndex +
					          " is not reachable from cell #" +
					          startCellIndex);
			}
		}

		private void HandleOnCellClick(int cellIndex, int buttonIndex)
		{
			if (path != null)
			{
				startCellIndex = cellIndex;
				tank.MoveTo(path, 0.5f);
				tank.maxSearchCost -= pathCost;
				ClearPreviousPath();
			}
		}

		private void ClearPreviousPath()
		{
			map.RestoreCellMaterials();
			texts.ForEach((t) => Destroy(t.gameObject));
			texts.Clear();
			path = null;
		}

		private void PaintCountries()
		{
			// Color some countries to show different cross cost
			var cellsInCountry = map.GetCellsInCountry(map.GetCountryIndex("France"));
			for (var k = 0; k < cellsInCountry.Count; k++)
			{
				map.ToggleCellSurface(cellsInCountry[k], true, new Color(1, 1, 1, 0.25f));
				map.PathFindingCellSetAllSidesCost(cellsInCountry[k], 3);
			}
			cellsInCountry = map.GetCellsInCountry(map.GetCountryIndex("Germany"));
			for (var k = 0; k < cellsInCountry.Count; k++)
			{
				map.ToggleCellSurface(cellsInCountry[k], true, new Color(0, 1, 0, 0.25f));
				map.PathFindingCellSetAllSidesCost(cellsInCountry[k], 2);
			}
		}

		// Create tank instance and add it to the map
		private GameObjectAnimator DropTankOnPosition(Vector2 mapPosition)
		{
			var tankGO = Instantiate(Resources.Load<GameObject>("Tank/CompleteTank"));
			tankGO.transform.localScale = Misc.Vector3one * 0.25f;
			tank = tankGO.WMSK_MoveTo(mapPosition);
			tank.autoRotation = true;
			tank.terrainCapability = TERRAIN_CAPABILITY.OnlyGround;
			tank.maxSearchCost = 10;
			tank.maxAltitude = 0.1f;
			return tank;
		}

		private void ShowMoveRange()
		{
			var cellIndex = map.GetCellIndex(tank.currentMap2DLocation);
			if (cellIndex < 0)
				return;
			var cells = tank.GetCellNeighbours();
			map.CellBlink(cells, Color.blue, 1f);
		}

		private void ShowPathAndCosts(List<int> path)
		{
			var steps = path.Count;

			var pathColor = new Color(0.5f, 0.5f, 0, 0.5f);
			for (var k = 1; k < steps; k++)
			{
				// ignore step 0 since this is current tank cell
				var cellIndex = path[k];

				// Color path cells
				map.SetCellTemporaryColor(cellIndex, pathColor);

				// Show the accumulated cost
				var accumCost = map.GetCellPathCost(cellIndex);
				Vector3 cellPosition = map.GetCellPosition(cellIndex);
				var text = map.AddMarker2DText(accumCost.ToString(), cellPosition);
				text.transform.localScale *= 0.3f; // make font smaller
				texts.Add(text);
			}
		}

		/// <summary>
		/// Gets a list of cells with height > 0.1f and colors them in white
		/// </summary>
		private void ShowMountains()
		{
			var highCells = map.GetCellsByAltitude(0.1f, 1f);
			var cellCount = highCells.Count;
			var color = new Color(1f, 1f, 1f, 0.8f);
			for (var k = 0; k < cellCount; k++)
			{
				var cellIndex = map.GetCellIndex(highCells[k]);
				map.ToggleCellSurface(cellIndex, color);
			}
		}

		private void BuildRoad()
		{
			var city1Index = map.GetCityIndex("Berlin", "Germany");
			var city2Index = map.GetCityIndex("Paris", "France");
			// Get starting and end cell
			var cell1 = map.GetCell(map.GetCity(city1Index).unity2DLocation);
			var cell2 = map.GetCell(map.GetCity(city2Index).unity2DLocation);
			// Get the path
			var cellIndices = map.FindRoute(cell1, cell2, TERRAIN_CAPABILITY.OnlyGround);
			if (cellIndices == null)
				return;

			// Highlight road cells
			map.CellBlink(cellIndices, new Color(1, 1, 0.5f, 0.5f), 1f);

			// Get coordinates for the path
			var positionsCount = cellIndices.Count;
			var positions = new Vector2[positionsCount];
			for (var k = 0; k < positionsCount; k++)
				positions[k] = map.cells[cellIndices[k]].center;

			// Build a road along the map coordinates
			var lma = map.AddLine(positions, Color.white, 0, 0.1f);
			lma.lineMaterial.mainTexture = roadTexture;
			lma.lineMaterial.mainTextureScale = new Vector2(8f, 1f);

			// Apply reduced costs to hex sides crossed by the road
			for (var k = 1; k < positionsCount; k++)
				map.PathFindingCellSetSideCost(cellIndices[k - 1], cellIndices[k], 1);
		}
	}
}