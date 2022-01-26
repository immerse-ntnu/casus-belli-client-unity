using System;
using System.Collections.Generic;
using UnityEngine;
using WorldMapStrategyKit.MapGenerator.Geom;

namespace WorldMapStrategyKit
{
	public class MapRegion
	{
		public Polygon polygon;

		public Vector2[] points { get; set; }

		public List<Segment> segments;
		public List<MapRegion> neighbours;
		public MapEntity entity;
		public Rect rect2D;
		public float rect2DArea;

		public Material customMaterial { get; set; }

		public MapRegion(MapEntity entity)
		{
			this.entity = entity;
			neighbours = new List<MapRegion>(12);
			segments = new List<Segment>(50);
		}

		public MapRegion Clone()
		{
			var c = new MapRegion(entity);
			c.customMaterial = customMaterial;
			c.points = new Vector2[points.Length];
			Array.Copy(points, c.points, points.Length);
			c.polygon = polygon.Clone();
			c.segments = new List<Segment>(segments);
			return c;
		}

		public bool Intersects(MapRegion otherRegion) => otherRegion.rect2D.Overlaps(otherRegion.rect2D);

		public bool Contains(float x, float y)
		{
			if (x < rect2D.xMin || x > rect2D.xMax || y < rect2D.yMin || y > rect2D.yMax)
				return false;

			if (points == null)
				return false;

			var numPoints = points.Length;
			var j = numPoints - 1;
			var inside = false;
			for (var i = 0; i < numPoints; j = i++)
				if ((points[i].y <= y && y < points[j].y || points[j].y <= y && y < points[i].y) &&
				    x <
				    (points[j].x - points[i].x) * (y - points[i].y) / (points[j].y - points[i].y) +
				    points[i].x)
					inside = !inside;
			return inside;
		}

		public bool Contains(MapRegion otherRegion)
		{
			if (!Intersects(otherRegion))
				return false;

			if (!Contains(otherRegion.rect2D.xMin, otherRegion.rect2D.yMin))
				return false;
			if (!Contains(otherRegion.rect2D.xMin, otherRegion.rect2D.yMax))
				return false;
			if (!Contains(otherRegion.rect2D.xMax, otherRegion.rect2D.yMin))
				return false;
			if (!Contains(otherRegion.rect2D.xMax, otherRegion.rect2D.yMax))
				return false;

			var opc = otherRegion.points.Length;
			for (var k = 0; k < opc; k++)
				if (!Contains(otherRegion.points[k].x, otherRegion.points[k].y))
					return false;
			return true;
		}
	}
}