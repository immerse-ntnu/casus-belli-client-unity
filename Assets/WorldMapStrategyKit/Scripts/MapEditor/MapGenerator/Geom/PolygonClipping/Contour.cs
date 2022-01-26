using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit.MapGenerator.Geom
{
	public class Contour
	{
		public List<Point> points;
		public Rectangle bounds;

		public Contour() => points = new List<Point>(6);

		public Contour(List<Point> points) => this.points = points;

		public Contour Clone()
		{
			var u = new Contour();
			u.points = new List<Point>(points);
			u.bounds = bounds;
			return u;
		}

		public void Clear()
		{
			points.Clear();
			bounds = null;
		}

		public void Add(Point p)
		{
			points.Add(p);
			bounds = null;
		}

		public void AddRange(List<Point> points)
		{
			this.points.AddRange(points);
			bounds = null;
		}

		public void AddRange(Vector2[] points)
		{
			for (var k = 0; k < points.Length; k++)
				this.points.Add(new Point(points[k].x, points[k].y));
			bounds = null;
		}

		public Vector2[] GetVector2Points()
		{
			var count = points.Count;
			var np = new Vector2[count];
			for (var k = 0; k < count; k++)
			{
				var x = (float)Math.Round(points[k].x, 7);
				var y = (float)Math.Round(points[k].y, 7);
				np[k] = new Vector2(x, y);
			}
			return np;
		}

		public Rectangle boundingBox
		{
			get
			{
				if (bounds != null)
					return bounds;

				double minX = double.MaxValue, minY = double.MaxValue;
				double maxX = double.MinValue, maxY = double.MinValue;
				var count = points.Count;
				for (var k = 0; k < count; k++)
				{
					var p = points[k];
					if (p.x > maxX)
						maxX = p.x;
					if (p.x < minX)
						minX = p.x;
					if (p.y > maxY)
						maxY = p.y;
					if (p.y < minY)
						minY = p.y;
				}
				bounds = new Rectangle(minX, minY, maxX - minX, maxY - minY);
				return bounds;
			}
		}

		public Segment GetSegment(int index)
		{
			var pointMax = points.Count - 1;
			if (index == pointMax)
				return new Segment(points[pointMax], points[0]);
			return new Segment(points[index], points[index + 1]);
		}

		/**
	 * Checks if a point is inside a contour using the point in polygon raycast method.
	 * This works for all polygons, whether they are clockwise or counter clockwise,
	 * convex or concave.
	 * @see 	http://en.wikipedia.org/wiki/Point_in_polygon#Ray_casting_algorithm
	 * @param	p
	 * @param	contour
	 * @return	True if p is inside the polygon defined by contour
	 */
		public bool ContainsPoint(Point p)
		{
			// Cast ray from p.x towards the right
			var intersections = 0;
			for (var i = 0; i < points.Count; i++)
			{
				var curr = points[i];
				var next = i == points.Count - 1 ? points[0] : points[i + 1];

				if ((p.y >= next.y || p.y <= curr.y) && (p.y >= curr.y || p.y <= next.y))
					continue;

				// Edge is from curr to next.
				if (p.x < Math.Max(curr.x, next.x) && next.y != curr.y)
				{
					// Find where the line intersects...
					var xInt = (p.y - curr.y) * (next.x - curr.x) / (next.y - curr.y) + curr.x;
					if (curr.x == next.x || p.x <= xInt)
						intersections++;
				}
			}

			if (intersections % 2 == 0)
				return false;
			return true;
		}
	}
}