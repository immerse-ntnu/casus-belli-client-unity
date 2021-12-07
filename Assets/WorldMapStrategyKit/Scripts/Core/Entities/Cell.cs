using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public partial class Cell : IFader
	{
		public int row, column;

		/// <summary>
		/// Center of this cell in local space coordinates (-0.5..0.5)
		/// </summary>
		public Vector2 center;

		private JSONObject _attrib;

		/// <summary>
		/// Use this property to add/retrieve custom attributes for this country
		/// </summary>
		public JSONObject attrib
		{
			get
			{
				if (_attrib == null)
					_attrib = new JSONObject();
				return _attrib;
			}
			set => _attrib = value;
		}

		private Vector2[] _points;

		/// <summary>
		/// List of vertices of this cell.
		/// </summary>
		public Vector2[] points
		{
			get
			{
				if (_points != null)
					return _points;
				var pointCount = segments.Length;
				_points = new Vector2[pointCount];
				for (var k = 0; k < pointCount; k++)
					_points[k] = segments[k].start;
				return _points;
			}
		}

		/// <summary>
		/// Segments of this cell. Internal use.
		/// </summary>
		public CellSegment[] segments;

		public Rect rect2D;

		/// <summary>
		/// Temporary material used by Cell*Temporary* functions
		/// </summary>
		public Material tempMaterial;

		public Material customMaterial { get; set; }

		public Vector2 customTextureScale, customTextureOffset;
		public float customTextureRotation;

		public bool isFading { get; set; }

		public bool isWrapped;

		/// <summary>
		/// Internal use.
		/// </summary>
		public bool flag;

		/// <summary>
		/// Reference to the renderer component when surface is present
		/// </summary>
		public Renderer renderer;

		public Cell(int row, int column, Vector2 center)
		{
			this.row = row;
			this.column = column;
			this.center = center;
			segments = new CellSegment[6];
		}

		public bool Contains(Vector2 position)
		{
			if (!rect2D.Contains(position))
				return false;
			var numPoints = points.Length;
			var j = numPoints - 1;
			var inside = false;
			var x = position.x;
			var y = position.y;
			for (var i = 0; i < numPoints; j = i++)
				if ((_points[i].y <= y && y < _points[j].y || _points[j].y <= y && y < _points[i].y) &&
				    x <
				    (_points[j].x - _points[i].x) * (y - _points[i].y) / (_points[j].y - _points[i].y) +
				    _points[i].x)
					inside = !inside;
			return inside;
		}

		public bool Contains(Region region)
		{
			if (region == null || region.points == null)
				return false;
			for (var k = 0; k < region.points.Length; k++)
				if (!Contains(region.points[k]))
					return false;
			return true;
		}

		/// <summary>
		/// Intersection test between two rects
		/// </summary>
		/// <param name="otherRect">Other rect.</param>
		public bool Intersects(Rect otherRect)
		{
			if (otherRect.xMin > rect2D.xMax)
				return false;
			if (otherRect.xMax < rect2D.xMin)
				return false;
			if (otherRect.yMin > rect2D.yMax)
				return false;
			if (otherRect.yMax < rect2D.yMin)
				return false;

			return true;
		}

		/// <summary>
		/// Returns true if one rect crosses other rect but does not contain it (edge crossing)
		/// </summary>
		public bool IntersectsEdgesOnly(Rect otherRect)
		{
			if (otherRect.xMin > rect2D.xMax)
				return false;
			if (otherRect.xMax < rect2D.xMin)
				return false;
			if (otherRect.yMin > rect2D.yMax)
				return false;
			if (otherRect.yMax < rect2D.yMin)
				return false;

			if (otherRect.xMin < rect2D.xMin)
				return true;
			if (otherRect.xMax > rect2D.xMax)
				return true;
			if (otherRect.yMin < rect2D.yMin)
				return true;
			if (otherRect.yMax > rect2D.yMax)
				return true;

			return false;
		}
	}
}