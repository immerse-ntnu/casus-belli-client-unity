using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoUnitSelection : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle labelStyle;
		private GameObjectAnimator tank1, tank2;

		private void Start()
		{
			// Get a reference to the World Map API:
			map = WMSK.instance;

			// UI Setup - non-important, only for this demo
			labelStyle = new GUIStyle();
			labelStyle.alignment = TextAnchor.MiddleLeft;
			labelStyle.normal.textColor = Color.white;

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			// Create two tanks
			var parisLocation = map.GetCity("Paris", "France").unity2DLocation;
			tank1 = DropTankOnPosition(parisLocation);
			tank1.name = "French Tank";
			var madridLocation = map.GetCity("Madrid", "Spain").unity2DLocation;
			tank2 = DropTankOnPosition(madridLocation);
			tank2.name = "Spanish Tank";

			// Fly to a mid-point between Paris and Madrid
			var midPoint = (parisLocation + madridLocation) * 0.5f;
			map.FlyToLocation(midPoint, 2f, 0.1f);

			// Listen to unit-level events (if you need unit-level events...)
			tank1.OnPointerEnter += (GameObjectAnimator anim) =>
				Debug.Log("UNIT EVENT: " + tank1.name + " mouse enter event.");
			tank1.OnPointerExit += (GameObjectAnimator anim) =>
				Debug.Log("UNIT EVENT: " + tank1.name + " mouse exit.");
			tank1.OnPointerUp += (GameObjectAnimator anim) =>
				Debug.Log("UNIT EVENT: " + tank1.name + " mouse button up.");
			tank1.OnPointerDown += (GameObjectAnimator anim) =>
				Debug.Log("UNIT EVENT: " + tank1.name + " mouse button down.");

			tank2.OnPointerEnter += (GameObjectAnimator anim) =>
				Debug.Log("UNIT EVENT: " + tank2.name + " mouse enter event.");
			tank2.OnPointerExit += (GameObjectAnimator anim) =>
				Debug.Log("UNIT EVENT: " + tank2.name + " mouse exit.");
			tank2.OnPointerUp += (GameObjectAnimator anim) =>
				Debug.Log("UNIT EVENT: " + tank2.name + " mouse button up.");
			tank2.OnPointerDown += (GameObjectAnimator anim) =>
				Debug.Log("UNIT EVENT: " + tank2.name + " mouse button down.");

			// Listen to global Vieport GameObject (VGO) events (... better and simple approach)

			map.OnVGOPointerDown = delegate(GameObjectAnimator obj)
			{
				Debug.Log("GLOBAL EVENT: Left button pressed on " + obj.name);
				ColorTankMouseDown(obj);
			};
			map.OnVGOPointerUp = delegate(GameObjectAnimator obj)
			{
				Debug.Log("GLOBAL EVENT: Left button released on " + obj.name);
				ColorTankMouseUp(obj);
			};

			map.OnVGOPointerRightDown = delegate(GameObjectAnimator obj)
			{
				Debug.Log("GLOBAL EVENT: Right button pressed on " + obj.name);
				ColorTankMouseDown(obj);
			};
			map.OnVGOPointerRightUp = delegate(GameObjectAnimator obj)
			{
				Debug.Log("GLOBAL EVENT: Right button released on " + obj.name);
				ColorTankMouseUp(obj);
			};

			map.OnVGOPointerEnter = delegate(GameObjectAnimator obj)
			{
				Debug.Log("GLOBAL EVENT: Mouse entered " + obj.name);
				ColorTankHover(obj);
			};
			map.OnVGOPointerExit = delegate(GameObjectAnimator obj)
			{
				Debug.Log("GLOBAL EVENT: Mouse exited " + obj.name);
				RestoreTankColor(obj);
			};

			map.OnClick += (float x, float y, int buttonIndex) => { Debug.Log("Map Clicked"); };
		}

		/// <summary>
		/// UI Buttons
		/// </summary>
		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();
			GUI.Box(new Rect(10, 10, 460, 40),
				"Click on any tank and watch the console for unit-level and global-level events",
				labelStyle);
		}

		// Create tank instance and add it to the map
		private GameObjectAnimator DropTankOnPosition(Vector2 mapPosition)
		{
			var tankGO = Instantiate(Resources.Load<GameObject>("Tank/CompleteTank"));
			var tank = tankGO.WMSK_MoveTo(mapPosition);
			tank.autoRotation = true;
			return tank;
		}

		private void ColorTankHover(GameObjectAnimator obj)
		{
			// Changes tank color - but first we store original color inside its attribute bag
			var renderer = obj.GetComponentInChildren<Renderer>();
			obj.attrib["color"] = renderer.sharedMaterial.color;
			renderer.material.color =
				Color.yellow; // notice how I use material and not sharedmaterial - this is to prevent affecting all clone instances - we just want to color this one, so we need to make this material unique.
		}

		private void ColorTankMouseDown(GameObjectAnimator obj)
		{
			// Changes tank color to white
			var renderer = obj.GetComponentInChildren<Renderer>();
			renderer.sharedMaterial.color = Color.white;
		}

		private void ColorTankMouseUp(GameObjectAnimator obj)
		{
			// Changes tank color to white
			var renderer = obj.GetComponentInChildren<Renderer>();
			renderer.sharedMaterial.color = Color.yellow;
		}

		private void RestoreTankColor(GameObjectAnimator obj)
		{
			// Restores original tank color
			var renderer = obj.GetComponentInChildren<Renderer>();
			Color tankColor = obj.attrib["color"]; // get back the original color from attribute bag
			renderer.sharedMaterial.color = tankColor;
		}
	}
}