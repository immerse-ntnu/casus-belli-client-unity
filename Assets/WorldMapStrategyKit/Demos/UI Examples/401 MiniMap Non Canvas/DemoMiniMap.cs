using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class DemoMiniMap : MonoBehaviour
	{
		private GUIStyle labelStyle, labelStyleShadow, buttonStyle;

		private void Start()
		{
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

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);
		}

		/// <summary>
		/// UI Buttons
		/// </summary>
		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			// buttons background color
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			// Add button to toggle Earth texture
			if (GUI.Button(new Rect(10, 10, 160, 30), "  Open Mini Map", buttonStyle))
			{
				var left = 0.8f;
				var top = 0.8f;
				var width = 0.2f;
				var height = 0.2f;
				var normalizedScreenRect = new Vector4(left, top, width, height);
				var minimap = WMSKMiniMap.Show(normalizedScreenRect);
				minimap.duration = 2f;
				minimap.zoomLevel = 0.1f;
			}

			if (WMSKMiniMap.IsVisible())
			{
				if (GUI.Button(new Rect(10, 50, 160, 30), "  Random Position", buttonStyle))
				{
					var left = Random.value * 0.8f;
					var top = Random.value * 0.8f;
					var width = 0.2f;
					var height = 0.2f;
					var normalizedScreenRect = new Vector4(left, top, width, height);
					WMSKMiniMap.RepositionAt(normalizedScreenRect);
				}

				if (GUI.Button(new Rect(10, 90, 160, 30), "  Hide Mini Map", buttonStyle))
					WMSKMiniMap.Hide();
			}
		}
	}
}