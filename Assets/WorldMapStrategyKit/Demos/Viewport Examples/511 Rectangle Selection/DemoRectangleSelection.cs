using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class DemoRectangleSelection : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle labelStyle;
		private List<GameObjectAnimator> tanks;
		private List<GameObjectAnimator> selectedUnits;

		private void Start()
		{
			// setup GUI resizer - only for the demo
			labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleLeft;
			labelStyle.normal.textColor = Color.white;
			GUIResizer.Init(800, 500);

			// Get a reference to the World Map API:
			map = WMSK.instance;
			map.renderViewportGOAutoScaleMax = 4f;
			map.renderViewportGOAutoScaleMin = 1f;

			// Create tanks
			const int NUM_TANKS = 5;
			const float DISTANCE = 0.004f;
			tanks = new List<GameObjectAnimator>(NUM_TANKS);
			var locationBase = map.GetCity("Paris", "France").unity2DLocation;
			for (var k = 0; k < NUM_TANKS; k++)
			{
				// initial position for tank
				var pos = locationBase +
				          new Vector2(Random.value - 0.5f, Random.value - 0.5f).normalized * DISTANCE;
				var tries = 0;
				// is another tank on this position?
				while (tries++ < 100 && map.VGOGet(pos, 0.005f) != null)
					pos = locationBase +
					      new Vector2(Random.value - 0.5f, Random.value - 0.5f).normalized *
					      Random.value *
					      DISTANCE;
				// drop tank there
				var tank = DropTankOnPosition(pos);
				tanks.Add(tank);
			}
			selectedUnits = new List<GameObjectAnimator>();

			// Fly to Paris
			map.FlyToLocation(locationBase, 2f, 0.05f);
		}

		/// <summary>
		/// UI Buttons
		/// </summary>
		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			if (map.rectangleSelectionInProgress)
				GUI.Box(new Rect(10, 10, 600, 40), "Drag and release to make a selection.", labelStyle);
			else
			{
				GUI.Box(new Rect(10, 10, 600, 40),
					"Hold down LEFT SHIFT to initiate a rectangle selection.", labelStyle);
				if (selectedUnits.Count > 0)
					GUI.Box(new Rect(10, 25, 600, 40), "Press C to clear current selection.", labelStyle);
			}
		}

		// Create tank instance and add it to the map
		private GameObjectAnimator DropTankOnPosition(Vector2 mapPosition)
		{
			var tankGO = Instantiate(Resources.Load<GameObject>("Tank/CompleteTank"));
			var tank = tankGO.WMSK_MoveTo(mapPosition);
			tank.autoRotation = true;
			tank.attrib["Color"] = tank.GetComponentInChildren<Renderer>().sharedMaterial.color;
			return tank;
		}

		private void Update()
		{
			if (Input.GetKeyDown(KeyCode.LeftShift))
			{
				var rectangleFillColor = new Color(1f, 1f, 1f, 0.38f);
				var rectangleLineColor = Color.green;
				map.RectangleSelectionInitiate(rectangleSelectionCallback, rectangleFillColor,
					rectangleLineColor);
			}
			if (Input.GetKeyDown(KeyCode.C))
				ClearCurrentSelection();
		}

		private void rectangleSelectionCallback(Rect rect, bool finishRectangleSelection)
		{
			if (finishRectangleSelection)
			{
				selectedUnits = map.VGOGet(rect);
				if (selectedUnits.Count > 0)
					foreach (var go in selectedUnits)
						go.GetComponentInChildren<Renderer>().material.color = Color.blue;
				else
					ClearCurrentSelection();
			}
		}

		private void ClearCurrentSelection()
		{
			foreach (var go in selectedUnits)
				go.GetComponentInChildren<Renderer>().material.color = go.attrib["Color"];
			selectedUnits.Clear();
		}
	}
}