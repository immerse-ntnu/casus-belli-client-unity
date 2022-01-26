// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System;
using UnityEditor;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public enum VIEWPORT_QUALITY
	{
		Low = 0,
		Medium = 1,
		High = 2
	}

	public enum HEIGHT_OFFSET_MODE
	{
		ABSOLUTE_ALTITUDE = 0,
		ABSOLUTE_CLAMPED = 1,
		RELATIVE_TO_GROUND = 2
	}

	public partial class WMSK : MonoBehaviour
	{
		#region Public properties

		private static WMSK _instance;

		/// <summary>
		/// Instance of the world map. Use this property to access World Map functionality.
		/// </summary>
		public static WMSK instance
		{
			get
			{
				if (_instance == null)
				{
					var candidates = FindObjectsOfType<WMSK>();
					WMSK miniMap = null;
					for (var k = 0; k < candidates.Length; k++)
					{
						var c = candidates[k];
#if UNITY_EDITOR
						if (EditorUtility.IsPersistent(c.gameObject))
							continue; // exclude prefabs
#endif
						if (c.isMiniMap)
						{
							miniMap = c;
							continue;
						}
						_instance = c;
						break;
					}
					if (miniMap != null)
						return miniMap;
					if (_instance == null)
						Debug.LogWarning(
							"'WorldMapStrategyKit' GameObject could not be found in the scene. Make sure it's created with this name before using any map functionality.");
				}
				if (_instance != null && _instance.countries == null)
					_instance.Init();
				return _instance;
			}
		}

		public static WMSK GetInstance(Transform t)
		{
			WMSK map = null;
			while (t != null)
			{
				map = t.GetComponent<WMSK>();
				t = t.parent;
			}
			if (map == null)
				map = instance;
			return map;
		}

		public static bool instanceExists
		{
			get
			{
				if (_instance == null)
				{
					var obj = GameObject.Find("WorldMapStrategyKit");
					if (obj == null)
						return false;
					_instance = obj.GetComponent<WMSK>();
					if (_instance == null)
						return false;
				}
				return true;
			}
		}

		/// <summary>
		/// Pause or un-pause WMSK interaction, unit movement, etc.
		/// </summary>
		[NonSerialized] public bool paused;

		/// <summary>
		/// In-game elapsed time from start. When paused is set to true, time is frozen.
		/// </summary>
		[NonSerialized] public float time;

		[NonSerialized] public float timeSpeed = 1f;

		[SerializeField] private Camera _customCamera;

		/// <summary>
		/// Set this property to false to force WMSK not cache materials
		/// </summary>
		[NonSerialized] public bool cacheMaterials = true;

		/// <summary>
		/// Optional main camera
		/// </summary>
		public Camera customCamera
		{
			get => _customCamera;
			set
			{
				if (_customCamera != value)
				{
					_customCamera = value;
					isDirty = true;
				}
			}
		}

		/// <summary>
		/// Gets the current main camera. To use a different camera, use the customCamera property.
		/// </summary>
		/// <value>The camera main.</value>
		public Camera cameraMain => _customCamera == null ? Camera.main : _customCamera;

		[SerializeField] private bool _prewarm;

		/// <summary>
		/// Precomputes big country surfaces and path finding matrices during initialization to allow smoother performance during play.
		/// </summary>
		public bool prewarm
		{
			get => _prewarm;
			set
			{
				if (_prewarm != value)
				{
					_prewarm = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool _enableEnclaves;

		/// <summary>
		/// Allows regions surrounded completely by another different region of a different country
		/// </summary>
		public bool enableEnclaves
		{
			get => _enableEnclaves;
			set
			{
				if (_enableEnclaves != value)
				{
					_enableEnclaves = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private string _geodataResourcesPath = "WMSK/Geodata";

		/// <summary>
		/// Path where geodata files reside. This path is a relative path below Resources folder. So a geodata file would be read as Resources/<geodataResourcesPath>/cities10 for example.
		/// Note that your project can contain several Resources folders. Create your own Resources folder so you don't have to backup your geodata folder on each update if you make any modifications to the files.
		/// </summary>
		public string geodataResourcesPath
		{
			get => _geodataResourcesPath;
			set
			{
				if (_geodataResourcesPath != value)
				{
					_geodataResourcesPath = value.Trim();
					if (_geodataResourcesPath.Length < 1)
						_geodataResourcesPath = "WMSK/Geodata";
					var lc = _geodataResourcesPath.Substring(_geodataResourcesPath.Length - 1, 1);
					if (lc.Equals("/") || lc.Equals("\\"))
						_geodataResourcesPath =
							_geodataResourcesPath.Substring(0, _geodataResourcesPath.Length - 1);
					isDirty = true;
				}
			}
		}

		[SerializeField] private Texture2D _heightMapTexture;

		/// <summary>
		/// The heightmap texture used in WMSK
		/// </summary>
		public Texture2D heightMapTexture
		{
			get => _heightMapTexture;
			set
			{
				if (_heightMapTexture != value)
				{
					_heightMapTexture = value;
					earthLastElevation = -1;
					if (renderViewportIsEnabled)
						UpdateViewport();
					isDirty = true;
				}
			}
		}

		[SerializeField] private bool _dontLoadGeodataAtStart;

		/// <summary>
		/// When set to true, WMSK won't load default geodata contents during startup. You should manually call ReloadData() or create your map procedurally.
		/// </summary>
		public bool dontLoadGeodataAtStart
		{
			get => _dontLoadGeodataAtStart;
			set => _dontLoadGeodataAtStart = value;
		}

		#endregion

		#region Public API area

		/// <summary>
		/// Returns the position in map local coordinates (x, y)
		/// </summary>
		public Vector2 WorldToMap2DPosition(Vector3 position)
		{
			switch (viewportMode)
			{
				case ViewportMode.None:
					return transform.InverseTransformPoint(position);
				case ViewportMode.Terrain:
					position.z += WMSK_TERRAIN_MODE_Y_OFFSET;
					return transform.InverseTransformPoint(position);
				default:
					var viewportPos = _renderViewport.transform.InverseTransformPoint(position);
					viewportPos.x += 0.5f;
					viewportPos.y += 0.5f;
					viewportPos.z = lastDistanceFromCamera;
					var worldPos = currentCamera.ViewportToWorldPoint(viewportPos);
					return transform.InverseTransformPoint(worldPos);
			}
		}

		public float WorldToAltitude(Vector3 position)
		{
			var mapPos = WorldToMap2DPosition(position);
			var wpos = Map2DToWorldPosition(mapPos, 0f);
			return Vector3.Distance(position, wpos) / _renderViewportElevationFactor;
		}

		/// <summary>
		/// Returns a 2D coordinate compatible with wrapping mode.
		/// For instance, if current render viewport views part of the left-side map on the right side, then
		/// the returned coordinate will have an x > 0.5f
		/// </summary>
		public Vector2 Map2DToRenderViewport(Vector2 position)
		{
			if (!_wrapHorizontally)
				return position;
			if (position.x < _renderViewportRect.xMax - 1f)
				position.x += 1f;
			else if (position.x - 1f > _renderViewportRect.xMin)
				position.x -= 1f;
			return position;
		}

		/// <summary>
		/// Returns the world position of the given map coordinate.
		/// This takes into account the viewport and ground elevation is used,
		/// unless you pass -1 to height which will assume absolute 0 height.
		/// </summary>
		public Vector3 Map2DToWorldPosition(Vector2 position, float height) =>
			Map2DToWorldPosition(position, height, HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED, false);

		/// <summary>
		/// Returns the world position of the given map coordinate.
		/// This takes into account the viewport and ground elevation is used,
		/// unless you pass -1 to height which will assume absolute 0 height.
		/// If viewport is enabled, you can use the ignoreViewport param to return the flat 2D Map position.
		/// Use heightOffsetMode to position wisely the height:
		/// - Absolute Altitude will return an absolute height irrespective of altitude at map point (it can cross ground)
		/// - Absolute Clamped will return either the ground altitude or the absolute height (the greater value)
		/// - Relative to the ground will simply add the height to the ground altitude
		/// </summary>
		public Vector3 Map2DToWorldPosition(Vector2 position, float height,
			HEIGHT_OFFSET_MODE heightOffsetMode, bool ignoreViewport) =>
			Map2DToWorldPosition(position, height, 0, heightOffsetMode, ignoreViewport);

		/// <summary>
		/// Returns the world position of the given map coordinate.
		/// This takes into account the viewport and ground elevation is used,
		/// unless you pass -1 to height which will assume absolute 0 height.
		/// If viewport is enabled, you can use the ignoreViewport param to return the flat 2D Map position.
		/// Use heightOffsetMode to position wisely the height:
		/// - Absolute Altitude will return an absolute height irrespective of altitude at map point (it can cross ground)
		/// - Absolute Clamped will return either the ground altitude or the absolute height (the greater value)
		/// - Relative to the ground will simply add the height to the ground altitude
		/// BaseHeight is always refered to the lower bottom of a gameobject.
		/// If computing the position of a single point in space, pass 0 to pivotHeight. Otherwhise, pass the y position of the center of the game object to pivotHeight.
		/// For example, if want to position a sphere, pass the desired altitude to baseHeight and sphere.transform.localScale.y * 0.5f to pivotHeight.
		/// </summary>
		public Vector3 Map2DToWorldPosition(Vector2 position, float baseHeight, float pivotHeight,
			HEIGHT_OFFSET_MODE heightOffsetMode, bool ignoreViewport)
		{
			if (!renderViewportIsEnabled || ignoreViewport)
				return transform.TransformPoint(position);

			Vector3 worldPos;
			if (renderViewportIsTerrain)
			{
				// Terrain mode
				worldPos = transform.TransformPoint(position);
				switch (heightOffsetMode)
				{
					case HEIGHT_OFFSET_MODE.RELATIVE_TO_GROUND:
						worldPos.y = terrain.SampleHeight(worldPos) + baseHeight;
						break;
					case HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED:
						var y = baseHeight - WMSK_TERRAIN_MODE_Y_OFFSET;
						worldPos.y = Mathf.Max(y, terrain.SampleHeight(worldPos));
						break;
					case HEIGHT_OFFSET_MODE.ABSOLUTE_ALTITUDE:
						worldPos.y += baseHeight - WMSK_TERRAIN_MODE_Y_OFFSET;
						break;
				}
			}
			else
			{
				// Viewport
				baseHeight *= _renderViewportElevationFactor;
				switch (heightOffsetMode)
				{
					case HEIGHT_OFFSET_MODE.RELATIVE_TO_GROUND:
						baseHeight += ComputeEarthHeight(position, true);
						break;
					case HEIGHT_OFFSET_MODE.ABSOLUTE_CLAMPED:
						baseHeight = Mathf.Max(baseHeight, ComputeEarthHeight(position, true));
						break;
					case HEIGHT_OFFSET_MODE.ABSOLUTE_ALTITUDE:
						break;
				}
				var height = baseHeight + pivotHeight;

				position = Map2DToRenderViewport(position); // makes it compatible with wrapping mode
				worldPos = transform.TransformPoint(position); // converts it to world position
				var viewportPos =
					_currentCamera
						.WorldToViewportPoint(
							worldPos); // maps to camera clip space which equals to current render viewport view

				viewportPos.x -= 0.5f;
				viewportPos.y -= 0.5f;
				if (currentCurvature != 0)
					height -= Mathf.Cos(viewportPos.x * 3.1415927f) * currentCurvature;

				worldPos = _renderViewport.transform.TransformPoint(new Vector3(viewportPos.x,
					viewportPos.y, -height)); // convert to world space again
			}
			return worldPos;
		}

		// Destroys everything: countries, frontiers, cities, mountpoints
		public void ClearAll()
		{
			DestroyGridSurfaces();
			DestroySurfaces();
			DestroyMapLabels();
			countries = new Country[0];
			if (_provinces != null)
				provinces = new Province[0];
			if (_cities != null)
				cities = new City[0];
			if (mountPoints != null)
				mountPoints.Clear();
			OptimizeFrontiers();
			Redraw();
		}

		/// <summary>
		/// Enables Calculator component and returns a reference to its API.
		/// </summary>
		public WMSK_Calculator calc =>
			GetComponent<WMSK_Calculator>() ?? gameObject.AddComponent<WMSK_Calculator>();

		/// <summary>
		/// Enables Ticker component and returns a reference to its API.
		/// </summary>
		public WMSK_Ticker ticker => GetComponent<WMSK_Ticker>() ?? gameObject.AddComponent<WMSK_Ticker>();

		/// <summary>
		/// Enables Decorator component and returns a reference to its API.
		/// </summary>
		public WMSK_Decorator decorator =>
			GetComponent<WMSK_Decorator>() ?? gameObject.AddComponent<WMSK_Decorator>();

		/// <summary>
		/// Enables Editor component and returns a reference to its API.
		/// </summary>
		public WMSK_Editor editor => GetComponent<WMSK_Editor>() ?? gameObject.AddComponent<WMSK_Editor>();

		public delegate bool AttribPredicate(JSONObject json);

		#endregion
	}
}