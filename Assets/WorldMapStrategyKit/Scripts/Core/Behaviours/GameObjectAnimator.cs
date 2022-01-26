using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public enum DURATION_TYPE
	{
		Step = 0,
		Route = 1,
		MapLap = 2
	}

	public enum EASE_TYPE
	{
		Linear = 0,
		EaseOut = 1,
		EaseIn = 2,
		Exponential = 3,
		SmoothStep = 4,
		SmootherStep = 5
	}

	public delegate void GOEvent(GameObjectAnimator anim);

	public class GameObjectAnimator : MonoBehaviour, IExtendableAttribute
	{
		#region GameObject Animator Properties

		/// <summary>
		/// User defined value. Could be the unit or resource type.
		/// </summary>
		public int type;

		/// <summary>
		/// User-defined value for unit grouping. Some methogs accepts this value, like ToggleGroupVisibility method.
		/// </summary>
		public int group;

		/// <summary>
		/// User-defined value to specify to which player belongs this unit.
		/// </summary>
		public int player;

		private bool _visible = true;

		/// <summary>
		/// Controls general object visibility.
		/// </summary>
		public bool visible
		{
			get => _visible;
			set
			{
				if (_visible != value)
				{
					_visible = value;
					UpdateVisibility(true);
					UpdateTransformAndVisibility();
				}
			}
		}

		/// <summary>
		/// Scales/rotates object even if it's out of viewport
		/// </summary>
		public bool updateWhenOffScreen;

		/// <summary>
		/// Returns true if the gameobject is currently visible inside the viewport.
		/// </summary>
		public bool isVisibleInViewport;

		/// <summary>
		/// Current destination if it's moving. If unit moves along a path, destination marks the next step position.
		/// </summary>
		public Vector2 destination;

		/// <summary>
		/// Duration of the translation if it's moving.
		/// </summary>
		public float duration;

		/// <summary>
		/// Ease type for the movement. Default = Linear.
		/// </summary>
		public EASE_TYPE easeType = EASE_TYPE.Linear;

		/// <summary>
		/// If the gameobject is moving across the map.
		/// </summary>
		public bool isMoving;

		[SerializeField] private Vector2 _currentMap2DLocation;

		/// <summary>
		/// Current map local coordinate of the gameobject (x,y in the range of -0.5...0.5)
		/// </summary>
		public Vector2 currentMap2DLocation => _currentMap2DLocation;

		/// <summary>
		/// Altitude from ground of the gameobject since start of movement. A zero means object is grounded.
		/// </summary>
		public float altitudeStart;

		/// <summary>
		/// Altitude from ground of the gameobject at the end of movement. A zero means object is grounded. A value of -1 means use altitudeStart.
		/// </summary>
		public float altitudeEnd = -1;

		/// <summary>
		/// Synonym for altitudeStart.
		/// </summary>
		public float altitude { get => altitudeStart; set => altitudeStart = value; }

		[Obsolete("Use altitudeStart instead.")]
		public float height { get => altitudeStart; set => altitudeStart = value; }

		/// <summary>
		/// Height mode. See HEIGHT_OFFSET_MODE possible values:
		/// - ABSOLUTE_ALTITUDE: gameobject is set to given altitude ignoring terrain,
		/// - ABSOLUTE_CLAMPED: gameobject is set to given altitude but never below terrain (default value, if initial or default altitude is greater than zero).
		/// - RELATIVE_TO_GROUND: altitude is added to terrain height below current gameobject position (default value, if initial or default altitude is left to zero).
		/// </summary>
		public HEIGHT_OFFSET_MODE heightMode = HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED;

		/// <summary>
		/// Whether the gameobject will scale automatically when user zooms in/out into the viewport
		/// </summary>
		public bool autoScale = true;

		private bool _follow;

		/// <summary>
		/// If the camera should focus and follow this game object while moving
		/// </summary>
		public bool follow
		{
			get => _follow;
			set
			{
				_follow = value;
				followStartTime = map.time;
				followStartZoomLevel = map != null ? map.GetZoomLevel() : 1f;
				if (!map.GetCurrentMapLocation(out followStart2DLocation))
					followStart2DLocation =
						map.transform.InverseTransformPoint(map.currentCamera.transform.position);
			}
		}

		/// <summary>
		/// The follow zoom level of the camera (0..1).
		/// </summary>
		public float followZoomLevel = 0.1f;

		/// <summary>
		/// The follow duration in seconds. Measure the speed of change for the camera position.
		/// </summary>
		public float followDuration = 1f;

		/// <summary>
		/// When the game object moves, it will rotate around the map Y axis towards the movement direction effectively adapting to the terrain contour. Use this only for moveable units.
		/// </summary>
		public bool autoRotation;

		/// <summary>
		/// If set to true, the game object will maintain it's current rotation when moved over the viewport. Note that autoRotation overrides this property, so if you set autoRotation = true, preserveOriginalRotation will be ignored.
		/// </summary>
		public bool preserveOriginalRotation;

		/// <summary>
		/// The auto-rotation speed (0..1, 1=immediate rotation).
		/// </summary>
		public float rotationSpeed = 0.1f;

		/// <summary>
		/// Type of terrain the unit can pass through.
		/// </summary>
		public TERRAIN_CAPABILITY terrainCapability = TERRAIN_CAPABILITY.Any;

		/// <summary>
		/// Minimun altitude this unit move over (0..1).
		/// </summary>
		public float minAltitude;

		/// <summary>
		/// Maximum altitude this unit move over (0..1).
		/// </summary>
		public float maxAltitude = 1.0f;

		/// <summary>
		/// The max search cost for the path finding algorithm. A value of -1 will use the global default max defined by pathFindingMaxCost
		/// </summary>
		public float maxSearchCost = -1;

		/// <summary>
		/// The max movement steps for the path finding algorithm. A value of -1 will use the global default max defined by pathFindingMaxSteps
		/// </summary>
		public int maxSearchSteps = -1;

		[SerializeField] private int _uniqueId;

		/// <summary>
		/// A user-defined unique identifier useful to get a quick reference to this GO with VGOGet method.
		/// </summary>
		public int uniqueId
		{
			get => _uniqueId;
			set
			{
				var isRegistered = map.VGOIsRegistered(this);
				if (isRegistered)
					map.VGOUnRegisterGameObject(this);
				_uniqueId = value;
				if (isRegistered)
					map.VGORegisterGameObject(this);
			}
		}

		/// <summary>
		/// Use this property to add/retrieve custom attributes for this country
		/// </summary>
		public JSONObject attrib { get; set; }

		/// <summary>
		/// Specifies the Y coordinate of the pivot. If pivot is at bottom of the game object, you don't have to change this value (0). But if the pivot is at center, set this value to 0.5f. If pivot is at top, set this value to 1f.
		/// </summary>
		public float pivotY;

		/// <summary>
		/// Set to false to disable any buoyancy effect over this unit (animation due to sea waves).
		/// </summary>
		public bool enableBuoyancyEffect = true;

		/// <summary>
		/// Useful for arc trajectories (eg. cannons). Set this value to greater than 1 to produce a parabole.
		/// </summary>
		public float arcMultiplier = 1f;

		/// <summary>
		/// If set to true, the unit can change orientation but won't move. This value is cleared after unit has ended rotation.
		/// </summary>
		public bool isStatic;

		/// <summary>
		/// The starting location in map coordinates.
		/// </summary>
		public Vector2 startingMap2DLocation;

		/// <summary>
		/// The destination in map coordinates.
		/// </summary>
		public Vector2 endingMap2DLocation;

		/// <summary>
		/// The original scale of the game object when WMSK_MoveTo() was used first. Used for restoring go scale after switching from viewport to 2d mode.
		/// </summary>
		public Vector3 originalScale;

		/// <summary>
		/// If set to true, other map events won't be triggered when clicking on this unit (like OnClick)
		/// </summary>
		public bool blocksRayCast;

		/// <summary>
		/// True is mouse is currently over the gameobject
		/// </summary>
		public bool mouseIsOver;

		/// <summary>
		/// List of renderers for this unit that will be enabled/disabled when it enters or exits the map
		/// </summary>
		public List<Renderer> managedRenderers;

		#endregion

		/// <summary>
		/// Returns true if the last known pos of the gameobject was on water
		/// </summary>
		public bool lastKnownPosIsOnWater => _isOnWater;

		/// <summary>
		/// Returns true if the unit is currently on water (useful for hybrid units)
		/// </summary>
		public bool isOnWater
		{
			get
			{
				CheckWaterPos();
				return _isOnWater;
			}
		}

		#region GameObject Animator Events

		/// <summary>
		/// Fired when this GO starts moving
		/// </summary>
		public event GOEvent OnMoveStart;

		/// <summary>
		/// Fired when this GO moves
		/// </summary>
		public event GOEvent OnMove;

		/// <summary>
		/// Fired when this GO moves
		/// </summary>
		public event GOEvent OnKilled;

		/// <summary>
		/// Fired when this GO ends movement
		/// </summary>
		public event GOEvent OnMoveEnd;

		/// <summary>
		/// Fired when mouse enters this GO
		/// </summary>
		public event GOEvent OnPointerEnter;

		/// <summary>
		/// Fired when mouse exits this GO
		/// </summary>
		public event GOEvent OnPointerExit;

		/// <summary>
		/// Fired when left mouse button is pressed on this GO
		/// </summary>
		public event GOEvent OnPointerDown;

		/// <summary>
		/// Fired when left mouse button is pressed on this GO
		/// </summary>
		public event GOEvent OnPointerUp;

		/// <summary>
		/// Fired when right mouse button is pressed on this GO
		/// </summary>
		public event GOEvent OnPointerRightDown;

		/// <summary>
		/// Fired when right mouse button is pressed on this GO
		/// </summary>
		public event GOEvent OnPointerRightUp;

		/// <summary>
		/// Fired when the Unit enters a new country
		/// </summary>
		public event GOEvent OnCountryEnter;

		/// <summary>
		/// Fired when the Unit enters a new province
		/// </summary>
		public event GOEvent OnProvinceEnter;

		/// <summary>
		/// Fired when the Unit enters a new country region
		/// </summary>
		public event GOEvent OnCountryRegionEnter;

		/// <summary>
		/// Fired when the Unit enters a new country region
		/// </summary>
		public event GOEvent OnProvinceRegionEnter;

		/// <summary>
		/// Fired when this Unit becomes visible or invisible either using the visible property or because the unit exits the map in viewport mode
		/// </summary>
		public event GOEvent OnVisibleChange;

		#endregion

		private void OnValidate()
		{
			if (!Application.isPlaying && map != null)
				UpdateTransformAndVisibility(true);
		}

		#region Public API

		/// <summary>
		/// Initiates a movement for this GameObject towards a given destination
		/// </summary>
		/// <param name="duration">Duration of the movement</param>
		/// <param name="durationType">Step: each step will take the same duration, Route: the given duration is for the entire route, MapLap: the duration is the time to cross entire map. Default duration type is 'Step'. Use 'MapLap' if you pass a custom set of non-continuous points to ensure a consistent speed of movement.</param> 
		/// <returns><c>true</c>, if move is possible, <c>false</c> otherwise.</returns>
		public bool MoveTo(Vector2 destination, float duration,
			DURATION_TYPE durationType = DURATION_TYPE.Step)
		{
			if (terrainCapability != TERRAIN_CAPABILITY.Any)
			{
				// Get a route
				var route = FindRoute(destination);
				return MoveTo(route, duration, durationType);
			}
			ClearOptions();
			if (isMoving)
				Stop();
			endingMap2DLocation = destination;
			if (map == null)
				Init();
			if (!map.VGOIsRegistered(this))
			{
				transform.position =
					new Vector3(1000, -1000,
						0); // prevents showing unit at wrong location the first frame
				_currentMap2DLocation = destination;
			}
			if (startingMap2DLocation == Misc.Vector2zero)
				startingMap2DLocation = _currentMap2DLocation;
			isMoving = true;
			StartCoroutine(StartMove(destination, duration));
			return true;
		}

		/// <summary>
		/// Initiates a movement for this GameObject along a predefined route of cell indices (if using grid)
		/// </summary>
		/// <param name="duration">Duration of the movement</param>
		/// <param name="durationType">Step: each step will take the same duration, Route: the given duration is for the entire route, MapLap: the duration is the time to cross entire map. Default duration type is 'Step'. Use 'MapLap' if you pass a custom set of non-continuous points to ensure a consistent speed of movement.</param> 
		/// <returns><c>true</c>, if move is possible, <c>false</c> otherwise.</returns>
		public bool MoveTo(List<int> route, float duration,
			DURATION_TYPE durationType = DURATION_TYPE.Step)
		{
			var stepCount = route.Count;
			var vv = new List<Vector2>(stepCount);
			for (var k = 0; k < stepCount; k++)
				vv.Add(map.GetCell(route[k]).center);
			return MoveTo(vv, duration, durationType);
		}

		/// <summary>
		/// Initiates a movement for this GameObject along a predefined route
		/// </summary>
		/// <param name="duration">Duration of the movement</param>
		/// <param name="durationType">Step: each step will take the same duration, Route: the given duration is for the entire route, MapLap: the duration is the time to cross entire map. Default duration type is 'Step'. Use 'MapLap' if you pass a custom set of non-continuous points to ensure a consistent speed of movement.</param> 
		/// <returns><c>true</c>, if move is possible, <c>false</c> otherwise.</returns>
		public bool MoveTo(List<Vector2> route, float duration,
			DURATION_TYPE durationType = DURATION_TYPE.Step)
		{
			ClearOptions();
			if (isMoving)
				Stop();
			this.route = route;
			if (route == null)
				return false;
			var routeCount = route.Count;
			if (routeCount == 0)
				return false;
			routeNextDestinationIndex = 1;
			destination = routeCount > 1 ? route[1] : route[0];
			endingMap2DLocation = route[routeCount - 1];
			if (map == null)
				Init();
			if (!map.VGOIsRegistered(this))
			{
				transform.position =
					new Vector3(1000, -1000, 0); // prevents showing unit at wrong location the first frame
				_currentMap2DLocation = route[0];
			}
			if (startingMap2DLocation == Misc.Vector2zero)
				startingMap2DLocation = _currentMap2DLocation;

			// Duration?
			switch (durationType)
			{
				case DURATION_TYPE.Step:
					duration *= Mathf.Max(routeCount, 1);
					break;
				case DURATION_TYPE.MapLap:
					float length = 0;
					var prevPos = route[0];
					for (var k = 1; k < routeCount; k++)
					{
						length += Vector3.Distance(prevPos, route[k]);
						prevPos = route[k];
					}
					duration *= length;
					break;
			}
			// Starts first step
			isMoving = true;
			StartCoroutine(StartMove(destination, duration));
			return true;
		}

		/// <summary>
		/// Returns a potential movement path between two cities
		/// </summary>
		/// <returns>The route.</returns>
		public List<Vector2> FindRoute(City city)
		{
			if (city == null)
				return null;
			var route = map.FindRoute(_currentMap2DLocation, city.unity2DLocation, terrainCapability,
				minAltitude, maxAltitude, maxSearchCost, maxSearchSteps);
			return route;
		}

		/// <summary>
		/// Returns a potential movement path between two cities
		/// </summary>
		/// <returns>The route.</returns>
		public List<Vector2> FindRoute(string cityName, string countryName)
		{
			var city = map.GetCity(cityName, countryName);
			return FindRoute(city);
		}

		/// <summary>
		/// Returns a potential movement path from current position to destination with unit terrain capabilities
		/// </summary>
		/// <returns>The route.</returns>
		/// <param name="destination">Destination.</param>
		public List<Vector2> FindRoute(Vector2 destination)
		{
			var route = map.FindRoute(_currentMap2DLocation, destination, terrainCapability, minAltitude,
				maxAltitude, maxSearchCost, maxSearchSteps);
			return route;
		}

		/// <summary>
		/// Returns a potential movement path from current position to destination with unit terrain capabilities
		/// </summary>
		/// <returns>The route.</returns>
		/// <param name="destination">Destination.</param>
		/// <param name="totalCost">The cost for traversing this path.</param>
		public List<Vector2> FindRoute(Vector2 destination, out float totalCost)
		{
			var route = map.FindRoute(_currentMap2DLocation, destination, out totalCost, terrainCapability,
				minAltitude, maxAltitude, maxSearchCost, maxSearchSteps);
			return route;
		}

		/// <summary>
		/// Returns a potential movement path from current position to destination with unit terrain capabilities
		/// </summary>
		/// <returns>The route.</returns>
		/// <param name="destination">Destination.</param>
		public List<Vector2> FindRoute(Cell destinationCell)
		{
			var startingCell = map.GetCell(_currentMap2DLocation);
			var route = map.FindRoute(startingCell, destinationCell, terrainCapability, maxSearchCost,
				maxSearchSteps);
			return CellsToVector2List(route);
		}

		/// <summary>
		/// Returns a potential movement path from current position to destination with unit terrain capabilities
		/// </summary>
		/// <returns>The route.</returns>
		/// <param name="destination">Destination.</param>
		public List<Vector2> FindRoute(Cell destinationCell, out float totalCost)
		{
			var startingCell = map.GetCell(_currentMap2DLocation);
			var route = map.FindRoute(startingCell, destinationCell, out totalCost, terrainCapability,
				maxSearchCost, maxSearchSteps);
			return CellsToVector2List(route);
		}

		/// <summary>
		/// Returns true if the unit is near given map coordinate (optionally pass a max distance value).
		/// </summary>
		/// <returns><c>true</c> if this instance is near the specified mapPosition maxDistance; otherwise, <c>false</c>.</returns>
		/// <param name="mapPosition">Map position.</param>
		/// <param name="maxDistance">Max distance.</param>
		public bool isNear(Vector2 mapPosition, float maxDistance = 0.01f) =>
			FastVector.SqrDistance(ref _currentMap2DLocation, ref mapPosition) < maxDistance * maxDistance;

		/// <summary>
		/// Cancels current unit movement
		/// </summary>
		public void Stop()
		{
			duration = 0;
			route = null;
			stepTime = map.time;
			isMoving = false;
			onFixedMoveEnabled = false;
		}

		/// <summary>
		/// Gets the a list of cell indices where the unit can move with current maxSearchCost value and terrain capabilities.
		/// </summary>
		public List<int> GetCellNeighbours(int maxSteps = -1)
		{
			var cellIndex = map.GetCellIndex(_currentMap2DLocation);
			if (cellIndex < 0)
				return null;
			if (maxSteps < 0)
				maxSteps = (int)maxSearchCost + 1;
			return map.GetCellNeighbours(cellIndex, maxSteps, maxSearchCost, terrainCapability);
		}

		public void LookAt(Vector2 destination)
		{
			isStatic = true;
			MoveTo(destination, 2f);
		}

		/// <summary>
		/// Fires a bullet, cannon-ball, missile, etc.
		/// </summary>
		/// <param name="delay">Firing delay. Gives time to the unit so it can orient to target.</param>
		/// <param name="bullet">Bullet. You must supply your own bullet gameobject.</param>
		/// <param name="startAnchor">Start anchor. Where does the bullet appear? This vector3 is expressed in local coordinates of the firing unit and ignores its scale.</param>
		/// <param name="destination">Destination. Target 2d map coordinates.</param>
		/// <param name="duration">Duration for the bullet to reach its destination..</param>
		/// <param name="arcMultiplier">Pass a value greater than 1 to produce a parabole.</param>
		public GameObjectAnimator Fire(float delay, GameObject bullet, Vector3 startAnchor,
			Vector2 destination, float bulletSpeed, float arcMultiplier = 1f, bool testAnchor = false)
		{
			var bulletAnim = bullet.GetComponent<GameObjectAnimator>() ??
			                 bullet.AddComponent<GameObjectAnimator>();
			var renderer = bullet.GetComponent<Renderer>();
			if (renderer != null)
				renderer.enabled = false;
			StartCoroutine(FireThis(delay, bulletAnim, startAnchor, destination, bulletSpeed,
				arcMultiplier, testAnchor));
			return bulletAnim;
		}

		/// <summary>
		/// Modifies the movement speed of the unit during movement. Only affects current path.
		/// </summary>
		public void ChangeDuration(float extraDuration)
		{
			var newDuration = duration + extraDuration;
			if (newDuration < 0.001f)
			{
				newDuration = 0.001f;
				extraDuration = newDuration - duration;
			}
			stepTime -= (map.time - stepTime) * extraDuration / duration;
			duration = newDuration;
		}

		#endregion

		#region internal fields

		private float startingTime;

		// moment when move is issued
		private float stepTime;

		// moment of current step animation (1- rotating, 2- translating)
		private float followStartTime;

		// moment when the follow is issued (usually equals to startingTime but it could be set afterwards)
		private float followStartZoomLevel;
		private Vector3 followStart2DLocation;
		private Vector3 destinationDirection;
		private WMSK map;
		private List<Vector2> route;
		private int routeNextDestinationIndex;
		private bool _isOnWater;
		private Vector2 onWaterPosition;
		private Quaternion lastComputedRotation;
		private bool isAffectedByBuoyancy;
		private float progress;
		private float currentAltitude;
		private int lastCountryIndex = -1, lastProvinceIndex = -1;
		private int lastCountryRegionIndex = -1, lastProvinceRegionIndex = -1;
		private bool onFixedMoveEnabled;
		[NonSerialized] public bool mouseEventProcessedThisFrame;

		private float spriteRotation;
		private bool isSprite;

		#endregion

		#region internal gameloop events

		private void Awake()
		{
			if (map == null)
				Init();
		}

		private void Init()
		{
			map = WMSK.instance;
			attrib = new JSONObject();
			// ensure has collider; if not, adds a default box collider
			var collider = GetComponent<Collider>();
			if (collider == null)
				gameObject.AddComponent<BoxCollider>();
			UpdateManagedRenderersList();
			isSprite = gameObject.GetComponentInChildren<SpriteRenderer>() != null;
			spriteRotation = isSprite ? 0 : -90;
		}

		private void OnMouseEnter()
		{
			if (map.mouseIsOverUIElement && map.respectOtherUI)
				return;
			mouseIsOver = true;
			if (OnPointerEnter != null)
				OnPointerEnter(this);
			map.BubbleEvent(map.OnVGOPointerEnter, this);
			map.VGOLastHighlighted = this;
		}

		private void OnMouseExit()
		{
			if (map.mouseIsOverUIElement && map.respectOtherUI)
				return;
			mouseIsOver = false;
			if (OnPointerExit != null)
				OnPointerExit(this);
			map.BubbleEvent(map.OnVGOPointerExit, this);
			map.VGOLastHighlighted = null;
		}

		private void OnMouseDown()
		{
			if (map.mouseIsOverUIElement && map.respectOtherUI)
				return;
			if (Input.GetMouseButton(0))
			{
				if (OnPointerDown != null)
					OnPointerDown(this);
				map.BubbleEvent(map.OnVGOPointerDown, this);
				map.VGOLastClicked = this;
			}
			else if (Input.GetMouseButton(1))
			{
				if (OnPointerRightDown != null)
					OnPointerRightDown(this);
				map.BubbleEvent(map.OnVGOPointerRightDown, this);
				map.VGOLastClicked = this;
			}
		}

		private void OnMouseUp()
		{
			if (mouseEventProcessedThisFrame || map.mouseIsOverUIElement && map.respectOtherUI)
				return;
			mouseEventProcessedThisFrame = true;
			if (Input.GetMouseButton(0) || Input.GetMouseButtonUp(0))
			{
				if (OnPointerUp != null)
					OnPointerUp(this);
				map.BubbleEvent(map.OnVGOPointerUp, this);
			}
			else if (Input.GetMouseButton(1) || Input.GetMouseButtonUp(1))
			{
				if (OnPointerRightUp != null)
					OnPointerRightUp(this);
				map.BubbleEvent(map.OnVGOPointerRightUp, this);
			}
		}

		private void OnMouseOver()
		{
			if (mouseEventProcessedThisFrame || map.mouseIsOverUIElement && map.respectOtherUI)
				return;
			mouseEventProcessedThisFrame = true;
			mouseIsOver = true;
			if (Input.GetMouseButtonDown(1))
			{
				if (OnPointerRightDown != null)
					OnPointerRightDown(this);
				map.BubbleEvent(map.OnVGOPointerRightDown, this);
				map.VGOLastClicked = this;
			}
			else if (Input.GetMouseButtonUp(1))
			{
				if (OnPointerRightUp != null)
					OnPointerRightUp(this);
				map.BubbleEvent(map.OnVGOPointerRightUp, this);
			}
		}

		private void OnDestroy()
		{
			if (OnKilled != null)
				OnKilled(this);
			map.BubbleEvent(map.OnVGOKilled, this);
			map.VGOUnRegisterGameObject(this);
		}

		#endregion

		#region NGUI event redirectors

		private void OnHover(bool isOver)
		{
			if (isOver)
			{
				if (mouseIsOver)
					OnMouseOver();
				else
					OnMouseEnter();
			}
			else
				OnMouseExit();
		}

		private void OnPress(bool isPressed)
		{
			if (isPressed)
				OnMouseDown();
			else
				OnMouseUp();
		}

		#endregion

		#region Internal stuff

		private void ClearOptions()
		{
			route = null;
			progress = 0;
		}

		/// <summary>
		/// Initiates a movement for this Gameobject.
		/// </summary>
		private IEnumerator StartMove(Vector2 destination, float duration)
		{
			// wait for any other additional setting on this GameObjectAnimator before movements starts
			yield return new WaitForEndOfFrame();

			if (map == null)
				Init();

			this.duration = duration * map.VGOGlobalSpeed;
			this.destination = destination;
			startingTime = map.time;
			stepTime = startingTime;
			currentAltitude = altitudeStart;
			if (altitudeEnd < 0)
				altitudeEnd = altitudeStart;
			isMoving = true;
			CheckWaterPos();

			onFixedMoveEnabled = true;

			if (map.VGOIsRegistered(this))
				startingMap2DLocation = _currentMap2DLocation;
			else
			{
				// Register the gameobject for terrain updates
				map.VGORegisterGameObject(this);
				if (startingMap2DLocation == Misc.Vector2zero)
					startingMap2DLocation = map.WorldToMap2DPosition(transform.position);

				originalScale = transform.localScale;

				PerformUpdateLoop();

				// Reset current object visibility
				UpdateVisibility(true);
			}

			SetupDirection(destination);
			UpdateTransformAndVisibility(true);

			if (duration > 0 && !isStatic)
			{
				if (OnMoveStart != null)
					OnMoveStart(this);
				map.BubbleEvent(map.OnVGOMoveStart, this);
			}
		}

		private void SetupDirection(Vector2 nextStop)
		{
			if (autoRotation)
			{
				var worldPosDestination = map.Map2DToWorldPosition(nextStop, currentAltitude,
					transform.localScale.y * pivotY, heightMode, false);
				destinationDirection = worldPosDestination - transform.position;
				if (destinationDirection == Misc.Vector3zero)
				{
					worldPosDestination = map.Map2DToWorldPosition(endingMap2DLocation, currentAltitude,
						transform.localScale.y * pivotY, heightMode, false);
					destinationDirection = worldPosDestination - transform.position;
				}
			}
		}

		public void PerformUpdateLoop()
		{
			mouseEventProcessedThisFrame = false;

			if (isMoving)
			{
				if (onFixedMoveEnabled)
				{
					// Updates game object position
					if (isAffectedByBuoyancy)
					{
						transform.rotation = lastComputedRotation;
						isAffectedByBuoyancy = false;
					}

					MoveGameObject();
					UpdateTransformAndVisibility();
					CheckEvents();

					// Follow object?
					if (_follow)
					{
						var t = Lerp.EaseOut((map.time - followStartTime) / followDuration);
						var zoomLevel = Mathf.Lerp(followStartZoomLevel, followZoomLevel, t);
						var loc = Vector2.Lerp(followStart2DLocation, _currentMap2DLocation, t);
						map.FlyToLocation(loc, 0, zoomLevel);
					}
				}
			}
			else
				CheckBuoyancyEffect();
		}

		private void MoveGameObject()
		{
			var canMove = true;
			var hasMoved = false;
			var moveHasEnded = false;
			var elapsed = map.time - stepTime;

			// Lerp translate
			progress = 0;
			if (elapsed >= duration || duration <= 0)
			{
				progress = 1.0f;
				isMoving = false;
				if (duration > 0 && !isStatic)
					moveHasEnded = true;
			}
			else
			{
				// Check if GO needs to rotate towards destination first
				if (autoRotation && isVisibleInViewport && !isSprite)
				{
					var projDest = Vector3.ProjectOnPlane(destinationDirection, transform.up);
					var projForward = Vector3.ProjectOnPlane(transform.forward, transform.up);
					var angle = Vector3.Angle(projDest, projForward);
					if (angle > 45.0f)
					{
						// prevents movement until rotation has finished
						stepTime += Time.deltaTime;
						canMove = false;
					}
				}
				if (elapsed > 0)
					progress = GetLerpT(elapsed / duration);
			}

			// Update position and visibility
			if (!isStatic)
			{
				if (route != null)
				{
					var index = (int)((route.Count - 1) * progress);
					var findex = Mathf.Min(index + 1, route.Count - 1);
					destination = route[findex];
					if (routeNextDestinationIndex != findex)
					{
						routeNextDestinationIndex = findex;
						SetupDirection(destination);
					}
					if (canMove)
					{
						var stepStartPos = route[index];
						// Check borders crossing
						if (stepStartPos.x > 0.45f && destination.x < -0.45f)
							stepStartPos.x -= 1f;
						else if (stepStartPos.x < -0.45f && destination.x > 0.45f)
							stepStartPos.x += 1f;
						var t = progress * (route.Count - 1);
						t -= (int)t;
						_currentMap2DLocation = LerpMove(stepStartPos, destination, t);
						hasMoved = true;
					}
				}
				else
				{
					if (canMove)
					{
						_currentMap2DLocation = LerpMove(startingMap2DLocation, destination, progress);
						hasMoved = true;
					}
				}
				if (altitudeEnd != altitudeStart)
				{
					currentAltitude = Mathf.Lerp(altitudeStart, altitudeEnd, progress);
					hasMoved = true;
				}
			}
			else if (progress >= 1f) // end of movement - clear isStatic flag
				isStatic = false;

			CheckWaterPos();

			// Register events
			if (hasMoved)
			{
				if (OnMove != null)
					OnMove(this);
				map.BubbleEvent(map.OnVGOMove, this);
			}

			if (moveHasEnded)
			{
				if (OnMoveEnd != null)
					OnMoveEnd(this);
				map.BubbleEvent(map.OnVGOMoveEnd, this);
			}
		}

		private Vector2 LerpMove(Vector2 from, Vector2 to, float t)
		{
			if (map.wrapHorizontally)
			{
				from = map.Map2DToRenderViewport(from);
				to = map.Map2DToRenderViewport(to);
				to = Vector2.Lerp(from, to, t);
				if (to.x < -0.5f)
					to.x += 1f;
				else if (to.x > 0.5f)
					to.x -= 1f;
				return to;
			}
			return Vector2.Lerp(@from, to, t);
		}

		private float GetLerpT(float t)
		{
			switch (easeType)
			{
				case EASE_TYPE.EaseIn:
					return Lerp.EaseIn(t);
				case EASE_TYPE.EaseOut:
					return Lerp.EaseOut(t);
				case EASE_TYPE.Exponential:
					return Lerp.Exponential(t);
				case EASE_TYPE.SmoothStep:
					return Lerp.SmoothStep(t);
				case EASE_TYPE.SmootherStep:
					return Lerp.SmootherStep(t);
			}
			return t;
		}

		/// <summary>
		/// Updates GO's position, rotation, scale and visibility according to its current map position and direction
		/// </summary>
		public void UpdateTransformAndVisibility()
		{
			UpdateTransformAndVisibility(false);
		}

		private bool lastNormalValid;
		private Vector3 lastSurfaceNormal;
		private Vector2 lastNormalCheckMapPos;

		/// <summary>
		/// Updates GO's position, rotation, scale and visibility according to its current map position and direction
		/// </summary>
		public void UpdateTransformAndVisibility(bool forceUpdateRotation)
		{
			var renderViewportIsTerrain = map.renderViewportIsTerrain;

			// if gameobject is not inside the viewport area, hide it's renderers and early exit
			var wasVisible = isVisibleInViewport;
			if (!renderViewportIsTerrain)
				UpdateVisibility(false);

			if (!updateWhenOffScreen && !isVisibleInViewport)
				return;

			// Updates game object position
			if (isAffectedByBuoyancy)
			{
				transform.rotation = lastComputedRotation;
				isAffectedByBuoyancy = false;
			}

			// Adjust scale
			if (autoScale && !renderViewportIsTerrain)
			{
				if (map.renderViewportIsEnabled)
				{
					var desiredScale = map.renderViewportScaleFactor *
					                   map.renderViewportGOAutoScaleMultiplier;
					var minScale = map.renderViewportGOAutoScaleMin;
					var maxScale = map.renderViewportGOAutoScaleMax;
					if (desiredScale < minScale)
						desiredScale = minScale;
					else if (desiredScale > maxScale)
						desiredScale = maxScale;
					transform.localScale = originalScale * desiredScale;
				}
				else if (transform.localScale != originalScale)
					transform.localScale = originalScale;
			}

			// Adjust world position having into account terrain elevation
			var h = currentAltitude;
			if (arcMultiplier > 1f)
				h += Mathf.Sin(progress * Mathf.PI) * arcMultiplier;
			var worldPos = map.Map2DToWorldPosition(_currentMap2DLocation, h,
				transform.localScale.y * pivotY, heightMode, false);
			transform.position = worldPos;

			// Make it climb up/down the slope
			if (isVisibleInViewport || renderViewportIsTerrain)
			{
				// if not autorotates according to terrain, align it with the renderViewport rotation
				if (isSprite || !autoRotation && !preserveOriginalRotation)
				{
					var rva = map.renderViewport.transform.rotation.eulerAngles;
					if (!renderViewportIsTerrain)
						rva.x += spriteRotation;
					transform.rotation = Quaternion.Euler(rva);
					// otherwise, calculate the normal and align to it
				}
				else if ((isMoving || forceUpdateRotation) && autoRotation)
				{
					Vector3 normal;
					if (map.renderViewportIsEnabled)
					{
						if (lastNormalCheckMapPos != _currentMap2DLocation)
						{
							lastNormalValid = map.wrapHorizontally
								? map.RenderViewportGetNormal(_currentMap2DLocation, out normal)
								: map.RenderViewportGetNormal(worldPos, out normal);
							lastNormalCheckMapPos = _currentMap2DLocation;
							lastSurfaceNormal = normal;
						}
						else
							normal = lastSurfaceNormal;
					}
					else
					{
						lastNormalValid = true;
						normal = -map.transform.forward;
					}
					if (lastNormalValid)
					{
						if (destinationDirection != Misc.Vector3zero && !isSprite)
						{
							// Head to target
							if (!wasVisible)
							{
								// set direction immediately as the unit appears in the viewport
								SetupDirection(destination);
								var destRotation = Quaternion.LookRotation(destinationDirection, normal);
								transform.rotation = destRotation;
							}
							else
							{
								var destRotation = Quaternion.LookRotation(destinationDirection, normal);
								transform.rotation = Quaternion.Slerp(transform.rotation, destRotation,
									rotationSpeed);
							}
						}
						else // Orient to slope
							transform.rotation = Quaternion.LookRotation(normal) * Misc.QuaternionX90;
					}
				}
			}

			lastComputedRotation = transform.rotation;
			CheckBuoyancyEffect();
		}

		/// <summary>
		/// Updates the visibility of the unit. Units outside of viewport area are hidden automatically.
		/// </summary>
		/// <param name="forceRefreshVisibility">If set to <c>true</c> force refresh visibility.</param>
		public void UpdateVisibility(bool forceRefreshVisibility)
		{
			var shouldBeVisible = visible;

			if (map.renderViewportIsTerrain) { }
			else
				shouldBeVisible = shouldBeVisible &&
				                  (!map.renderViewportIsEnabled ||
				                   gameObject.activeSelf &&
				                   map.renderViewportRect.Contains(
					                   map.Map2DToRenderViewport(_currentMap2DLocation)));

			if (shouldBeVisible != isVisibleInViewport || forceRefreshVisibility)
			{
				isVisibleInViewport = shouldBeVisible;
				if (forceRefreshVisibility)
					UpdateManagedRenderersList();
				var count = managedRenderers.Count;
				for (var rrk = 0; rrk < count; rrk++)
					managedRenderers[rrk].enabled = isVisibleInViewport;

				if (OnVisibleChange != null)
					OnVisibleChange(this);
			}
		}

		/// <summary>
		/// Fills the managedRenderers array with the current renderer components on this unit (including the children)
		/// </summary>
		public void UpdateManagedRenderersList()
		{
			if (managedRenderers == null)
				managedRenderers = new List<Renderer>();
			GetComponentsInChildren(true, managedRenderers);
		}

		private void CheckWaterPos()
		{
			if (terrainCapability == TERRAIN_CAPABILITY.OnlyWater)
				_isOnWater = true;
			else if (terrainCapability == TERRAIN_CAPABILITY.OnlyGround)
				_isOnWater = false;
			else if (onWaterPosition != _currentMap2DLocation)
			{
				onWaterPosition = _currentMap2DLocation;
				_isOnWater = map.ContainsWater(_currentMap2DLocation);
			}
		}

		public void CheckBuoyancyEffect()
		{
			if (enableBuoyancyEffect &&
			    terrainCapability != TERRAIN_CAPABILITY.OnlyGround &&
			    map.VGOBuoyancyAmplitude > 0 &&
			    map.lastKnownZoomLevel <= map.VGOBuoyancyMaxZoomLevel &&
			    isOnWater)
			{
				// Buoyancy effect
				transform.rotation = Quaternion.Lerp(transform.rotation,
					Quaternion.Euler(map.VGOBuoyancyCurrentAngle, 0, 0) * lastComputedRotation, 0.2f);
				isAffectedByBuoyancy = true;
			}
		}

		private void CheckEvents()
		{
			if (map == null)
				return;
			var countryIndex = -1;
			if (OnCountryEnter != null || map.OnVGOCountryEnter != null)
			{
				countryIndex = map.GetCountryIndex(_currentMap2DLocation);
				if (countryIndex != lastCountryIndex)
				{
					lastCountryIndex = countryIndex;
					if (countryIndex >= 0)
					{
						if (OnCountryEnter != null)
							OnCountryEnter(this);
						if (map.OnVGOCountryEnter != null)
							map.BubbleEvent(map.OnVGOCountryEnter, this);
					}
				}
			}

			if (OnCountryRegionEnter != null || map.OnVGOCountryRegionEnter != null)
			{
				var regionIndex = map.GetCountryRegionIndex(_currentMap2DLocation);
				if (regionIndex != lastCountryRegionIndex)
				{
					lastCountryRegionIndex = regionIndex;
					if (regionIndex >= 0)
					{
						if (OnCountryRegionEnter != null)
							OnCountryRegionEnter(this);
						if (map.OnVGOCountryRegionEnter != null)
							map.BubbleEvent(map.OnVGOCountryRegionEnter, this);
					}
				}
			}

			if (OnProvinceEnter != null)
			{
				var provinceIndex = map.GetProvinceIndex(_currentMap2DLocation, countryIndex);
				if (provinceIndex != lastProvinceIndex)
				{
					lastProvinceIndex = provinceIndex;
					if (provinceIndex >= 0)
					{
						if (OnProvinceEnter != null)
							OnProvinceEnter(this);
						if (map.OnVGOProvinceEnter != null)
							map.BubbleEvent(map.OnVGOProvinceEnter, this);
					}
				}
			}

			if (OnProvinceRegionEnter != null || map.OnVGOProvinceRegionEnter != null)
			{
				var regionIndex = map.GetProvinceRegionIndex(_currentMap2DLocation);
				if (regionIndex != lastProvinceRegionIndex)
				{
					lastProvinceRegionIndex = regionIndex;
					if (regionIndex >= 0)
					{
						if (OnProvinceRegionEnter != null)
							OnProvinceRegionEnter(this);
						if (map.OnVGOProvinceRegionEnter != null)
							map.BubbleEvent(map.OnVGOProvinceRegionEnter, this);
					}
				}
			}
		}

		private IEnumerator FireThis(float delay, GameObjectAnimator bulletAnim, Vector3 startAnchor,
			Vector2 destination, float bulletSpeed, float arcMultiplier = 1f, bool testAnchor = false)
		{
			yield return new WaitForSeconds(delay);
			var renderer = bulletAnim.gameObject.GetComponent<Renderer>();
			if (renderer != null)
				renderer.enabled = true;
			bulletAnim.heightMode = HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED;
			bulletAnim.pivotY = 0f;
			bulletAnim.autoRotation = false;
			bulletAnim.terrainCapability = TERRAIN_CAPABILITY.Any;
			bulletAnim.autoScale = autoScale;
			bulletAnim.arcMultiplier = arcMultiplier;
			var worldPos = transform.TransformPoint(startAnchor);
			if (testAnchor)
			{
				bulletAnim.transform.position = worldPos;
				yield break;
			}
			var startingPosition = map.WorldToMap2DPosition(worldPos);
			bulletAnim.startingMap2DLocation = startingPosition;
			var h = map.WorldToAltitude(worldPos);
			bulletAnim.altitudeStart = h;
			bulletAnim.altitudeEnd = 0;
			var duration = Vector2.Distance(destination, startingPosition) * 200f / bulletSpeed;
			bulletAnim.MoveTo(destination, duration);
		}

		private List<Vector2> CellsToVector2List(List<int> route)
		{
			if (route == null)
				return null;
			var cc = route.Count;
			var path = new List<Vector2>(cc);
			for (var c = 0; c < cc; c++)
				path.Add(map.cells[route[c]].center);
			return path;
		}

		#endregion
	}
}