using UnityEngine;

namespace WorldMapStrategyKit
{
	public class DemoOverlayFunctions : MonoBehaviour
	{
		private WMSK map;
		private GameObjectAnimator tank;

		private void Start()
		{
			map = WMSK.instance;
			map.OnClick += MapClickEvent;
		}

		public void DemoRoute()
		{
			var cityStartRoute = map.GetCity("Madrid", "Spain").unity2DLocation;
			var cityEndRoute = map.GetCity("Rome", "Italy").unity2DLocation;
			var startCell = map.GetCell(cityStartRoute);
			var endCell = map.GetCell(cityEndRoute);

			var cellIndices = map.FindRoute(startCell, endCell, TERRAIN_CAPABILITY.OnlyGround);

			// Highlight cells in path
			map.CellBlink(cellIndices, Color.yellow, 4f);

			// Closer look
			map.FlyToLocation(cityStartRoute, 0.5f, 0.2f);
		}

		public void AddGameObject()
		{
			// Get a random city
			var city = map.GetCityRandom();

			// Get city location
			var cityPosition = city.unity2DLocation;

			// Create a tank and position it on the appropiate location
			var tankGO = Instantiate(Resources.Load<GameObject>("Tank/CompleteTank"));
			tank = tankGO.WMSK_MoveTo(cityPosition);
			tank.autoRotation = true;
			tank.terrainCapability = TERRAIN_CAPABILITY.OnlyGround;

			// Closer look
			map.FlyToLocation(cityPosition, 0.5f, 0.1f);
		}

		private void MapClickEvent(float x, float y, int mouseButtonIndex)
		{
			// If tank is created, try to move tank over that position
			if (tank != null)
				tank.MoveTo(new Vector2(x, y), 100f, DURATION_TYPE.MapLap);
		}
	}
}