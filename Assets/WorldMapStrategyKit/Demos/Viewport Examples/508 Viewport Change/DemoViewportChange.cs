/*

Normal <-> Viewport Modes Switch Demo

Important considerations:

In normal mode, the main camera moves around the map.
In viewport mode, the main camera is free, not moved, WMSK uses a secondary camera to render the map as a RenderTexture over the viewport mesh.

Because of that, in normal mode we usually don't worry about the camera position. It will center on the map, clamp on edges if Fit to Window/Height is enabled, etc.
But when switching to Viewport Mode the main camera is "released" so we reposition it in front of the viewport.

We use a simple plane with a Standard Fade Shader to smooth transition from one view to the other.
*/

using System.Collections;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoViewportChange : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle buttonStyle;
		private GameObject viewport, fadePlane;
		private GameObjectAnimator tank;
		private Material fadeMaterial;
		private float fadeStartTime;
		private Vector2 kathmanduLocation, beijingLocation;

		private void Start()
		{
			// Get a reference to the World Map API:
			map = WMSK.instance;

			// Get a reference to the viewport gameobject (we'll enable/disable it when changing modes)
			viewport = GameObject.Find("Viewport");

			// Get the material of the fade plane
			fadePlane = Camera.main.transform.Find("FadePlane").gameObject;
			fadeMaterial = fadePlane.GetComponent<Renderer>().sharedMaterial;

			// UI Setup - non-important, only for this demo
			buttonStyle = new GUIStyle();
			buttonStyle.alignment = TextAnchor.MiddleCenter;
			buttonStyle.normal.background = Texture2D.whiteTexture;
			buttonStyle.normal.textColor = Color.white;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			map.CenterMap();

			// Create tank
			kathmanduLocation = map.GetCity("Kathmandu", "Nepal").unity2DLocation;
			beijingLocation = map.GetCity("Beijing", "China").unity2DLocation;
			tank = DropTankOnPosition(kathmanduLocation);

			// Start movement
			tank.MoveTo(beijingLocation, 0.1f);
			tank.OnMoveEnd += anim => SwitchDestination();
		}

		private void SwitchDestination()
		{
			if (tank.isNear(beijingLocation))
				tank.MoveTo(kathmanduLocation, 0.1f);
			else
				tank.MoveTo(beijingLocation, 0.1f);
		}

		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			GUI.Box(new Rect(10, 10, 365, 75),
				"Fly to operations with viewport switching during movement.");

			// buttons background color
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			if (map.renderViewportIsEnabled)
			{
				if (GUI.Button(new Rect(85, 40, 100, 30), "2D Mode", buttonStyle))
					FlyZoomOut();
			}
			else
			{
				if (GUI.Button(new Rect(85, 40, 100, 30), "Viewport Mode", buttonStyle))
					FollowTankZoomIn();
			}

			if (tank.isMoving)
			{
				if (GUI.Button(new Rect(205, 40, 100, 30), "Stop Tank", buttonStyle))
					tank.Stop();
			}
			else
			{
				if (GUI.Button(new Rect(205, 40, 100, 30), "Resume", buttonStyle))
					tank.MoveTo(tank.endingMap2DLocation, 0.1f);
			}
		}

		// Create tank instance and add it to the map
		private GameObjectAnimator DropTankOnPosition(Vector2 mapPosition)
		{
			var tankGO = Instantiate(Resources.Load<GameObject>("Tank/CompleteTank"));
			tankGO.transform.localScale = Vector3.one * 0.25f; // make tank smaller

			// Add tank to the viewport
			var tank = tankGO.WMSK_MoveTo(mapPosition);
			tank.autoRotation = true;
			tank.terrainCapability = TERRAIN_CAPABILITY.OnlyGround;
			return tank;
		}

		// Initiates a fly to operation to target country.
		// During flight, we switch off and later on the viewport reference
		// This can be useful to switch from a global map view to a localized 3D view
		// The double switch here is just for demo/testing purposes.
		// You can assign/remove the viewport reference as you need
		private void FollowTankZoomIn()
		{
			tank.follow = true;
			tank.followZoomLevel = 0.05f;
			tank.followDuration = 4f;

			// Before switching view mode, initiates fade to smooth transition effect
			Invoke(nameof(StartFade), 1.8f);

			// After 2.5 seconds of flight, switch to viewport mode. You could also check current zoomLevel in the Update() method and change accordingly.
			Invoke(nameof(EnableViewport), 2.5f);
		}

		private void FlyZoomOut()
		{
			tank.follow = false;
			map.FlyToLocation(tank.currentMap2DLocation, 4f, 0.2f);

			// Before switching view mode, initiates fade to smooth transition effect
			Invoke(nameof(StartFade), 0.4f);

			// After 1 second of flight, switch to normal mode. You could also check current zoomLevel in the Update() method and change accordingly.
			Invoke(nameof(DisableViewport), 1f);
		}

		private void EnableViewport()
		{
			map.renderViewport = viewport; // <---- switch to viewport mode

			// The camera is free in viewport mode, so we move it in front of the viewport and look at it
			Camera.main.transform.position = viewport.transform.position - Vector3.forward * 100f;
			Camera.main.transform.LookAt(viewport.transform);
		}

		private void DisableViewport()
		{
			map.renderViewport = null; // <--- switch to normal map view
		}

		private void StartFade()
		{
			fadeStartTime = Time.time;
			fadePlane.SetActive(true);
			StartCoroutine(DoFade());
		}

		private IEnumerator DoFade()
		{
			const float duration = 1.5f;
			float t = 0;
			do
			{
				t = (Time.time - fadeStartTime) / duration;
				var alpha =
					1.0f -
					Mathf.Clamp01(Mathf.Abs(t * 1.1f - 0.5f) *
					              2f); // changes alpha from 0 to 1 and then to 0 again
				fadeMaterial.color = new Color(0f, 0f, 0f, alpha);
				yield return new WaitForEndOfFrame();
			} while (t < 1f);

			fadePlane.SetActive(false);
		}
	}
}