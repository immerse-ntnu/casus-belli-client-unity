using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class Region : IFader
	{
		/// <summary>
		/// Region border
		/// </summary>
		public Vector2[] points;

		/// <summary>
		/// Center of this region
		/// </summary>
		public Vector2 center;

		/// <summary>
		/// 2D rect enclosing all points
		/// </summary>
		public Rect rect2D;

		/// <summary>
		/// Equals to rect2D.width * rect2D.height - precomputed for performance purposes in comparison functions
		/// </summary>
		public float rect2DArea;

		public Material customMaterial { get; set; }

		public Vector2 customTextureScale, customTextureOffset;
		public float customTextureRotation;

		public List<Region> neighbours { get; set; }

		/// <summary>
		/// Used by the extrusion APIs
		/// </summary>
		public float extrusionAmount;

		/// <summary>
		/// Country or province whose this region belongs to
		/// </summary>
		/// <value>The entity.</value>
		public IAdminEntity entity { get; set; }

		public int regionIndex { get; set; }

		public bool isFading { get; set; }

		/// <summary>
		/// Some operations require to sanitize the region point list. This flag determines if the point list changed and should pass a sanitize call.
		/// </summary>
		public bool sanitized;

		/// <summary>
		/// Internal cache for path finding engine.
		/// </summary>
		public List<int> pathFindingPositions;

		/// <summary>
		/// Optional custom texture for borders. Overrides any general outline setting.
		/// </summary>
		public Texture2D customBorderTexture;

		public float customBorderWidth = 0.1f;
		public float customBorderTextureTiling = 1f;
		public Color customBorderTintColor = Color.white;
		public float customBorderAnimationSpeed;
		public float customBorderAnimationStartTime;
		public float customBorderAnimationAcumOffset;

		private static Dictionary<Vector2, bool> dictPoints;

		public struct CurvedLabelInfo
		{
			public Vector2 axisStart;
			public Vector2 axisEnd;
			public float axisAngle;
			public Vector2 axisAveragedThickness;
			public Vector2 axisMidDisplacement;
			public Vector2 p0, p1;
			public bool isDirty;
		}

		public CurvedLabelInfo curvedLabelInfo;

		public Region(IAdminEntity entity, int regionIndex)
		{
			this.entity = entity;
			this.regionIndex = regionIndex;
			sanitized = true;
			neighbours = new List<Region>();
		}

		public Region Clone()
		{
			var c = new Region(entity, regionIndex);
			c.center = center;
			c.rect2D = rect2D;
			c.rect2DArea = rect2DArea;
			c.customMaterial = customMaterial;
			c.customTextureScale = customTextureScale;
			c.customTextureOffset = customTextureOffset;
			c.customTextureRotation = customTextureRotation;
			c.points = new Vector2[points.Length];
			Array.Copy(points, c.points, points.Length);
			c.customBorderTexture = customBorderTexture;
			c.customBorderWidth = customBorderWidth;
			c.customBorderTextureTiling = customBorderTextureTiling;
			c.customBorderTintColor = customBorderTintColor;
			c.extrusionAmount = extrusionAmount;
			return c;
		}

		public bool Contains(Vector2 p)
		{
			if (!rect2D.Contains(p))
				return false;

			var numPoints = points.Length;
			var j = numPoints - 1;
			var inside = false;
			for (var i = 0; i < numPoints; j = i++)
				if ((points[i].y <= p.y && p.y < points[j].y || points[j].y <= p.y && p.y < points[i].y) &&
				    p.x <
				    (points[j].x - points[i].x) * (p.y - points[i].y) / (points[j].y - points[i].y) +
				    points[i].x)
					inside = !inside;
			return inside;
		}

		public bool Contains(Region other)
		{
			if (other == null || !rect2D.Overlaps(other.rect2D))
				return false;

			var numPoints = other.points.Length;
			for (var i = 0; i < numPoints; i++)
				if (!Contains(other.points[i]))
					return false;
			return true;
		}

		public bool Intersects(Region other)
		{
			if (points == null || other == null || other.points == null)
				return false;

			var otherRect = other.rect2D;

			if (otherRect.xMin > rect2D.xMax)
				return false;
			if (otherRect.xMax < rect2D.xMin)
				return false;
			if (otherRect.yMin > rect2D.yMax)
				return false;
			if (otherRect.yMax < rect2D.yMin)
				return false;

			var pointCount = points.Length;
			var otherPointCount = other.points.Length;

			for (var k = 0; k < otherPointCount; k++)
			{
				var j = pointCount - 1;
				var inside = false;
				var p = other.points[k];
				for (var i = 0; i < pointCount; j = i++)
					if ((points[i].y <= p.y && p.y < points[j].y ||
					     points[j].y <= p.y && p.y < points[i].y) &&
					    p.x <
					    (points[j].x - points[i].x) * (p.y - points[i].y) / (points[j].y - points[i].y) +
					    points[i].x)
						inside = !inside;
				if (inside)
					return true;
			}

			for (var k = 0; k < pointCount; k++)
			{
				var j = otherPointCount - 1;
				var inside = false;
				var p = points[k];
				for (var i = 0; i < otherPointCount; j = i++)
					if ((other.points[i].y <= p.y && p.y < other.points[j].y ||
					     other.points[j].y <= p.y && p.y < other.points[i].y) &&
					    p.x <
					    (other.points[j].x - other.points[i].x) *
					    (p.y - other.points[i].y) /
					    (other.points[j].y - other.points[i].y) +
					    other.points[i].x)
						inside = !inside;
				if (inside)
					return true;
			}

			return false;
		}

		// Clears all point data and reset region info
		public void Clear()
		{
			points = new Vector2[0];
			rect2D = new Rect(0, 0, 0, 0);
			rect2DArea = 0;
			neighbours.Clear();
			curvedLabelInfo.isDirty = true;
		}

		/// <summary>
		/// Updates the region rect2D. Needed if points is updated manually.
		/// </summary>
		public void UpdatePointsAndRect(List<Vector2> newPoints)
		{
			sanitized = false;
			var pointCount = newPoints.Count;
			if (points == null || points.Length != pointCount)
				points = newPoints.ToArray();
			else
				for (var k = 0; k < pointCount; k++)
					points[k] = newPoints[k];
			curvedLabelInfo.isDirty = true;
			ComputeBounds();
		}

		/// <summary>
		/// Updates the region rect2D. Needed if points is updated manually.
		/// </summary>
		/// <param name="newPoints">New points.</param>
		/// <param name="inflate">If set to <c>true</c> points will be slightly displaced to prevent polygon clipping floating point issues.</param>
		public void UpdatePointsAndRect(Vector2[] newPoints, bool inflate = false)
		{
			sanitized = false;
			points = newPoints;
			curvedLabelInfo.isDirty = true;
			ComputeBounds();
			if (inflate)
			{
				var tmp = Misc.Vector2zero;
				for (var k = 0; k < points.Length; k++)
				{
					FastVector.NormalizedDirection(ref center, ref points[k], ref tmp);
					FastVector.Add(ref points[k], ref tmp, 0.00001f);
				}
			}
		}

		/// <summary>
		/// Updates the region rect2D. Needed if points is updated manually.
		/// </summary>
		public void UpdatePointsAndRect(Region fromRegion)
		{
			sanitized = false;
			points = fromRegion.points;
			rect2D = fromRegion.rect2D;
			rect2DArea = fromRegion.rect2DArea;
			center = fromRegion.center;
			curvedLabelInfo.isDirty = true;
		}

		private void ComputeBounds()
		{
			var min = Misc.Vector2max;
			var max = Misc.Vector2min;
			for (var k = 0; k < points.Length; k++)
			{
				var x = points[k].x;
				var y = points[k].y;
				if (x < min.x)
					min.x = x;
				if (x > max.x)
					max.x = x;
				if (y < min.y)
					min.y = y;
				if (y > max.y)
					max.y = y;
			}
			rect2D = new Rect(min.x, min.y, max.x - min.x, max.y - min.y);
			rect2DArea = rect2D.width * rect2D.height;
			FastVector.Average(ref min, ref max, ref center); // center = (min + max) * 0.5f;
		}

		public Vector2 GetNearestPoint(Vector2 p)
		{
			var minDist = float.MaxValue;
			var nearest = p;
			for (var k = 0; k < points.Length; k++)
			{
				var dx = points[k].x - p.x;
				var dy = points[k].y - p.y;
				var dist = dx * dx + dy * dy;
				if (dist < minDist)
				{
					nearest = points[k];
					minDist = dist;
				}
			}
			return nearest;
		}

		public void RemoveDuplicatePoints()
		{
			if (points == null)
				return;
			var regionPointsCount = points.Length;
			// Dictionary setter is a little bit faster than HashSet
			if (dictPoints == null)
				dictPoints = new Dictionary<Vector2, bool>(regionPointsCount);
			else
				dictPoints.Clear();
			for (var k = 0; k < regionPointsCount; k++)
				dictPoints[points[k]] = true;
			var tmpPoints = new List<Vector2>(dictPoints.Keys);
			UpdatePointsAndRect(tmpPoints);
		}
	}
}