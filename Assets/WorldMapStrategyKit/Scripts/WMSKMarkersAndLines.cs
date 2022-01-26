// World Strategy Kit for Unity - Main Script
// (C) 2016-2020 by Ramiro Oliva (Kronnect)
// Don't modify this script - changes could be lost if you upgrade to a more recent version of WMSK

using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public partial class WMSK : MonoBehaviour
	{
		private const float EARTH_RADIUS_KM = 6371f;

		#region Public properties

		[SerializeField] private bool
			_showCursor = true;

		/// <summary>
		/// Toggle cursor lines visibility.
		/// </summary>
		public bool showCursor
		{
			get => _showCursor;
			set
			{
				if (value != _showCursor)
				{
					_showCursor = value;
					isDirty = true;

					if (cursorLayerVLine != null)
						cursorLayerVLine.SetActive(_showCursor);
					if (cursorLayerHLine != null)
						cursorLayerHLine.SetActive(_showCursor);
				}
			}
		}

		/// <summary>
		/// Cursor lines color.
		/// </summary>
		[SerializeField] private Color
			_cursorColor = new(0.56f, 0.47f, 0.68f);

		public Color cursorColor
		{
			get
			{
				if (cursorMatH != null)
					return cursorMatH.color;
				return _cursorColor;
			}
			set
			{
				if (value != _cursorColor)
				{
					_cursorColor = value;
					isDirty = true;

					if (cursorMatH != null && _cursorColor != cursorMatH.color)
						cursorMatH.color = _cursorColor;
					if (cursorMatV != null && _cursorColor != cursorMatV.color)
						cursorMatV.color = _cursorColor;
				}
			}
		}

		[SerializeField] private bool
			_cursorFollowMouse = true;

		/// <summary>
		/// Makes the cursor follow the mouse when it's over the World.
		/// </summary>
		public bool cursorFollowMouse
		{
			get => _cursorFollowMouse;
			set
			{
				if (value != _cursorFollowMouse)
				{
					_cursorFollowMouse = value;
					isDirty = true;
				}
			}
		}

		private Vector3
			_cursorLocation;

		public Vector3 cursorLocation
		{
			get => _cursorLocation;
			set
			{
				if (_cursorLocation.x != value.x ||
				    _cursorLocation.z != value.z ||
				    _cursorLocation.y != value.y)
				{
					_cursorLocation = value;
					if (cursorLayerVLine != null)
					{
						var pos = cursorLayerVLine.transform.localPosition;
						cursorLayerVLine.transform.localPosition =
							new Vector3(_cursorLocation.x, 0, pos.z);
					}
					if (cursorLayerHLine != null)
					{
						var pos = cursorLayerHLine.transform.localPosition;
						cursorLayerHLine.transform.localPosition =
							new Vector3(0, _cursorLocation.y, pos.z);
					}
				}
			}
		}

		/// <summary>
		/// If set to false, cursor will be hidden when mouse if not over the map.
		/// </summary>
		[SerializeField] private bool
			_cursorAllwaysVisible = true;

		public bool cursorAlwaysVisible
		{
			get => _cursorAllwaysVisible;
			set
			{
				if (value != _cursorAllwaysVisible)
				{
					_cursorAllwaysVisible = value;
					isDirty = true;
					CheckCursorVisibility();
				}
			}
		}

		[SerializeField] private bool
			_showLatitudeLines = true;

		/// <summary>
		/// Toggle latitude lines visibility.
		/// </summary>
		public bool showLatitudeLines
		{
			get => _showLatitudeLines;
			set
			{
				if (value != _showLatitudeLines)
				{
					_showLatitudeLines = value;
					isDirty = true;

					if (latitudeLayer != null)
						latitudeLayer.SetActive(_showLatitudeLines);
					else if (_showLatitudeLines)
						DrawLatitudeLines();
					if (_showLatitudeLines)
						showGrid = false;
				}
			}
		}

		[SerializeField, Range(5.0f, 45.0f)] private int
			_latitudeStepping = 15;

		/// <summary>
		/// Specify latitude lines separation.
		/// </summary>
		public int latitudeStepping
		{
			get => _latitudeStepping;
			set
			{
				if (value != _latitudeStepping)
				{
					_latitudeStepping = value;
					isDirty = true;

					if (gameObject.activeInHierarchy)
						DrawLatitudeLines();
				}
			}
		}

		[SerializeField] private bool
			_showLongitudeLines = true;

		/// <summary>
		/// Toggle longitude lines visibility.
		/// </summary>
		public bool showLongitudeLines
		{
			get => _showLongitudeLines;
			set
			{
				if (value != _showLongitudeLines)
				{
					_showLongitudeLines = value;
					isDirty = true;

					if (longitudeLayer != null)
						longitudeLayer.SetActive(_showLongitudeLines);
					else if (_showLongitudeLines)
						DrawLongitudeLines();
					if (_showLongitudeLines)
						showGrid = false;
				}
			}
		}

		[SerializeField, Range(5.0f, 45.0f)] private int
			_longitudeStepping = 15;

		/// <summary>
		/// Specify longitude lines separation.
		/// </summary>
		public int longitudeStepping
		{
			get => _longitudeStepping;
			set
			{
				if (value != _longitudeStepping)
				{
					_longitudeStepping = value;
					isDirty = true;

					if (gameObject.activeInHierarchy)
						DrawLongitudeLines();
				}
			}
		}

		/// <summary>
		/// Color for imaginary lines (longitude and latitude).
		/// </summary>
		[SerializeField] private Color
			_imaginaryLinesColor = new(0.16f, 0.33f, 0.498f);

		public Color imaginaryLinesColor
		{
			get
			{
				if (imaginaryLinesMat != null)
					return imaginaryLinesMat.color;
				return _imaginaryLinesColor;
			}
			set
			{
				if (value != _imaginaryLinesColor)
				{
					_imaginaryLinesColor = value;
					isDirty = true;

					if (imaginaryLinesMat != null && _imaginaryLinesColor != imaginaryLinesMat.color)
						imaginaryLinesMat.color = _imaginaryLinesColor;
				}
			}
		}

		#endregion

		#region Public API area

		/// <summary>
		/// Adds a custom marker (sprite) to the map on specified location and with custom scale.
		/// </summary>
		/// <param name="sprite">Sprite gameObject.</param>
		/// <param name="planeLocation">Plane location.</param>
		/// <param name="scale">Scale.</param>
		/// <param name="enableEvents">If set to <c>true</c> enable events, a MarkerClickHandler script will be attached to the sprite gameObject. You can use the OnMarkerClick field to hook your mouse click handler.</param>
		public void AddMarker2DSprite(GameObject sprite, Vector3 planeLocation, float scale,
			bool enableEvents = false)
		{
			AddMarker2DSprite(sprite, planeLocation, new Vector2(scale, scale * mapWidth / mapHeight),
				enableEvents);
		}

		/// <summary>
		/// Adds a custom marker (sprite) to the map on specified location and with custom scale.
		/// </summary>
		/// <param name="sprite">Sprite.</param>
		/// <param name="planeLocation">Plane location.</param>
		/// <param name="scale">Scale for x and y axis.</param>
		/// <param name="enableEvents">If set to <c>true</c> enable events, a MarkerClickHandler script will be attached to the sprite gameObject. You can use the OnMarkerClick field to hook your mouse click handler.</param>
		public void AddMarker2DSprite(GameObject sprite, Vector3 planeLocation, Vector2 scale,
			bool enableEvents = false)
		{
			if (sprite == null)
				return;

			CheckMarkersLayer();

			sprite.transform.SetParent(markersLayer.transform, false);
			sprite.transform.localPosition = planeLocation + Misc.Vector3back * 0.01f;
			sprite.transform.localRotation = Quaternion.Euler(0, 0, 0);
			sprite.layer = gameObject.layer;
			sprite.transform.localScale = new Vector3(scale.x, scale.y, 1f);

			if (renderViewportIsEnabled)
				SetGameObjectLayer(sprite);

			if (enableEvents)
				if (GetComponent<MarkerClickHandler>() == null)
					sprite.AddComponent<MarkerClickHandler>().map = this;
		}

		private void SetGameObjectLayer(GameObject o)
		{
			var rr = o.GetComponentsInChildren<Renderer>(true);
			for (var k = 0; k < rr.Length; k++)
				rr[k].gameObject.layer = gameObject.layer;
		}

		/// <summary>
		/// Adds a custom text to the map on specified location and with custom scale.
		/// </summary>
		public TextMesh AddMarker2DText(string text, Vector3 planeLocation)
		{
			CheckMarkersLayer();

			var textObj = new GameObject(text);
			textObj.transform.SetParent(markersLayer.transform, false);
			textObj.transform.localPosition = planeLocation + Misc.Vector3back * 0.01f;
			textObj.transform.localRotation = Quaternion.Euler(0, 0, 0);
			textObj.transform.localScale = new Vector3(0.01f / mapWidth, 0.01f / mapHeight, 1f);
			textObj.layer = gameObject.layer;
			if (renderViewportIsEnabled)
				textObj.layer = gameObject.layer;
			var tm = textObj.AddComponent<TextMesh>();
			tm.text = text;
			tm.anchor = TextAnchor.MiddleCenter;
			tm.fontSize = (int)mapHeight * 10;
			tm.alignment = TextAlignment.Center;
			return tm;
		}

		/// <summary>
		/// Adds a custom marker (gameobject) to the map on specified location and with custom scale multiplier.
		/// </summary>
		public void AddMarker3DObject(GameObject marker, Vector3 planeLocation, float scale = 1f)
		{
			AddMarker3DObject(marker, planeLocation, marker.transform.localScale * scale);
		}

		/// <summary>
		/// Adds a custom marker (gameobject) to the map on specified location and with custom scale.
		/// </summary>
		public void AddMarker3DObject(GameObject marker, Vector3 planeLocation, Vector3 scale,
			float pivotY = 0.5f)
		{
			// Try to get the height of the object
			float height = 0;
			if (marker.GetComponent<MeshFilter>() != null)
				height = marker.GetComponent<MeshFilter>().sharedMesh.bounds.size.y;
			else if (marker.GetComponent<Collider>() != null)
				height = marker.GetComponent<Collider>().bounds.size.y;

			var h = height * scale.y; // lift the marker so it appears on the surface of the map

			CheckMarkersLayer();
			SetGameObjectLayer(marker);

			marker.transform.rotation = transform.rotation *
			                            Quaternion.Euler(-90, 0, 0) *
			                            marker.transform.localRotation;
			marker.transform.localScale = scale;

			marker.transform.SetParent(markersLayer.transform, true);
			marker.transform.localPosition = planeLocation + Misc.Vector3back * h * pivotY;
		}

		/// <summary>
		/// Updates a custom marker (gameobject) position preserving scale and height. Can be used after calling AddMarker3DObject to move units over the 2D map.
		/// </summary>
		public void UpdateMarker3DObjectPosition(GameObject marker, Vector3 planeLocation)
		{
			marker.transform.localPosition = new Vector3(planeLocation.x, planeLocation.y,
				marker.transform.localPosition.z);
		}

		/// <summary>
		/// Adds a line to the 2D map with options (returns the line gameobject).
		/// </summary>
		/// <param name="start">starting location on the plane (-0.5, -0.5)-(0.5,0.5)</param>
		/// <param name="end">end location on the plane (-0.5, -0.5)-(0.5,0.5)</param>
		/// <param name="arcElevation">arc elevation (-0.5 .. 0.5)</param>
		public LineMarkerAnimator AddLine(Vector2 start, Vector2 end, Color color, float arcElevation,
			float lineWidth)
		{
			var path = new[] { start, end };
			var lma = AddLine(path, markerLineMat, arcElevation, lineWidth);
			lma.color = color;
			return lma;
		}

		/// <summary>
		/// Adds a line to the 2D map with options (returns the line gameobject).
		/// </summary>
		/// <param name="points">Sequence of points for the line</param>
		/// <param name="Color">line color</param>
		/// <param name="arcElevation">arc elevation (-0.5 .. 0.5)</param>
		public LineMarkerAnimator AddLine(Vector2[] points, Color color, float arcElevation,
			float lineWidth)
		{
			var lma = AddLine(points, markerLineMat, arcElevation, lineWidth);
			lma.color = color;
			return lma;
		}

		/// <summary>
		/// Adds a line to the 2D map over a Cell edge with options (returns the line gameobject).
		/// </summary>
		/// <param name="points">Sequence of points for the line</param>
		/// <param name="side">the side of the hexagonal cell</param>
		/// <param name="color">line color</param>
		/// <param name="lineWidth">line width</param>
		public LineMarkerAnimator AddLine(int cellIndex, CELL_SIDE side, Color color, float lineWidth)
		{
			var lma = AddLine(cellIndex, side, markerLineMat, lineWidth);
			if (lma != null)
				lma.color = color;
			return lma;
		}

		/// <summary>
		/// Adds a line to the 2D map over a Cell edge with options (returns the line gameobject).
		/// </summary>
		/// <param name="points">Sequence of points for the line</param>
		/// <param name="side">the side of the hexagonal cell</param>
		/// <param name="material">line material</param>
		/// <param name="lineWidth">line width</param>
		public LineMarkerAnimator AddLine(int cellIndex, CELL_SIDE side, Material material,
			float lineWidth)
		{
			if (cells == null || cellIndex < 0 || cellIndex >= cells.Length)
				return null;
			var points = new Vector2[2];
			var cell = cells[cellIndex];
			switch (side)
			{
				case CELL_SIDE.TopLeft:
					points[0] = cell.points[0];
					points[1] = cell.points[1];
					break;
				case CELL_SIDE.Top:
					points[0] = cell.points[1];
					points[1] = cell.points[2];
					break;
				case CELL_SIDE.TopRight:
					points[0] = cell.points[2];
					points[1] = cell.points[3];
					break;
				case CELL_SIDE.BottomRight:
					points[0] = cell.points[3];
					points[1] = cell.points[4];
					break;
				case CELL_SIDE.Bottom:
					points[0] = cell.points[4];
					points[1] = cell.points[5];
					break;
				case CELL_SIDE.BottomLeft:
					points[0] = cell.points[5];
					points[1] = cell.points[0];
					break;
			}
			var lma = AddLine(points, material, 0, lineWidth);
			lma.numPoints = 2;
			return lma;
		}

		/// <summary>
		/// Adds a line to the 2D map from the center of one given cell to another (returns the line gameobject).
		/// </summary>
		/// <param name="points">Sequence of points for the line</param>
		/// <param name="side">the side of the hexagonal cell</param>
		/// <param name="color">line color</param>
		/// <param name="lineWidth">line width</param>
		public LineMarkerAnimator AddLine(int cellIndex1, int cellIndex2, Color color, float lineWidth)
		{
			var lma = AddLine(cellIndex1, cellIndex2, markerLineMat, lineWidth);
			if (lma != null)
				lma.color = color;
			return lma;
		}

		/// <summary>
		/// Adds a line to the 2D map from the center of one given cell to another (returns the line gameobject).
		/// </summary>
		/// <param name="points">Sequence of points for the line</param>
		/// <param name="side">the side of the hexagonal cell</param>
		/// <param name="material">line material</param>
		/// <param name="lineWidth">line width</param>
		public LineMarkerAnimator AddLine(int cellIndex1, int cellIndex2, Material material,
			float lineWidth)
		{
			if (cells == null ||
			    cellIndex1 < 0 ||
			    cellIndex1 >= cells.Length ||
			    cellIndex2 < 0 ||
			    cellIndex2 >= cells.Length)
				return null;
			var points = new Vector2[2];
			points[0] = cells[cellIndex1].center;
			points[1] = cells[cellIndex2].center;
			var lma = AddLine(points, material, 0, lineWidth);
			lma.numPoints = 2;
			return lma;
		}

		/// <summary>
		/// Adds a line to the 2D map with options (returns the line gameobject).
		/// </summary>
		/// <param name="start">starting location on the plane (-0.5, -0.5)-(0.5,0.5)</param>
		/// <param name="end">end location on the plane (-0.5, -0.5)-(0.5,0.5)</param>
		/// <param name="lineMaterial">line material</param>
		/// <param name="arcElevation">arc elevation (-0.5 .. 0.5)</param>
		public LineMarkerAnimator AddLine(Vector2 start, Vector2 end, Material lineMaterial,
			float arcElevation, float lineWidth)
		{
			var path = new[] { start, end };
			return AddLine(path, lineMaterial, arcElevation, lineWidth);
		}

		/// <summary>
		/// Adds a line to the 2D map with options (returns the line gameobject).
		/// </summary>
		/// <param name="points">Sequence of points for the line</param>
		/// <param name="lineMaterial">line material</param>
		/// <param name="arcElevation">arc elevation (-0.5 .. 0.5)</param>
		public LineMarkerAnimator AddLine(Vector2[] points, Material lineMaterial, float arcElevation,
			float lineWidth)
		{
			CheckMarkersLayer();
			var newLine = new GameObject("MarkerLine");
			newLine.layer = gameObject.layer;
			var usesRenderViewport = renderViewportIsEnabled && arcElevation > 0;
			if (!usesRenderViewport)
				newLine.transform.SetParent(markersLayer.transform, false);
			var lma = newLine.AddComponent<LineMarkerAnimator>();
			lma.map = this;
			lma.path = points;
			lma.color = Color.white;
			lma.arcElevation = arcElevation;
			lma.lineWidth = lineWidth;
			lma.lineMaterial = lineMaterial;
			return lma;
		}

		/// <summary>
		/// Adds a custom marker (polygon) to the map on specified location and with custom size in km.
		/// </summary>
		/// <param name="position">Position for the center of the circle.</param>
		/// <param name="kmRadius">Radius in KM.</param>
		/// <param name="ringWidthStart">Ring inner limit (0..1). Pass 0 to draw a full circle.</param>
		/// <param name="ringWidthEnd">Ring outer limit (0..1). Pass 1 to draw a full circle.</param>
		/// <param name="color">Color</param>
		public GameObject AddCircle(Vector2 position, float kmRadius, float ringWidthStart,
			float ringWidthEnd, Color color)
		{
			CheckMarkersLayer();
			var rw = 2.0f * Mathf.PI * EARTH_RADIUS_KM;
			var w = kmRadius / rw;
			var h = w * 2f;
			var marker = Drawing.DrawCircle("MarkerCircle", position, w, h, 0, Mathf.PI * 2.0f,
				ringWidthStart, ringWidthEnd, 64, GetColoredMarkerMaterial(color));
			if (marker != null)
			{
				marker.transform.SetParent(markersLayer.transform, false);
				marker.transform.localPosition = new Vector3(position.x, position.y, -0.01f);
				marker.layer = markersLayer.layer;
			}
			return marker;
		}

		/// <summary>
		/// Deletes all custom markers and lines
		/// </summary>
		public void ClearMarkers()
		{
			if (markersLayer == null)
				return;
			Destroy(markersLayer);
		}

		/// <summary>
		/// Removes all marker lines.
		/// </summary>
		public void ClearLineMarkers()
		{
			if (markersLayer == null)
				return;
			var t = markersLayer.transform.GetComponentsInChildren<LineRenderer>();
			for (var k = 0; k < t.Length; k++)
				Destroy(t[k].gameObject);
		}

		private List<Transform> ttmp;

		/// <summary>
		/// Returns a list of all added markers game objects
		/// </summary>
		/// <returns>The markers.</returns>
		public void GetMarkers(List<Transform> results)
		{
			if (results == null)
				return;
			results.Clear();
			if (markersLayer == null)
				return;
			markersLayer.transform.GetComponentsInChildren(results);
			results.RemoveAt(0); // removes parent
		}

		/// <summary>
		/// Returns a list of all added markers game objects inside a given country
		/// </summary>
		/// <returns>The markers.</returns>
		public void GetMarkers(Country country, List<Transform> results)
		{
			if (results == null || country == null || country.regions == null)
				return;
			GetMarkers(results);
			var cc = results.Count;
			if (ttmp == null)
				ttmp = new List<Transform>(cc);
			else
				ttmp.Clear();
			var countryRegionsCount = country.regions.Count;
			for (var k = 0; k < cc; k++)
			{
				Vector2 pos = markersLayer.transform.InverseTransformPoint(results[k].position);
				for (var r = 0; r < countryRegionsCount; r++)
					if (country.regions[r].Contains(pos))
					{
						ttmp.Add(results[k]);
						break;
					}
			}
			results.Clear();
			results.AddRange(ttmp);
		}

		/// <summary>
		/// Returns a list of all added markers game objects inside a given province
		/// </summary>
		/// <returns>The markers.</returns>
		public void GetMarkers(Province province, List<Transform> results)
		{
			if (results == null || province == null || province.regions == null)
				return;
			GetMarkers(results);
			var cc = results.Count;
			if (ttmp == null)
				ttmp = new List<Transform>(cc);
			else
				ttmp.Clear();
			var provinceRegionsCount = province.regions.Count;
			for (var k = 0; k < cc; k++)
			{
				Vector2 pos = markersLayer.transform.InverseTransformPoint(results[k].position);
				for (var r = 0; r < provinceRegionsCount; r++)
					if (province.regions[r].Contains(pos))
					{
						ttmp.Add(results[k]);
						break;
					}
			}
			results.Clear();
			results.AddRange(ttmp);
		}

		/// <summary>
		/// Returns a list of all added markers game objects inside a given cell
		/// </summary>
		/// <returns>The markers.</returns>
		public void GetMarkers(Cell cell, List<Transform> results)
		{
			if (results == null || cell == null)
				return;
			GetMarkers(results);
			var cc = results.Count;
			if (ttmp == null)
				ttmp = new List<Transform>(cc);
			else
				ttmp.Clear();
			for (var k = 0; k < cc; k++)
			{
				Vector2 pos = markersLayer.transform.InverseTransformPoint(results[k].position);
				if (cell.Contains(pos))
				{
					ttmp.Add(results[k]);
					break;
				}
			}
			results.Clear();
			results.AddRange(ttmp);
		}

		/// <summary>
		/// Returns a list of all added markers game objects inside a given region
		/// </summary>
		/// <returns>The markers.</returns>
		public void GetMarkers(Region region, List<Transform> results)
		{
			if (results == null)
				return;
			GetMarkers(results);
			var cc = results.Count;
			if (ttmp == null)
				ttmp = new List<Transform>(cc);
			else
				ttmp.Clear();
			for (var k = 0; k < cc; k++)
			{
				Vector2 pos = markersLayer.transform.InverseTransformPoint(results[k].position);
				if (region.Contains(pos))
				{
					ttmp.Add(results[k]);
					break;
				}
			}
			results.Clear();
			results.AddRange(ttmp);
		}

		#endregion
	}
}