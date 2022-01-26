// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public delegate void OnMouseClick(float x, float y, int buttonIndex);

	public delegate void OnMouseEvent(float x, float y);

	public delegate void OnRectangleSelection(Rect rectangle, bool finishedSelection);

	public delegate void OnSimpleMapEvent();

	public enum UNIT_SELECTION_MODE
	{
		Disabled = 0,
		Single = 10
	}

	public partial class WMSK : MonoBehaviour
	{
		#region Public properties

		/// <summary>
		/// Raised when user clicks on the map using left mouse button. Returns x/y local space coordinates (-0.5, 0.5)
		/// </summary>
		public event OnMouseClick OnClick;

		/// <summary>
		/// Occurs when mouse moves over the map.
		/// </summary>
		public event OnMouseEvent OnMouseMove;

		/// <summary>
		/// Occurs when mouse button is pressed on the map.
		/// </summary>
		public event OnMouseClick OnMouseDown;

		/// <summary>
		/// Occurs when mouse button is released on the map.
		/// </summary>
		public event OnMouseClick OnMouseRelease;

		/// <summary>
		/// Occurs when user start dragging on the map
		/// </summary>
		public event OnSimpleMapEvent OnDragStart;

		/// <summary>
		/// Occurs when user ends dragging on the map (release mouse button after a drag) 
		/// </summary>
		public event OnSimpleMapEvent OnDragEnd;

		/// <summary>
		/// Occurs when a FlyTo command is started
		/// </summary>
		public event OnSimpleMapEvent OnFlyStart;

		/// <summary>
		/// Occurs when a FlyTo command has reached destination
		/// </summary>
		public event OnSimpleMapEvent OnFlyEnd;

		/// <summary>
		/// Returns true is mouse has entered the Earth's collider.
		/// </summary>
		[NonSerialized] public bool
			mouseIsOver;

		/// <summary>
		/// Returns true is mouse is over an Unity UI element (button, label, ...)
		/// </summary>
		[NonSerialized] public bool
			mouseIsOverUIElement;

		/// <summary>
		/// The navigation time in seconds.
		/// </summary>
		[SerializeField, Range(1.0f, 16.0f)] private float
			_navigationTime = 4.0f;

		public float navigationTime
		{
			get => _navigationTime;
			set
			{
				if (_navigationTime != value)
				{
					_navigationTime = value;
					isDirty = true;
				}
			}
		}

		/// <summary>
		/// Returns whether a navigation is taking place at this moment.
		/// </summary>
		public bool isFlying => flyToActive;

		[SerializeField] private bool
			_fitWindowWidth = true;

		/// <summary>
		/// Ensure the map is always visible and occupy the entire Window.
		/// </summary>
		public bool fitWindowWidth
		{
			get => _fitWindowWidth;
			set
			{
				if (value != _fitWindowWidth)
				{
					_fitWindowWidth = value;
					isDirty = true;
					if (_fitWindowWidth)
					{
						CenterMap();
						_wrapHorizontally = false;
					}
					else if (!_fitWindowHeight)
						maxFrustumDistance = float.MaxValue;
				}
			}
		}

		[SerializeField] private bool
			_fitWindowHeight = true;

		/// <summary>
		/// Ensure the map is always visible and occupy the entire Window.
		/// </summary>
		public bool fitWindowHeight
		{
			get => _fitWindowHeight;
			set
			{
				if (value != _fitWindowHeight)
				{
					_fitWindowHeight = value;
					isDirty = true;
					if (_fitWindowHeight)
						CenterMap();
					else if (!fitWindowWidth)
						maxFrustumDistance = float.MaxValue;
				}
			}
		}

		[SerializeField] private Vector2
			_flyToScreenCenter = Misc.ViewportCenter;

		/// <summary>
		/// Sets the position of the screen used by the FlyTo() operations
		/// </summary>
		public Vector2 flyToScreenCenter
		{
			get => _flyToScreenCenter;
			set
			{
				if (value != _flyToScreenCenter)
				{
					value.x = Mathf.Clamp01(value.x);
					value.y = Mathf.Clamp01(value.y);
					_flyToScreenCenter = value;
					SetDestinationAndDistance(_cursorLocation, 0, GetFrustumDistance());
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool _wrapHorizontally;

		/// <summary>
		/// Allows to scroll around horizontal edges.
		/// </summary>
		public bool wrapHorizontally
		{
			get => _wrapHorizontally;
			set
			{
				if (value != _wrapHorizontally)
				{
					_wrapHorizontally = value;
					isDirty = true;
					_fitWindowWidth = !_wrapHorizontally;
					SetupViewport();
					if (_showGrid)
						GenerateGrid(); // need to refresh grid mesh
					if (!_wrapHorizontally)
						CenterMap();
				}
			}
		}

		[SerializeField] private Rect _windowRect = new(-0.5f, -0.5f, 1, 1);

		/// <summary>
		/// The playable area. By default it's a Rect(-0.5, -0.5, 1f, 1f) where x,y are the -0.5, -0.5 is the left/bottom corner, and 1,1 is the width/height. Use renderViewportRect to get the current map viewable area in the window.
		/// </summary>
		/// <value>The window rect.</value>
		public Rect windowRect
		{
			get => _windowRect;
			set
			{
				if (value != _windowRect)
				{
					_windowRect = value;
					fitWindowHeight = true;
					fitWindowWidth = true;
					isDirty = true;
					CenterMap();
				}
			}
		}

		[SerializeField] private bool
			_allowUserKeys;

		/// <summary>
		/// If user can use WASD keys to drag the map.
		/// </summary>
		public bool allowUserKeys
		{
			get => _allowUserKeys;
			set
			{
				if (value != _allowUserKeys)
				{
					_allowUserKeys = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private float _dragKeySpeedMultiplier = 0.1f;

		public float dragKeySpeedMultiplier
		{
			get => _dragKeySpeedMultiplier;
			set
			{
				if (value != dragKeySpeedMultiplier)
				{
					_dragKeySpeedMultiplier = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private KeyCode
			_keyUp = KeyCode.W;

		/// <summary>
		/// Keyboard mapping for up shift.
		/// </summary>
		public KeyCode keyUp
		{
			get => _keyUp;
			set
			{
				if (value != _keyUp)
				{
					_keyUp = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private KeyCode
			_keyDown = KeyCode.S;

		/// <summary>
		/// Keyboard mapping for down shift.
		/// </summary>
		public KeyCode keyDown
		{
			get => _keyDown;
			set
			{
				if (value != _keyDown)
				{
					_keyDown = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private KeyCode
			_keyLeft = KeyCode.A;

		/// <summary>
		/// Keyboard mapping for left shift.
		/// </summary>
		public KeyCode keyLeft
		{
			get => _keyLeft;
			set
			{
				if (value != _keyLeft)
				{
					_keyLeft = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private KeyCode
			_keyRight = KeyCode.D;

		/// <summary>
		/// Keyboard mapping for right shift.
		/// </summary>
		public KeyCode keyRight
		{
			get => _keyRight;
			set
			{
				if (value != _keyRight)
				{
					_keyRight = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool
			_dragFlipDirection;

		/// <summary>
		/// Whether the direction of the drag should be inverted.
		/// </summary>
		public bool dragFlipDirection
		{
			get => _dragFlipDirection;
			set
			{
				if (value != _dragFlipDirection)
				{
					_dragFlipDirection = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool
			_dragConstantSpeed;

		/// <summary>
		/// Whether the drag should follow a constant movement, withouth acceleration.
		/// </summary>
		public bool dragConstantSpeed
		{
			get => _dragConstantSpeed;
			set
			{
				if (value != _dragConstantSpeed)
				{
					_dragConstantSpeed = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private float _dragDampingDuration = 0.5f;

		public float dragDampingDuration
		{
			get => _dragDampingDuration;
			set
			{
				if (value != _dragDampingDuration)
				{
					_dragDampingDuration = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool
			_allowUserDrag = true;

		public bool allowUserDrag
		{
			get => _allowUserDrag;
			set
			{
				if (value != _allowUserDrag)
				{
					_allowUserDrag = value;
					dragDirection = Misc.Vector3zero;
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool
			_allowScrollOnScreenEdges;

		public bool allowScrollOnScreenEdges
		{
			get => _allowScrollOnScreenEdges;
			set
			{
				if (value != _allowScrollOnScreenEdges)
				{
					_allowScrollOnScreenEdges = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private int
			_screenEdgeThickness = 2;

		public int screenEdgeThickness
		{
			get => _screenEdgeThickness;
			set
			{
				if (value != _screenEdgeThickness)
				{
					_screenEdgeThickness = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool
			_centerOnRightClick = true;

		public bool centerOnRightClick
		{
			get => _centerOnRightClick;
			set
			{
				if (value != _centerOnRightClick)
				{
					_centerOnRightClick = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool
			_allowUserZoom = true;

		public bool allowUserZoom
		{
			get => _allowUserZoom;
			set
			{
				if (value != _allowUserZoom)
				{
					_allowUserZoom = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private float _zoomMaxDistance = 10f;

		public float zoomMaxDistance
		{
			get => _zoomMaxDistance;
			set
			{
				if (value != _zoomMaxDistance)
				{
					_zoomMaxDistance = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private float _zoomMinDistance = 0.01f;

		public float zoomMinDistance
		{
			get => _zoomMinDistance;
			set
			{
				if (value != _zoomMinDistance)
				{
					_zoomMinDistance = Mathf.Clamp01(value);
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool
			_invertZoomDirection;

		public bool invertZoomDirection
		{
			get => _invertZoomDirection;
			set
			{
				if (value != _invertZoomDirection)
				{
					_invertZoomDirection = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private float _zoomDampingDuration = 0.5f;

		public float zoomDampingDuration
		{
			get => _zoomDampingDuration;
			set
			{
				if (value != _zoomDampingDuration)
				{
					_zoomDampingDuration = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool
			_respectOtherUI = true;

		/// <summary>
		/// When enabled, will prevent interaction with the map if pointer is over an UI element
		/// </summary>
		public bool respectOtherUI
		{
			get => _respectOtherUI;
			set
			{
				if (value != _respectOtherUI)
				{
					_respectOtherUI = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool _enableFreeCamera;

		/// <summary>
		/// Allow camera to be freely moved/rotated when in terrain mode
		/// </summary>
		public bool enableFreeCamera
		{
			get => _enableFreeCamera;
			set
			{
				if (value != _enableFreeCamera)
				{
					_enableFreeCamera = value;
					if (!_enableFreeCamera && renderViewportIsTerrain)
					{
						var mapPos = Misc.Vector3zero;
						GetCurrentMapLocation(out mapPos);
						FlyToLocation(mapPos, 0.8f); // reset view
					}
					isDirty = true;
				}
			}
		}

		[SerializeField, Range(0.1f, 3)] private float
			_mouseWheelSensitivity = 0.5f;

		public float mouseWheelSensitivity
		{
			get => _mouseWheelSensitivity;
			set
			{
				if (value != _mouseWheelSensitivity)
				{
					_mouseWheelSensitivity = value;
					isDirty = true;
				}
			}
		}

		[SerializeField, Range(0.1f, 3)] private float
			_mouseDragSensitivity = 0.5f;

		public float mouseDragSensitivity
		{
			get => _mouseDragSensitivity;
			set
			{
				if (value != _mouseDragSensitivity)
				{
					_mouseDragSensitivity = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private int
			_mouseDragThreshold;

		public int mouseDragThreshold
		{
			get => _mouseDragThreshold;
			set
			{
				if (_mouseDragThreshold != value)
				{
					_mouseDragThreshold = value;
					isDirty = true;
				}
			}
		}

		#endregion

		#region Public API area

		/// <summary>
		/// Moves the map in front of the camera so it fits the viewport.
		/// </summary>
		public void CenterMap()
		{
			if (isMiniMap)
				return;

			if (_renderViewport == null)
				SetupViewport();

			var distance = GetFrustumDistance();
			SetDestinationAndDistance(Misc.Vector2zero, 0, distance);

			CheckRectConstraints();
		}

		/// <summary>
		/// Returns the coordinates of the center of the map as it's shown on the screen
		/// </summary>
		public bool GetCurrentMapLocation(out Vector3 location)
		{
			Vector3 screenPos;
			if (renderViewportIsEnabled && !renderViewportIsTerrain)
				screenPos = cameraMain.WorldToScreenPoint(_renderViewport.transform.position);
			else
				screenPos = new Vector3(cameraMain.pixelWidth / 2, cameraMain.pixelHeight / 2, 0f);
			return GetLocalHitFromScreenPos(screenPos, out location, true);
		}

		/// <summary>
		/// Sets the zoom level progressively
		/// </summary>
		/// <param name="zoomLevel">Value from 0 to 1 (close zoom, fit to window zoom)</param>
		/// <param name="duration">Duratin of the transition</param>
		public void SetZoomLevel(float zoomLevel, float duration)
		{
			if (duration == 0)
				SetZoomLevel(zoomLevel);
			else
			{
				Vector3 location;
				GetCurrentMapLocation(out location);
				FlyToLocation(location, duration, zoomLevel);
			}
		}

		/// <summary>
		/// Sets the zoom level
		/// </summary>
		/// <param name="zoomLevel">Value from 0 to 1</param>
		public void SetZoomLevel(float zoomLevel)
		{
			var cam = currentCamera;
			if (cam.orthographic)
			{
				var aspect = cam.aspect;
				float frustumDistanceH;
				if (_fitWindowWidth)
					frustumDistanceH = mapWidth * 0.5f / aspect;
				else
					frustumDistanceH = mapHeight * 0.5f;
				zoomLevel = Mathf.Clamp01(zoomLevel);
				cam.orthographicSize = Mathf.Max(frustumDistanceH * zoomLevel, 1);
			}
			else
			{
				// Takes the distance from the focus point and adjust it according to the zoom level
				Vector3 dest;
				if (GetLocalHitFromScreenPos(new Vector3(Screen.width * 0.5f, Screen.height * 0.5f),
					out dest, true))
					dest = transform.TransformPoint(dest);
				else
					dest = transform.position;
				var distance = GetZoomLevelDistance(zoomLevel);
				cam.transform.position = dest - (dest - cam.transform.position).normalized * distance;
				var minDistance = cam.nearClipPlane + 0.0001f;
				var camDistance = (dest - cam.transform.position).magnitude;
				// Last distance
				lastDistanceFromCamera = camDistance;
				if (camDistance < minDistance)
					cam.transform.position = dest - transform.forward * minDistance;
			}
		}

		/// <summary>
		/// Gets the current zoom level (0..1)
		/// </summary>
		public float GetZoomLevel()
		{
			float distanceToCamera;
			var cam = currentCamera;
			if (cam == null)
				return 1;

			if (_enableFreeCamera)
			{
				var mapPlane = new Plane(transform.forward, transform.position);
				var ray = new Ray(cam.transform.position, cam.transform.forward);
				if (mapPlane.Raycast(ray, out distanceToCamera))
				{
					var h = transform.localScale.y;
					return (distanceToCamera - _zoomMinDistance * h) /
					       (_zoomMaxDistance * h - _zoomMinDistance * h);
				}
			}

			float frustumDistanceW, frustumDistanceH;
			var aspect = cam.aspect;
			if (cam.orthographic)
			{
				if (_fitWindowWidth)
					frustumDistanceH = mapWidth * 0.5f / aspect;
				else
					frustumDistanceH = mapHeight * 0.5f;
				return cam.orthographicSize / frustumDistanceH;
			}

			var fv = cam.fieldOfView;
			var radAngle = fv * Mathf.Deg2Rad;
			frustumDistanceH = mapHeight * 0.5f / Mathf.Tan(radAngle * 0.5f);
			frustumDistanceW = mapWidth / aspect * 0.5f / Mathf.Tan(radAngle * 0.5f);
			float distance;
			if (_fitWindowWidth)
				distance = Mathf.Max(frustumDistanceH, frustumDistanceW);
			else
				distance = Mathf.Min(frustumDistanceH, frustumDistanceW);
			// Takes the distance from the camera to the plane //focus point and adjust it according to the zoom level
			var plane = new Plane(transform.forward, transform.position);
			distanceToCamera = Mathf.Abs(plane.GetDistanceToPoint(cam.transform.position));
			lastKnownZoomLevel = distanceToCamera / distance;
			return lastKnownZoomLevel;
		}

		/// <summary>
		/// Starts navigation to target location in local 2D coordinates.
		/// </summary>
		public void FlyToLocation(Vector2 destination)
		{
			FlyToLocation(destination, _navigationTime, GetZoomLevel());
		}

		/// <summary>
		/// Starts navigation to target location in local 2D coordinates.
		/// </summary>
		public void FlyToLocation(Vector2 destination, float duration)
		{
			FlyToLocation(destination, duration, GetZoomLevel());
		}

		/// <summary>
		/// Starts navigation to target location in local 2D coordinates.
		/// </summary>
		public void FlyToLocation(float x, float y)
		{
			FlyToLocation(new Vector2(x, y), _navigationTime, GetZoomLevel());
		}

		/// <summary>
		/// Starts navigation to target location in local 2D coordinates.
		/// </summary>
		public void FlyToLocation(float x, float y, float duration)
		{
			SetDestination(new Vector2(x, y), duration, GetZoomLevel());
		}

		/// <summary>
		/// Starts navigation to target location in local 2D coordinates with target zoom level.
		/// </summary>
		public void FlyToLocation(Vector2 destination, float duration, float zoomLevel)
		{
			SetDestination(destination, duration, zoomLevel);
		}

		/// <summary>
		/// Starts navigation to target lat/lon.
		/// </summary>
		/// <param name="latlon">Latitude (x) and Longitude (y).</param>
		public void FlyToLatLon(Vector2 latlon, float duration, float zoomLevel)
		{
			FlyToLatLon(latlon.x, latlon.y, duration, zoomLevel);
		}

		/// <summary>
		/// Starts navigation to target lat/lon.
		/// </summary>
		/// <param name="latitude">Latitude.</param>
		/// <param name="longitude">Longitude.</param>
		public void FlyToLatLon(float latitude, float longitude, float duration, float zoomLevel)
		{
			var location = Conversion.GetLocalPositionFromLatLon(latitude, longitude);
			FlyToLocation(location, duration, zoomLevel);
		}

		/// <summary>
		/// Initiates a rectangle selection operation.
		/// </summary>
		/// <returns>The rectangle selection.</returns>
		public GameObject RectangleSelectionInitiate(OnRectangleSelection rectangleSelectionCallback,
			Color rectangleFillColor, Color rectangleColor, float lineWidth = 0.02f)
		{
			RectangleSelectionCancel();
			var rectangle = GameObject.CreatePrimitive(PrimitiveType.Quad);
			if (rectangleSelectionMat == null)
			{
				rectangleSelectionMat =
					Instantiate(Resources.Load<Material>("WMSK/Materials/hudRectangleSelection"));
				if (disposalManager != null)
					disposalManager.MarkForDisposal(rectangleSelectionMat);
			}
			rectangleSelectionMat.color = rectangleFillColor;
			rectangle.GetComponent<Renderer>().sharedMaterial = rectangleSelectionMat;
			AddMarker2DSprite(rectangle, _cursorLocation, 0f);
			var rs = rectangle.AddComponent<RectangleSelection>();
			rs.map = this;
			rs.callback = rectangleSelectionCallback;
			rs.lineColor = rectangleColor;
			rs.lineWidth = lineWidth;
			if (Input.GetMouseButton(0))
				rs.InitiateSelection(lastMouseMapHitPos.x, lastMouseMapHitPos.y, 0);
			return rectangle;
		}

		/// <summary>
		/// Cancel any rectangle selection operation in progress
		/// </summary>
		public void RectangleSelectionCancel()
		{
			var rrss = GetComponentsInChildren<RectangleSelection>(true);
			for (var k = 0; k < rrss.Length; k++)
				DestroyImmediate(rrss[k].gameObject);
		}

		/// <summary>
		/// Returns true if a rectangle selection is occuring
		/// </summary>
		public bool rectangleSelectionInProgress
		{
			get
			{
				var rs = GetComponentInChildren<RectangleSelection>();
				return rs != null;
			}
		}

		#endregion
	}
}