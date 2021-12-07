using UnityEngine;
using System.Text;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class DemoMapPopulation : MonoBehaviour
	{
		public GameObject tankPrefab, shipPrefab, airplanePrefab, towerPrefab, spritePrefab, spherePrefab;

		private enum UNIT_TYPE
		{
			TANK = 1,
			SHIP = 2,
			AIRPLANE = 3
		}

		private WMSK map;
		private GUIStyle labelStyle, labelStyleShadow, buttonStyle;
		private bool enableAddTowerOnClick, enableClickToMoveTank, enableClickToMoveShip;
		private GameObjectAnimator tank, ship, airplane;
		private List<GameObjectAnimator> units;
		private int unitIndex;

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

			// setup GUI resizer - only for the demo
			GUIResizer.Init(800, 500);

			map.OnClick += (x, y, _) =>
			{
				if (enableAddTowerOnClick)
					AddTowerAtPosition(x, y);
				else if (enableClickToMoveTank)
					MoveTankWithPathFinding(new Vector2(x, y));
				else if (
					enableClickToMoveShip &&
					ship !=
					null) // as ship has terrainCapability set to Water the MoveTo() method will use path finding to route the ship to destination.
					MoveShipToDestination(new Vector2(x, y), 0.1f);
			};

			map.CenterMap();
		}

		// Executes on each frame - move ships and tanks around
		private void Update()
		{
			if (units != null)
			{
				// Make units move around the board
				unitIndex++;
				if (unitIndex >= units.Count)
					unitIndex = 0;
				var unit = units[unitIndex];
				if (!unit.isMoving)
				{
					var destination = unit.type == (int)UNIT_TYPE.TANK
						? GetRandomCityPosition()
						: GetRandomWaterPosition();
					unit.MoveTo(destination, 0.1f);
				}
			}

			if (Input.GetKeyDown(KeyCode.A))
				ship.visible = !ship.visible;
		}

		/// <summary>
		/// UI Buttons
		/// </summary>
		private void OnGUI()
		{
			// Do autoresizing of GUI layer
			GUIResizer.AutoResize();

			GUI.Box(new Rect(0, 0, 160, 160), "");

			var prev = enableAddTowerOnClick;
			enableAddTowerOnClick = GUI.Toggle(new Rect(10, 20, 150, 30), enableAddTowerOnClick,
				"Enable Tower On Click");
			if (enableAddTowerOnClick && prev != enableAddTowerOnClick)
			{
				enableClickToMoveTank = false;
				enableClickToMoveShip = false;
			}

			prev = enableClickToMoveTank;
			enableClickToMoveTank = GUI.Toggle(new Rect(180, 20, 200, 30), enableClickToMoveTank,
				"Enable Move Tank On Click");
			if (enableClickToMoveTank && prev != enableClickToMoveTank)
			{
				enableAddTowerOnClick = false;
				enableClickToMoveShip = false;
			}

			prev = enableClickToMoveShip;
			enableClickToMoveShip = GUI.Toggle(new Rect(390, 20, 200, 30), enableClickToMoveShip,
				"Enable Move Ship On Click");
			if (enableClickToMoveShip && prev != enableClickToMoveShip)
			{
				enableAddTowerOnClick = false;
				enableClickToMoveTank = false;
			}

			// buttons background color
			GUI.backgroundColor = new Color(0.1f, 0.1f, 0.3f, 0.95f);

			if (GUI.Button(new Rect(10, 50, 150, 30), "  Add Random Tower", buttonStyle))
				AddRandomTower();

			if (GUI.Button(new Rect(10, 90, 150, 30), "  Add Random Sprite", buttonStyle))
				AddRandomSprite();

			if (GUI.Button(new Rect(10, 130, 150, 30), "  Drop Tank on Paris", buttonStyle))
				DropTankOnCity();

			if (GUI.Button(new Rect(10, 170, 150, 30), "  Move Tank & Follow", buttonStyle))
				MoveTankAndFollow();

			if (ship != null)
			{
				if (GUI.Button(new Rect(10, 210, 150, 30), "  Destroy Ship", buttonStyle))
				{
					DestroyImmediate(ship.gameObject);
					ship = null;
				}
			}
			else if (GUI.Button(new Rect(10, 210, 150, 30), "  Launch Ship", buttonStyle))
					LaunchShip();

			if (GUI.Button(new Rect(10, 250, 150, 30), "  Start Flight", buttonStyle))
				ShowAirplane();

			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 50, 180, 30), "  Mass Create",
				buttonStyle))
				MassCreate(50);

			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 90, 180, 30),
				"  Find France Coast", buttonStyle))
				FindFranceCoast();

			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 130, 180, 30),
				"  Find France-Germany Line", buttonStyle))
				FindFranceGermanyLine();

			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 170, 180, 30),
				"  Find France-Germany Provinces", buttonStyle))
				FindFranceGermanyProvinces();

			if (GUI.Button(new Rect(GUIResizer.authoredScreenWidth - 190, 210, 180, 30), "  Add Sphere",
				buttonStyle))
				AddSphere();
		}

		/// <summary>
		/// Creates a tower instance and adds it to the map at a random city
		/// </summary>
		private void AddRandomTower()
		{
			// Get a random big city
			var cityIndex = -1;
			do
				cityIndex = Random.Range(0, map.cities.Length);
			while (map.cities[cityIndex].population < 10000);

			// Get city location
			var cityPosition = map.cities[cityIndex].unity2DLocation;

			// Create tower and add it to the map
			AddTowerAtPosition(cityPosition.x, cityPosition.y);

			// Fly to the location with provided zoom level
			map.FlyToLocation(cityPosition, 2.0f, 0.1f);
		}

		/// <summary>
		/// Creates a sprite instance and adds it to the map at a random city
		/// </summary>
		private void AddRandomSprite()
		{
			// Get a random big city
			var cityIndex = -1;
			do
				cityIndex = Random.Range(0, map.cities.Length);
			while (map.cities[cityIndex].population < 10000);

			// Get city location
			var cityPosition = map.cities[cityIndex].unity2DLocation;

			AddRandomSpriteAtPosition(cityPosition);

			// Fly to the location with provided zoom level
			map.FlyToLocation(cityPosition, 2.0f, 0.1f);
		}

		private void AddRandomSpriteAtPosition(Vector2 position)
		{
			// Instantiate the sprite, face it to up and position it into the map
			var star = Instantiate(spritePrefab);
			star.transform.localRotation = Quaternion.Euler(90, 0, 0);
			star.transform.localScale = Misc.Vector3one * 0.3f;
			star.WMSK_MoveTo(position, true, 0.25f);
		}

		/// <summary>
		/// Creates a tower instance and adds it to given coordinates
		/// </summary>
		private void AddTowerAtPosition(float x, float y)
		{
			// Instantiate game object and position it instantly over the city
			var tower = Instantiate(towerPrefab);
			var anim = tower.WMSK_MoveTo(x, y);
			anim.autoScale = false;
		}

		/// <summary>
		/// Creates a tank instance and adds it to specified city
		/// </summary>
		private void DropTankOnCity()
		{
			// Get a random big city
			var cityIndex = map.GetCityIndex("Paris", "France");

			// Get city location
			var cityPosition = map.cities[cityIndex].unity2DLocation;

			if (tank != null)
				DestroyImmediate(tank.gameObject);
			tank = DropTankOnPosition(cityPosition);

			// Zoom into tank
			map.FlyToLocation(cityPosition, 2.0f, 0.15f);

			// Enable move on click in this demo
			enableAddTowerOnClick = false;
			enableClickToMoveShip = false;
			enableClickToMoveTank = true;

			// Finally, signal me when tank starts and stops
			tank.OnMoveStart += (thisTank) =>
				Debug.Log("Tank has starting moving to " + thisTank.destination + " location.");
			tank.OnMoveEnd += (thisTank) =>
				Debug.Log("Tank has stopped at " + thisTank.currentMap2DLocation + " location.");
			tank.OnCountryEnter += (thisTank) => Debug.Log("Tank has entered country " +
			                                               map.GetCountry(thisTank.currentMap2DLocation)
				                                               .name +
			                                               ".");
			tank.OnProvinceEnter += (thisTank) => Debug.Log("Tank has entered province " +
			                                                map.GetProvince(thisTank.currentMap2DLocation)
				                                                .name +
			                                                ".");
		}

		// Create tank instance and add it to the map
		private GameObjectAnimator DropTankOnPosition(Vector2 mapPosition)
		{
			var tankGO = Instantiate(tankPrefab);
			tankGO.transform.localScale = Misc.Vector3one * 0.25f;
			tank = tankGO.WMSK_MoveTo(mapPosition);
			tank.type = (int)UNIT_TYPE.TANK;
			tank.autoRotation = true;
			tank.terrainCapability = TERRAIN_CAPABILITY.OnlyGround;
			return tank;
		}

		/// <summary>
		/// Checks if tank is near Paris. Then moves it to Moscow. Otherwise, moves it back to Paris.
		/// </summary>
		private void MoveTankAndFollow()
		{
			string destinationCity, destinationCountry;
			if (tank == null)
				DropTankOnCity();

			// Gets position of Paris in map
			var parisPosition = map.GetCity("Paris", "France").unity2DLocation;

			// Is the tank nearby (less than 50 km)? Then set destination to Moscow, otherwize Paris again
			if (map.calc.Distance(tank.currentMap2DLocation, parisPosition) < 50000)
			{
				destinationCity = "Moscow";
				destinationCountry = "Russia";
			}
			else
			{
				destinationCity = "Paris";
				destinationCountry = "France";
			}

			// Get position of destination
			var destination = map.GetCity(destinationCity, destinationCountry).unity2DLocation;
			Debug.Log(destinationCity + " " + destinationCountry + " " + destination);

			// For this movement, we will move the tank following a straight line
			tank.terrainCapability = TERRAIN_CAPABILITY.Any;

			// Move the tank to the new position with smooth ease
			tank.easeType = EASE_TYPE.SmoothStep;

			// Use a close zoom during follow - either current zoom level or 0.1f maximum so tank is watched closely
			tank.follow = true;
			tank.followZoomLevel = Mathf.Min(0.1f, map.GetZoomLevel());

			// Move it!
			tank.MoveTo(destination, 4.0f);
		}

		/// <summary>
		/// Moves the tank with path finding.
		/// </summary>
		private void MoveTankWithPathFinding(Vector2 destination)
		{
			// Ensure tank is limited terrain, avoid water
			if (tank == null)
			{
				DropTankOnCity();
				return;
			}
			tank.terrainCapability = TERRAIN_CAPABILITY.OnlyGround;
			// Example of durations
			tank.MoveTo(destination, 0.1f, DURATION_TYPE.Step);
//												tank.MoveTo (destination, 2f, DURATION_TYPE.Route);
//												tank.MoveTo (destination, 100f, DURATION_TYPE.MapLap);
		}

		private void MoveShipToDestination(Vector2 destination, float duration)
		{
			ship.MoveTo(destination, duration);
		}

		/// <summary>
		/// Creates ship. Main function called from button UI.
		/// </summary>
		private void LaunchShip()
		{
			// Get a coastal city and a water entrypoint
			var cityIndex = Random.Range(0, map.cities.Length);
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
			} while (!map.ContainsWater(cityPosition, 0.0001f, out waterPosition));

			if (safeAbort > 8000)
				return;

			// Create ship
			if (ship != null)
				DestroyImmediate(ship.gameObject);
			ship = DropShipOnPosition(waterPosition, true);

			// Fly to the location of ship with provided zoom level
			map.FlyToLocation(waterPosition, 2.0f, 0.1f);

			// Enable move on click in this demo
			enableAddTowerOnClick = false;
			enableClickToMoveTank = false;
			enableClickToMoveShip = true;
		}

		/// <summary>
		/// Creates a new ship on position.
		/// </summary>
		private GameObjectAnimator DropShipOnPosition(Vector2 position, bool addAOE)
		{
			// Create ship
			var shipGO = Instantiate(shipPrefab);
			shipGO.transform.localScale = Misc.Vector3one * 0.25f;
			ship = shipGO.WMSK_MoveTo(position);
			ship.type = (int)UNIT_TYPE.SHIP;
			ship.terrainCapability = TERRAIN_CAPABILITY.OnlyWater;
			ship.autoRotation = true;

			// Add circle of area of effect
			if (addAOE)
			{
				var circle = map.AddCircle(ship.currentMap2DLocation, 450, 0.9f, 1f,
					new Color(255, 255, 0, 0.33f));

				// Hook event OnMove to sync circle position and destroy it when ship no longer exists
				ship.OnMove += (ship) =>
					circle.transform.localPosition = new Vector3(ship.currentMap2DLocation.x,
						ship.currentMap2DLocation.y, 0);

				// Show/hide also with ship
				ship.OnVisibleChange += ship => circle.SetActive(ship.isVisibleInViewport);

				// Optionally hook OnKilled - so we don't have to remember to remove the circle when ship is destroyed
				ship.OnKilled += _ => Destroy(circle);

				// WHY circle is not parented to child?
				// Because circle should represent an accurate area of the world map. Units added to the viewport with WMSK_MoveTo(), are parented to the viewport object
				// and they get scaled in a way that's not realistic (the aim is to ensure unit visibility, hence when you zoom out, units still are visible, they're tiny but visible)
				// However, objects that are not parented to the viewport, but are parented to the 2D Map (which is also on the scene) will be visible through the viewport but keep their
				// appropriate scale (this is the case of circles, lines, ... markers in general)
			}

			return ship;
		}

		/// <summary>
		/// Returns a random position on water.
		/// </summary>
		private Vector2 GetRandomWaterPosition()
		{
			// Get a coastal city and a water entrypoint
			var waterPosition = Misc.Vector2zero;
			var safeAbort = 0;
			do
			{
				waterPosition = new Vector2(Random.value - 0.5f, Random.value - 0.5f);
				if (safeAbort++ > 10)
					break;
			} while (!map.ContainsWater(waterPosition));
			return waterPosition;
		}

		private Vector2 GetRandomCityPosition()
		{
			var cityIndex = Random.Range(0, map.cities.Length);
			return map.cities[cityIndex].unity2DLocation;
		}

		/// <summary>
		/// Creates an airplane on New York. Main function called from button UI.
		/// </summary>
		private void ShowAirplane()
		{
			// Destroy existing airplane
			if (airplane != null)
				DestroyImmediate(airplane.gameObject);

			// Location for airplane
			var position = map.GetCity("New York", "United States of America").unity2DLocation;

			// Create ship
			var airplaneGO = Instantiate(airplanePrefab);
			airplaneGO.transform.localScale = Misc.Vector3one * 0.25f;
			airplane = airplaneGO.WMSK_MoveTo(position);
			airplane.type =
				(int)UNIT_TYPE
					.AIRPLANE; // this is completely optional, just used in the demo scene to differentiate this unit from other tanks and ships
			airplane.terrainCapability =
				TERRAIN_CAPABILITY
					.Any; // ignores path-finding and can use a straight-line from start to destination
			airplane.pivotY =
				0.5f; // model is not ground based (which has a pivoty = 0, the default value, so setting the pivot to 0.5 will center vertically the model)
			airplane.autoRotation = true; // auto-head to destination when moving
			airplane.rotationSpeed = 0.25f; // speed of the rotation of auto-head to destination

			// Go to airplane location and wait for launch
			map.FlyToLocation(position, 1.5f, 0.05f);
			Invoke(nameof(StartFlight), 2f);
		}

		private void StartFlight()
		{
			airplane.arcMultiplier = 5f; // this is the arc for the plane trajectory
			airplane.easeType = EASE_TYPE.SmootherStep; // make it an easy-in-out movement

			var destination = map.GetCity("Paris", "France").unity2DLocation;
			airplane.MoveTo(destination, 150f);
			airplane.OnMoveEnd += (GameObjectAnimator anim) =>
			{
				anim.follow = false;
			}; // once the movement has finished, stop following the unit
		}

		/// <summary>
		/// Creates lots of ships and tanks. Called from main UI button.
		/// </summary>
		private void MassCreate(int numberOfUnits)
		{
			units = new List<GameObjectAnimator>();
			// add tanks
			for (var k = 0; k < numberOfUnits; k++)
			{
				var cityPosition = GetRandomCityPosition();
				var newTank = DropTankOnPosition(cityPosition);
				newTank.gameObject.hideFlags =
					HideFlags.HideInHierarchy; // don't show in hierarchy to avoid clutter
				units.Add(newTank);
			}
			// add ships
			for (var k = 0; k < numberOfUnits; k++)
			{
				var waterPosition = GetRandomWaterPosition();
				var newShip = DropShipOnPosition(waterPosition, false);
				newShip.gameObject.hideFlags =
					HideFlags.HideInHierarchy; // don't show in hierarchy to avoid clutter
				units.Add(newShip);
			}
		}

		/// <summary>
		/// Locates coastal points for a sample country and add custom sprites over that line
		/// </summary>
		private void FindFranceCoast()
		{
			var franceIndex = map.GetCountryIndex("France");
			var points = map.GetCountryCoastalPoints(franceIndex);
			foreach (var vec in points)
				AddRandomSpriteAtPosition(vec);
			if (points.Count > 0)
				map.FlyToLocation(points[0], 2, 0.2f);
		}

		/// <summary>
		/// Locates common frontiers points between France and Germany and add custom sprites over that line
		/// </summary>
		private void FindFranceGermanyLine()
		{
			var franceIndex = map.GetCountryIndex("France");
			var germanyIndex = map.GetCountryIndex("Germany");
			var points = map.GetCountryFrontierPoints(franceIndex, germanyIndex);
			foreach (var point in points)
				AddRandomSpriteAtPosition(point);
			if (points.Count > 0)
				map.FlyToLocation(points[0], 2, 0.2f);
		}

		private void FindFranceGermanyProvinces()
		{
			map.showProvinces = true;
			var franceIndex = map.GetCountryIndex("France");
			var germanyIndex = map.GetCountryIndex("Germany");
			var points = map.GetCountryFrontierPoints(franceIndex, germanyIndex);
			if (points.Count > 0)
				map.FlyToLocation(points[0], 2, 0.2f);

			foreach (var t in points)
			{
				var provIndex = map.GetProvinceIndex(t +
				                                     new Vector2(Random.value * 0.0001f - 0.00005f,
					                                     Random.value * 0.0001f - 0.00005f));
				if (provIndex >= 0 &&
				    (map.provinces[provIndex].countryIndex == franceIndex ||
				     map.provinces[provIndex].countryIndex == germanyIndex))
					map.ToggleProvinceSurface(provIndex, true,
						new Color(Random.value, Random.value, Random.value));
			}
		}

		/// <summary>
		/// This function adds a standard sphere primitive to the map. The difference here is that the pivot of the sphere is centered in the sphere. So we make use of pivotY property to specify it and
		/// this way the positioning over the terrain will work. Otherwise, the sphere will be cut by the terrain (the center of the sphere will be on the ground - and we want the sphere on top of the terrain).
		/// </summary>
		private void AddSphere()
		{
			var sphere = Instantiate(spherePrefab);
			var position = map.GetCity("Lhasa", "China").unity2DLocation;
			var anim = sphere.WMSK_MoveTo(position);
			anim.pivotY = 0.5f;
			map.FlyToLocation(position, 2f, 0.1f);
		}
	}
}