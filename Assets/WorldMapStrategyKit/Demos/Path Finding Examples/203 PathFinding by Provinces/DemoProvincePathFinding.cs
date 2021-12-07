using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class DemoProvincePathFinding : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle labelStyle, labelStyleShadow;
		private int selectStage;
		private int startProvinceIndex = -1;

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
			map.OnProvinceClick += HandleOnProvinceClick;
			map.OnProvinceEnter += HandleOnProvinceEnter;
		}

		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			string msg;
			if (selectStage == 0)
				msg = "Select starting province";
			else
				msg = "Move over other province to show provinces path";
			GUI.Label(new Rect(11, 11, 300, 20), msg, labelStyleShadow);
			GUI.Label(new Rect(10, 10, 300, 20), msg, labelStyle);
		}

		private void HandleOnProvinceEnter(int destinationProvinceIndex, int regionIndex)
		{
			if (startProvinceIndex >= 0 && startProvinceIndex != destinationProvinceIndex)
			{
				// Clear existing path
				Refresh();
				// Find a province path between starting province and destination province
				var provincesInPath = map.FindRoute(map.GetProvince(startProvinceIndex),
					map.GetProvince(destinationProvinceIndex));
				// If a path has been found, paint it!
				if (provincesInPath != null)
					provincesInPath.ForEach(provinceIndex =>
						map.ToggleProvinceSurface(provinceIndex, true, Color.grey));
				else // Otherwise, show it's not possible to reach that province.
					Debug.Log(map.provinces[destinationProvinceIndex].name +
					          " is not reachable from " +
					          map.provinces[startProvinceIndex].name +
					          "! You may need to adjust the neighbours property of some provinces to enable crossing.");
			}
		}

		private void HandleOnProvinceClick(int provinceIndex, int regionIndex, int buttonIndex)
		{
			startProvinceIndex = provinceIndex;
			selectStage = 1;
			Refresh();
		}

		private void Refresh()
		{
			map.HideProvinceSurfaces();
			map.ToggleProvinceSurface(startProvinceIndex, true, Color.blue);
		}
	}
}