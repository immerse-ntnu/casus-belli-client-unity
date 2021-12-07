using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class DemoInfiniteScroll : MonoBehaviour
	{
		private WMSK map;
		private GameObjectAnimator ship;
		private GUIStyle buttonStyle, labelStyle;
		private GameObject trailParent;

		private void Start()
		{
			// Get a reference to the World Map API:
			map = WMSK.instance;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			// setup GUI styles
			labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleCenter;
			labelStyle.normal.textColor = Color.white;
			buttonStyle = new GUIStyle(labelStyle);
			buttonStyle.alignment = TextAnchor.MiddleLeft;
			buttonStyle.normal.background = Texture2D.whiteTexture;
			buttonStyle.normal.textColor = Color.white;

			// Listen to click event to move the ship around
			map.OnClick += (float x, float y, int buttonIndex) =>
			{
				ship.MoveTo(new Vector2(x, y), 0.1f);
			};

			LaunchShip();
		}

		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			GUI.Label(new Rect(10, 15, 500, 30),
				"Wrap horizontally enabled to allow infinite horizontal scrolling. Move ship around.");

			// buttons background color
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			// Add button to toggle Earth texture
			if (GUI.Button(new Rect(10, 40, 130, 30), "  Locate Ship", buttonStyle))
				map.FlyToLocation(ship.currentMap2DLocation, 2f, 0.05f);
			if (GUI.Button(new Rect(10, 80, 130, 30), "  Reposition Ship", buttonStyle))
				LaunchShip();
			if (GUI.Button(new Rect(10, 120, 130, 30), "  Clear Trail", buttonStyle))
				if (trailParent != null)
					Destroy(trailParent);
		}

		/// <summary>
		/// Creates ship. Main function called from button UI.
		/// </summary>
		private void LaunchShip()
		{
			if (ship != null)
				DestroyImmediate(ship.gameObject);

			// Get a coastal city and a water entrypoint
			var cityIndex =
				(Time.frameCount + map.cities.Length) %
				map.cities.Length; // Random.Range (0, map.cities.Length);
			Vector2 cityPosition;
			var waterPosition = Misc.Vector2zero;
			var safeAbort = 0;
			do
			{
				cityIndex++;
				if (cityIndex >= map.cities.Length)
					cityIndex = 0;
				cityPosition = map.cities[cityIndex].unity2DLocation;
				if (safeAbort++ > 8000)
					break;
			} while (!map.ContainsWater(cityPosition, 0.001f, out waterPosition));

			if (safeAbort > 8000)
			{
				Debug.Log("No water position found!");
				return;
			}

			// Create ship
			ship = DropShipOnPosition(waterPosition);

			// Fly to the location of ship with provided zoom level
			map.FlyToLocation(waterPosition, 2.0f, 0.1f);
		}

		/// <summary>
		/// Creates a new ship on position.
		/// </summary>
		private GameObjectAnimator DropShipOnPosition(Vector2 position)
		{
			// Create ship
			var shipGO = Instantiate(Resources.Load<GameObject>("Ship/VikingShip"));
			ship = shipGO.WMSK_MoveTo(position);
			ship.terrainCapability = TERRAIN_CAPABILITY.OnlyWater;
			ship.autoRotation = true;
			return ship;
		}

		private Vector3 lastPosition;

		private void Update()
		{
			if (ship == null)
				return;

			var d = Vector3.Distance(ship.currentMap2DLocation, lastPosition);
			if (d > 0.002f)
			{
				if (ship.isMoving)
				{
					if (trailParent == null)
					{
						trailParent = new GameObject("Trail");
						trailParent
							.WMSK_MakeChild(); // makes this placeholder part of the map so it scrolls with it properly. All path points will be parented to this placeholder so they can be deleted just by removing this placeholder.
					}
					var sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
					sphere.transform.SetParent(trailParent.transform, false);
					sphere.WMSK_MoveTo(lastPosition, false);
				}
				lastPosition = ship.currentMap2DLocation;
			}

			if (Input.GetKeyDown(KeyCode.S))
				ship.Stop();
		}
	}
}