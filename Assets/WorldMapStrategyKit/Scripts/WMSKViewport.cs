// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using UnityEngine;

namespace WorldMapStrategyKit
{
	public enum VIEWPORT_LIGHTING_MODE
	{
		Lit,
		Unlit
	}

	public partial class WMSK : MonoBehaviour
	{
		#region Public properties

		[SerializeField] private float _earthElevation = 1.0f;

		/// <summary>
		/// Ground elevation when viewport is used.
		/// </summary>
		/// <value>The earth elevation.</value>
		public float earthElevation
		{
			get => _earthElevation;
			set
			{
				if (value != _earthElevation)
				{
					_earthElevation = value;
					isDirty = true;
					EarthBuildMesh();
				}
			}
		}

		[SerializeField] private bool _earthCloudLayer;

		/// <summary>
		/// Enables/disables the cloud layer when viewport is used.
		/// </summary>
		public bool earthCloudLayer
		{
			get => _earthCloudLayer;
			set
			{
				if (value != _earthCloudLayer)
				{
					_earthCloudLayer = value;
					isDirty = true;
					UpdateCloudLayer();
				}
			}
		}

		[SerializeField] private float _earthCloudLayerSpeed = 1.2f;

		/// <summary>
		/// Speed of the cloud animation of cloud layer when viewport is used.
		/// </summary>
		public float earthCloudLayerSpeed
		{
			get => _earthCloudLayerSpeed;
			set
			{
				if (value != _earthCloudLayerSpeed)
				{
					_earthCloudLayerSpeed = value;
					isDirty = true;
					UpdateCloudLayer();
				}
			}
		}

		[SerializeField] private float _earthCloudLayerElevation = -5.0f;

		/// <summary>
		/// Elevation of cloud layer when viewport is used.
		/// </summary>
		public float earthCloudLayerElevation
		{
			get => _earthCloudLayerElevation;
			set
			{
				if (value != _earthCloudLayerElevation)
				{
					_earthCloudLayerElevation = value;
					isDirty = true;
					UpdateCloudLayer();
				}
			}
		}

		[SerializeField] private float _earthCloudLayerAlpha = 1.0f;

		/// <summary>
		/// Global alpha for the optional cloud layer when viewport is used.
		/// </summary>
		public float earthCloudLayerAlpha
		{
			get => _earthCloudLayerAlpha;
			set
			{
				if (value != _earthCloudLayerAlpha)
				{
					_earthCloudLayerAlpha = value;
					isDirty = true;
					UpdateCloudLayer();
				}
			}
		}

		[SerializeField] private float _earthCloudLayerShadowStrength = 0.35f;

		/// <summary>
		/// Global alpha for the optional cloud layer when viewport is used.
		/// </summary>
		public float earthCloudLayerShadowStrength
		{
			get => _earthCloudLayerShadowStrength;
			set
			{
				if (value != _earthCloudLayerShadowStrength)
				{
					_earthCloudLayerShadowStrength = value;
					isDirty = true;
					UpdateCloudLayer();
				}
			}
		}

		[SerializeField] private GameObject
			_renderViewport;

		/// <summary>
		/// Target gameobject to display de map (optional)
		/// </summary>
		public GameObject renderViewport
		{
			get => _renderViewport;
			set
			{
				if (value == null)
					value = gameObject;
				if (value != _renderViewport)
				{
					if (_renderViewport != null)
					{
						GetCurrentMapLocation(out lastKnownMapCoordinates);
						if (_currentCamera != null)
							GetZoomLevel(); // updates lastKnownZoomLevel
					}
					AssignRenderViewport(value);
					isDirty = true;
					SetupViewport();
					RepositionViewportObjects();
					RepositionCamera();
				}
			}
		}

		[SerializeField] private RectTransform _renderViewportUIPanel;

		/// <summary>
		/// Panel placeholder where viewport is positioned
		/// </summary>
		public RectTransform renderViewportUIPanel
		{
			get => _renderViewportUIPanel;
			set
			{
				if (value != _renderViewportUIPanel)
				{
					_renderViewportUIPanel = value;
					panelUIOldSize = Vector2.zero;
					isDirty = true;
					SetupViewport();
				}
			}
		}

		private Rect _renderViewportRect;

		/// <summary>
		/// Returns the visible rectangle of the map represented by current GameView viewport location and zoom
		/// </summary>
		public Rect renderViewportRect
		{
			get
			{
				ComputeViewportRect();
				return _renderViewportRect;
			}
		}

		/// <summary>
		/// Returns the visible rectangle of the map represented by current SceneView viewport location and zoom
		/// </summary>
		public Rect renderViewportRectFromSceneView
		{
			get
			{
				Rect rect;
				ComputeViewportRect(true);
				rect = _renderViewportRect;
				lastRenderViewportGood = false;
				ComputeViewportRect();
				return rect;
			}
		}

		[SerializeField] private float _renderViewportResolution = 2;

		/// <summary>
		/// Quality of render viewport. This is a factor of the screen width. x2 is good for antialiasis. x1 equals to screen width.
		/// </summary>
		public float renderViewportResolution
		{
			get => _renderViewportResolution;
			set
			{
				if (value != _renderViewportResolution)
				{
					_renderViewportResolution = value;
					isDirty = true;
					SetupViewport();
				}
			}
		}

		[SerializeField] private int _renderViewportResolutionMaxRTWidth = 2048;

		/// <summary>
		/// Maximum width for the render texture. A value of 2048 is the recommended for most cases.
		/// </summary>
		public int renderViewportResolutionMaxRTWidth
		{
			get => _renderViewportResolutionMaxRTWidth;
			set
			{
				if (value != _renderViewportResolutionMaxRTWidth)
				{
					_renderViewportResolutionMaxRTWidth = value;
					isDirty = true;
					SetupViewport();
				}
			}
		}

		/// <summary>
		/// Returns true if render viewport is a terrain
		/// </summary>
		public bool renderViewportIsTerrain => viewportMode == ViewportMode.Terrain;

		/// <summary>
		/// Returns true if render viewport is a map panel UI element
		/// </summary>
		public bool renderViewportIsMapPanel => viewportMode == ViewportMode.MapPanel;

		/// <summary>
		/// Returns true if render viewport is a 3D viewport
		/// </summary>
		public bool renderViewportIs3DViewport => viewportMode == ViewportMode.Viewport3D;

		[SerializeField] private FilterMode _renderViewportFilterMode = FilterMode.Trilinear;

		public FilterMode renderViewportFilterMode
		{
			get => _renderViewportFilterMode;
			set
			{
				if (_renderViewportFilterMode != value)
				{
					_renderViewportFilterMode = value;
					isDirty = true;
					SetupViewport();
				}
			}
		}

		[SerializeField]
		private VIEWPORT_LIGHTING_MODE _renderViewportLightingMode = VIEWPORT_LIGHTING_MODE.Lit;

		public VIEWPORT_LIGHTING_MODE renderViewportLightingMode
		{
			get => _renderViewportLightingMode;
			set
			{
				if (_renderViewportLightingMode != value)
				{
					_renderViewportLightingMode = value;
					SetupViewport();
				}
			}
		}

		[SerializeField] private float _renderViewportCurvature;

		/// <summary>
		/// Curvature of render viewport
		/// </summary>
		public float renderViewportCurvature
		{
			get => _renderViewportCurvature;
			set
			{
				if (value != _renderViewportCurvature)
				{
					_renderViewportCurvature = value;
					isDirty = true;
					SetupViewport();
				}
			}
		}

		[SerializeField] private float _renderViewportCurvatureMinZoom;

		/// <summary>
		/// Curvature of render viewport when zoom is at minimum
		/// </summary>
		public float renderViewportCurvatureMinZoom
		{
			get => _renderViewportCurvatureMinZoom;
			set
			{
				if (value != _renderViewportCurvatureMinZoom)
				{
					_renderViewportCurvatureMinZoom = value;
					isDirty = true;
					SetupViewport();
				}
			}
		}

		[SerializeField] private RenderingPath _renderViewportRenderingPath = RenderingPath.Forward;

		public RenderingPath renderViewportRenderingPath
		{
			get => _renderViewportRenderingPath;
			set
			{
				if (_renderViewportRenderingPath != value)
				{
					_renderViewportRenderingPath = value;
					isDirty = true;
					SetupViewport();
				}
			}
		}

		[SerializeField] private float _renderViewportTerrainAlpha = 1.0f;

		/// <summary>
		/// Global alpha for the WMSK texture projectino on Unity terrain
		/// </summary>
		public float renderViewportTerrainAlpha
		{
			get => _renderViewportTerrainAlpha;
			set
			{
				if (value != _renderViewportTerrainAlpha)
				{
					_renderViewportTerrainAlpha = value;
					isDirty = true;
					lastMainCameraPos = Misc.Vector3zero; // forces terrain viewport refresh
				}
			}
		}

		[SerializeField] private float _renderViewportGOAutoScaleMultiplier = 1f;

		/// <summary>
		/// Global scale multiplier for game objects put on top of the viewport.
		/// </summary>
		public float renderViewportGOAutoScaleMultiplier
		{
			get => _renderViewportGOAutoScaleMultiplier;
			set
			{
				if (value != _renderViewportGOAutoScaleMultiplier)
				{
					_renderViewportGOAutoScaleMultiplier = value;
					isDirty = true;
					UpdateViewportObjectsVisibility();
				}
			}
		}

		[SerializeField] private float _renderViewportGOAutoScaleMin = 1f;

		/// <summary>
		/// Minimum scale applied to game objects on the viewport.
		/// </summary>
		public float renderViewportGOAutoScaleMin
		{
			get => _renderViewportGOAutoScaleMin;
			set
			{
				if (value != _renderViewportGOAutoScaleMin)
				{
					_renderViewportGOAutoScaleMin = value;
					isDirty = true;
					UpdateViewportObjectsVisibility();
				}
			}
		}

		[SerializeField] private float _renderViewportGOAutoScaleMax = 10f;

		/// <summary>
		/// Maximum scale applied to game objects on the viewport.
		/// </summary>
		public float renderViewportGOAutoScaleMax
		{
			get => _renderViewportGOAutoScaleMax;
			set
			{
				if (value != _renderViewportGOAutoScaleMax)
				{
					_renderViewportGOAutoScaleMax = value;
					isDirty = true;
					UpdateViewportObjectsVisibility();
				}
			}
		}

		[SerializeField] private GameObject _sun;

		public GameObject sun
		{
			get => _sun;
			set
			{
				if (value != _sun)
				{
					_sun = value;
					UpdateSun();
				}
			}
		}

		[SerializeField] private bool _sunUseTimeOfDay;

		/// <summary>
		/// Whether the rotation of the Sun can be controlled using the timeOfDay property (0-24h)
		/// </summary>
		public bool sunUseTimeOfDay
		{
			get => _sunUseTimeOfDay;
			set
			{
				if (value != _sunUseTimeOfDay)
				{
					_sunUseTimeOfDay = value;
					isDirty = true;
				}
			}
		}

		[SerializeField] private float _timeOfDay;

		/// <summary>
		/// Simulated time of day (0-24). This would move the light gameobject orientation referenced by sun property around the map.
		/// </summary>
		public float timeOfDay
		{
			get => _timeOfDay;
			set
			{
				if (value != _timeOfDay)
				{
					_timeOfDay = value;
					isDirty = true;
					UpdateSun();
				}
			}
		}

		#endregion

		#region Viewport APIs

		public bool renderViewportIsEnabled => viewportMode != ViewportMode.None;

		/// <summary>
		/// Computes the interpolated, perspective adjusted or not, height on given position.
		/// </summary>
		public float ComputeEarthHeight(Vector2 position, bool perspectiveAjusted)
		{
			if (position.x < -0.5f || position.x > 0.5f || position.y < -0.5f || position.y > 0.5f)
				return 0;

			position.x += 0.5f;
			position.y += 0.5f;

			var x0 = Mathf.FloorToInt(position.x * heightmapTextureWidth);
			var y0 = Mathf.FloorToInt(position.y * heightmapTextureHeight);
			var x1 = x0 + 1;
			if (x1 >= heightmapTextureWidth - 1)
				x1 = heightmapTextureWidth - 1;
			var y1 = y0 + 1;
			if (y1 >= heightmapTextureHeight - 1)
				y1 = heightmapTextureHeight - 1;

			var pos00 = y0 * heightmapTextureWidth + x0;
			var pos10 = y0 * heightmapTextureWidth + x1;
			var pos01 = y1 * heightmapTextureWidth + x0;
			var pos11 = y1 * heightmapTextureWidth + x1;
			var elev00 = viewportElevationPoints[pos00];
			var elev10 = viewportElevationPoints[pos10];
			var elev01 = viewportElevationPoints[pos01];
			var elev11 = viewportElevationPoints[pos11];
			if (perspectiveAjusted)
			{
				elev00 *= _renderViewportElevationFactor;
				elev10 *= _renderViewportElevationFactor;
				elev01 *= _renderViewportElevationFactor;
				elev11 *= _renderViewportElevationFactor;
			}

			var cellWidth = 1.0f / heightmapTextureWidth;
			var cellHeight = 1.0f / heightmapTextureHeight;
			var cellx = (position.x - x0 * cellWidth) / cellWidth;
			var celly = (position.y - y0 * cellHeight) / cellHeight;

			var elev = elev00 * (1.0f - cellx) * (1.0f - celly) +
			           elev10 * cellx * (1.0f - celly) +
			           elev01 * (1.0f - cellx) * celly +
			           elev11 * cellx * celly;

			return elev;
		}

		/// <summary>
		/// Returns the surface normal of the renderViewport at the position in map coordinates.
		/// </summary>
		public bool RenderViewportGetNormal(Vector2 mapPosition, out Vector3 normal)
		{
			if (_wrapHorizontally)
			{
				if (mapPosition.x >= 0.5f)
					mapPosition.x -= 1f;
				else if (mapPosition.x <= -0.5f)
					mapPosition.x += 1f;
			}
			Vector3 worldPos;
			if (renderViewportIsTerrain)
			{
				// Terrain mode
				worldPos = transform.TransformPoint(mapPosition);
				worldPos.y -= WMSK_TERRAIN_MODE_Y_OFFSET;
			}
			else // Viewport mode
				worldPos = _renderViewport.transform.TransformPoint(mapPosition);
			return RenderViewportGetNormal(worldPos, out normal);
		}

		/// <summary>
		/// Returns the surface normal of the renderViewport at the position in World coordinates.
		/// </summary>
		public bool RenderViewportGetNormal(Vector3 worldPosition, out Vector3 normal)
		{
			if (renderViewportIsTerrain)
			{
				var dist = terrain.terrainData.size.y;
				var ray = new Ray(worldPosition + Misc.Vector3up * dist, Misc.Vector3down);
				if (terrainHits == null)
					terrainHits = new RaycastHit[20];
				var hitCount = Physics.RaycastNonAlloc(ray, terrainHits, dist + 1f);
				for (var i = 0; i < hitCount; i++)
					if (terrainHits[i].transform.gameObject == terrain.gameObject)
					{
						normal = terrainHits[i].normal;
						return true;
					}
			}
			else
			{
				RaycastHit hit;
				var ray = new Ray(worldPosition - _renderViewport.transform.forward * 50.0f,
					_renderViewport.transform.forward);
				if (Physics.Raycast(ray, out hit, 100.0f, layerMask))
				{
					normal = hit.normal;
					return true;
				}
			}
			normal = Misc.Vector3zero;
			return false;
		}

		/// <summary>
		/// Returns the zoom level required to show the entire rect in local map coordinates
		/// </summary>
		/// <returns>The country zoom level of -1 if error.</returns>
		public float GetZoomExtents(Rect rect) =>
			GetFrustumZoomLevel(rect.width * mapWidth, rect.height * mapHeight);

		#endregion
	}
}