using UnityEngine;
using UnityEngine.UI;

namespace WorldMapStrategyKit
{
	[ExecuteInEditMode]
	public class WMSKMiniMap : MonoBehaviour
	{
		public const string MINIMAP_NAME = "WMSK_Minimap";

		private Vector4 _normalizedScreenRect;

		public Vector4 normalizedScreenRect
		{
			get => _normalizedScreenRect;
			set
			{
				if (value != _normalizedScreenRect)
				{
					_normalizedScreenRect = value;
					RepositionMiniMap();
				}
			}
		}

		/// <summary>
		/// This is a reference to the main map.
		/// </summary>
		public WMSK primaryMap;

		[Range(0.01f, 1f)] public float zoomLevel = 0.1f;

		[Range(0f, 8f)] public float duration = 2f;

		/// <summary>
		/// Reference to the minimap. Useful for customizing its appearance.
		/// </summary>
		public WMSK map;

		private static WMSKMiniMap _instance;

		/// <summary>
		/// Gets a reference to the minimap
		/// </summary>
		public static WMSKMiniMap instance
		{
			get
			{
				if (_instance == null)
				{
					var go = GameObject.Find(MINIMAP_NAME);
					if (go != null)
						_instance = go.GetComponent<WMSKMiniMap>();
					else
						_instance = FindObjectOfType<WMSKMiniMap>();
				}
				return _instance;
			}
		}

		// Optionally assign an UI element as its parent
		public RectTransform UIParent;

		private Vector3 oldPosition;
		private Vector2 oldSize;
		private Canvas canvas;
		private RawImage imagePlaceholder;
		private Renderer mapRenderer;

		private Vector3[] wc = new Vector3[4];

		/// <summary>
		///	Opens the mini map at the provided normalized screen rect.
		/// </summary>
		/// <param name="screenRect">Screen rectangle in normalized coordinates (0..1)</param>
		public static WMSKMiniMap Show(Vector4 screenRect)
		{
			var minimapObj = GameObject.Find(MINIMAP_NAME);
			if (minimapObj == null)
			{
				minimapObj = Instantiate(Resources.Load<GameObject>("WMSK/Prefabs/MiniMap")) as GameObject;
				minimapObj.name = MINIMAP_NAME;
			}
			var mm = minimapObj.GetComponent<WMSKMiniMap>();
			minimapObj.transform.SetParent(mm.map.cameraMain.transform, false);
			mm.normalizedScreenRect = screenRect;
			return mm;
		}

		/// <summary>
		/// Hides minimap
		/// </summary>
		public static void Hide()
		{
			if (instance != null)
				Destroy(instance.gameObject);
		}

		/// <summary>
		/// Returns true if minimap is visible
		/// </summary>
		public static bool IsVisible() => instance != null;

		/// <summary>
		/// Changes position and size of the minimap.
		/// </summary>
		/// <param name="normalizedScreenRect">Normalized screen rect.</param>
		public static void RepositionAt(Vector4 normalizedScreenRect)
		{
			if (instance != null)
				instance.normalizedScreenRect = normalizedScreenRect;
		}

		private void OnEnable()
		{
			map = GetComponent<WMSK>();
			map.showCountryNames = false;
			map.showCities = false;
			map.showProvinces = false;
			map.showFrontiers = false;
			map.showLatitudeLines = false;
			map.showOutline = false;
			map.showLongitudeLines = false;
			map.frontiersDetail = FRONTIERS_DETAIL.Low;
			map.earthStyle = EARTH_STYLE.Alternate1;
			map.allowUserDrag = false;
			map.allowUserKeys = false;
			map.allowUserZoom = false;
			map.enableCountryHighlight = false;
			map.zoomMaxDistance = 0;
			map.zoomMinDistance = 0;
			map.cursorColor = new Color(0.6f, 0.8f, 1f, 1f);
			map.cursorAlwaysVisible = false;
			map.respectOtherUI = false;
			map.OnClick += (float x, float y, int buttonIndex) =>
			{
				if (primaryMap == null)
					primaryMap = WMSK.instance;
				if (primaryMap != null)
					primaryMap.FlyToLocation(new Vector2(x, y), duration, zoomLevel);
			};
		}

		private Canvas GetTopmostCanvas(RectTransform rt)
		{
			var parentCanvases = rt.GetComponentsInParent<Canvas>();
			if (parentCanvases != null && parentCanvases.Length > 0)
				return parentCanvases[parentCanvases.Length - 1];
			return null;
		}

		private void Update()
		{
			if (UIParent == null)
				return;
			if (UIParent.position == oldPosition && UIParent.sizeDelta == oldSize)
				return;

			if (canvas == null)
				canvas = GetTopmostCanvas(UIParent);
			if (canvas == null)
				return;

			oldPosition = UIParent.position;
			oldSize = UIParent.sizeDelta;
			UIParent.GetWorldCorners(wc);
			Vector3 bl, tr;
			if (canvas.renderMode == RenderMode.ScreenSpaceCamera ||
			    canvas.renderMode == RenderMode.WorldSpace)
			{
				bl = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, wc[0]);
				tr = RectTransformUtility.WorldToScreenPoint(canvas.worldCamera, wc[2]);
			}
			else
			{
				bl = RectTransformUtility.WorldToScreenPoint(null, wc[0]);
				tr = RectTransformUtility.WorldToScreenPoint(null, wc[2]);
			}
			bl.x /= Screen.width;
			bl.y /= Screen.height;
			tr.x /= Screen.width;
			tr.y /= Screen.height;
			normalizedScreenRect = new Vector4(bl.x, bl.y, tr.x - bl.x, tr.y - bl.y);

			if (imagePlaceholder == null)
				imagePlaceholder = UIParent.GetComponent<RawImage>();
			if (mapRenderer == null)
				mapRenderer = map.GetComponent<Renderer>();

			if (imagePlaceholder != null)
			{
				if (Application.isPlaying)
				{
					if (imagePlaceholder.enabled)
						imagePlaceholder.enabled = false;
				}
				else
				{
					if (mapRenderer != null &&
					    imagePlaceholder.texture != mapRenderer.sharedMaterial.mainTexture)
						imagePlaceholder.texture = mapRenderer.sharedMaterial.mainTexture;
				}
			}
		}

		private void RepositionMiniMap()
		{
			var cameraMain = WMSK.instance.cameraMain;
			var z = cameraMain.nearClipPlane + 0.01f;
			var oldRotation = cameraMain.transform.rotation;
			// Sets scale
			cameraMain.transform.rotation = Quaternion.Euler(0, 0, 0);
			var pbl = cameraMain.ViewportToWorldPoint(new Vector3(_normalizedScreenRect.x,
				_normalizedScreenRect.y, z));
			var ptr = cameraMain.ViewportToWorldPoint(new Vector3(
				_normalizedScreenRect.x + _normalizedScreenRect.z,
				_normalizedScreenRect.y + _normalizedScreenRect.w, z));
			transform.localScale = new Vector3(ptr.x - pbl.x, ptr.y - pbl.y, 1f);
			// Sets position
			cameraMain.transform.rotation = oldRotation;
			pbl = cameraMain.ViewportToWorldPoint(new Vector3(_normalizedScreenRect.x,
				_normalizedScreenRect.y, z));
			ptr = cameraMain.ViewportToWorldPoint(new Vector3(
				_normalizedScreenRect.x + _normalizedScreenRect.z,
				_normalizedScreenRect.y + _normalizedScreenRect.w, z));
			transform.position = (pbl + ptr) * 0.5f;
			// Copy rotation
			transform.rotation = cameraMain.transform.rotation;
		}
	}
}