using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoSlippyMap2D : MonoBehaviour
	{
		private WMSK map;
		private GUIStyle style;

		private void Awake()
		{
			// Get a reference to the World Map API:
			map = WMSK.instance;
		}

		private void OnGUI()
		{
			if (style == null)
			{
				style = new GUIStyle(GUI.skin.box);
				style.normal.textColor = Color.white;
			}
			var totalLoad = map.tileWebDownloads + map.tileCacheLoads;
			var cacheHitRatio = totalLoad > 0 ? map.tileCacheLoads * 100.0f / totalLoad : 0;
			var rect = new Rect(5, 5, Screen.width - 10, 25);
			GUI.Box(rect, "Zoom level: " +
			              map.tileCurrentZoomLevel +
			              " Tiles loaded: " +
			              totalLoad +
			              " (" +
			              map.tileQueueLength +
			              " pending) Active Downloads: " +
			              map.tileConcurrentLoads +
			              ", Web Downloads: " +
			              map.tileWebDownloads +
			              " (" +
			              (map.tileWebDownloadsTotalSize / (1024f * 1024f)).ToString("F1") +
			              " Mb), Cache Loads: " +
			              map.tileCacheLoads +
			              " (" +
			              cacheHitRatio.ToString("F1") +
			              "%%)", style);

			if (map.tileServerCopyrightNotice != null)
			{
				var rectCredits = new Rect(5, Screen.height - 30, Screen.width - 10, 25);
				GUI.Box(rectCredits, "Credits: " + map.tileServerCopyrightNotice, style);
			}
		}
	}
}