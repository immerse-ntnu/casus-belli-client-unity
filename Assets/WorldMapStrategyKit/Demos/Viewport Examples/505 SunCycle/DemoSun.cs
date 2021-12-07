using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class DemoSun : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle labelStyle, labelStyleShadow, buttonStyle, sliderStyle, sliderThumbStyle;

		private float timeOfDay = 0.0f; // in hours (0-23.99)

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

			map.CenterMap();

			// Instantiate game object and position it instantly over the city
			var tower = Instantiate(Resources.Load<GameObject>("Tower/Tower"));
			var position = map.GetCity("Lhasa", "China").unity2DLocation;
			tower.WMSK_MoveTo(position);

			// Zoom in
			map.FlyToLocation(position, 1f, 0.1f);
		}

		/// <summary>
		/// UI Buttons
		/// </summary>
		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			GUI.Box(new Rect(0, 0, 160, 160), "");

			// Path line controls
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);
			GUI.Button(new Rect(10, 50, 150, 30), "  Time of Day", buttonStyle);
			GUI.backgroundColor = Color.white;
			timeOfDay = GUI.HorizontalSlider(new Rect(10, 85, 150, 35), timeOfDay, 0, 24f, sliderStyle,
				sliderThumbStyle);
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			timeOfDay += 0.01f;
			if (timeOfDay >= 24)
				timeOfDay = 0;
			map.timeOfDay = timeOfDay;
		}
	}
}