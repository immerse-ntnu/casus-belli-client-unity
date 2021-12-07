using UnityEngine;
using UnityEditor;

namespace WorldMapStrategyKit
{
	public static class WMSKMenuExtensions
	{
		[MenuItem("GameObject/3D Object/World Map Strategy Kit Map")]
		private static void CreateWMSKMap()
		{
			var wmsk = Object.Instantiate(Resources.Load<GameObject>("WMSK/Prefabs/WorldMapStrategyKit"));
			wmsk.name = "WorldMapStrategyKit";
		}

		[MenuItem("GameObject/3D Object/World Map Strategy Kit Viewport")]
		private static void CreateWMSKViewport()
		{
			var viewport = Object.Instantiate(Resources.Load<GameObject>("WMSK/Prefabs/Viewport"));
			viewport.name = "Viewport";
			if (!WMSK.instanceExists)
			{
				var wmsk = Object.Instantiate(
					Resources.Load<GameObject>("WMSK/Prefabs/WorldMapStrategyKit"));
				wmsk.name = "WorldMapStrategyKit";
			}
		}
	}
}