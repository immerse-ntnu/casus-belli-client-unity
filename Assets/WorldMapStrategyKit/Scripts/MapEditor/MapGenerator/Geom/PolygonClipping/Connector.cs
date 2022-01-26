using System.Collections.Generic;

namespace WorldMapStrategyKit.MapGenerator.Geom
{
	internal class Connector
	{
		private List<PointChain> openPolygons;
		private List<PointChain> closedPolygons;

		public Connector()
		{
			openPolygons = new List<PointChain>(16);
			closedPolygons = new List<PointChain>(16);
		}

		public void Clear()
		{
			openPolygons.Clear();
			closedPolygons.Clear();
		}

		public void AddRange(List<Segment> segments)
		{
			var segmentCount = segments.Count;
			for (var k = 0; k < segmentCount; k++)
				Add(segments[k]);
		}

		public void Add(Segment s)
		{
			// j iterates through the openPolygon chains.
			for (var j = 0; j < openPolygons.Count; j++)
			{
				var chain = openPolygons[j];
				if (!chain.LinkSegment(s))
					continue;

				if (chain.closed)
				{
					if (chain.pointList.Count == 2)
					{
						// We tried linking the same segment (but flipped end and start) to 
						// a chain. (i.e. chain was <p0, p1>, we tried linking Segment(p1, p0)
						// so the chain was closed illegally.
						chain.closed = false;
						return;
					}

					closedPolygons.Add(chain);
					openPolygons.RemoveAt(j);
					return;
				}

				var k = openPolygons.Count;
				for (var i = j + 1;
						i < k;
						i++) // Try to connect this open link to the rest of the chains. 
					// We won't be able to connect this to any of the chains preceding this one
					// because we know that linkSegment failed on those.
					if (chain.LinkPointChain(openPolygons[i]))
					{
						openPolygons.RemoveAt(i);
						return;
					}
				return;
			}

			var newChain = new PointChain(s);
			openPolygons.Add(newChain);
		}

		public Polygon ToPolygon()
		{
			// Check for empty result
			if ((closedPolygons.Count == 0 ||
			     closedPolygons.Count == 1 && closedPolygons[0].pointList.Count == 0) &&
			    (openPolygons.Count == 0 ||
			     openPolygons.Count == 1 && openPolygons[0].pointList.Count == 0))
				return null;

			var polygon = new Polygon();
			foreach (var pointChain in closedPolygons)
			{
				var c = new Contour();
				c.AddRange(pointChain.pointList);
				polygon.AddContour(c);
			}
			FixOrientation(polygon);
			return polygon;
		}

		/// <summary>
		/// Since polygons from countries and cells are not perfectly aligned in all cases, this method will take the largest contour and assume this is the resulting polygon
		/// (even if it's not closed...)
		/// </summary>
		public Polygon ToPolygonFromLargestLineStrip()
		{
			// Check for empty result
			if ((closedPolygons.Count == 0 ||
			     closedPolygons.Count == 1 && closedPolygons[0].pointList.Count == 0) &&
			    (openPolygons.Count == 0 ||
			     openPolygons.Count == 1 && openPolygons[0].pointList.Count == 0))
				return null;

			// Get the largest contour (open or closed)
			var maxPoints = -1;
			PointChain largestPointChain = null;
			foreach (var pointChain in closedPolygons)
				if (pointChain.pointList.Count > maxPoints)
				{
					maxPoints = pointChain.pointList.Count;
					largestPointChain = pointChain;
				}
			foreach (var pointChain in openPolygons)
				if (pointChain.pointList.Count > maxPoints)
				{
					maxPoints = pointChain.pointList.Count;
					largestPointChain = pointChain;
				}

			// ... and create a new polygon of that
			if (maxPoints < 0)
				return null;
			var polygon = new Polygon();
			var contour = new Contour(largestPointChain.pointList);
			polygon.AddContour(contour);
			FixOrientation(polygon);
			return polygon;
		}

		// isLeft(): test if a point is Left|On|Right of an infinite 2D line.
		//    Input:  three points P0, P1, and P2
		//    Return: >0 for P2 left of the line through P0 to P1
		//          =0 for P2 on the line
		//          <0 for P2 right of the line
		//    From http://geomalgorithms.com/a01-_area.html#isLeft()
		private double isLeft(Point p0, Point p1, Point p2) =>
			(p1.x - p0.x) * (p2.y - p0.y) - (p2.x - p0.x) * (p1.y - p0.y);

		// orientation2D_Polygon(): test the orientation of a simple 2D polygon
		//  Input:  Point* V = an array of n+1 vertex points with V[n]=V[0]
		//  Return: >0 for counterclockwise
		//          =0 for none (degenerate)
		//          <0 for clockwise
		//  Note: this algorithm is faster than computing the signed area.
		//  From http://geomalgorithms.com/a01-_area.html#orientation2D_Polygon()
		private double[] Orientation(Polygon V)
		{
			// first find rightmost lowest vertex of the polygon
			var ou = new double[V.contours.Count];
			for (var j = 0; j < V.contours.Count; j++)
			{
				var r = V.contours[j];
				var rmin = 0;
				var xmin = r.points[0].x;
				var ymin = r.points[0].y;
				for (var i = 0; i < r.points.Count; i++)
				{
					var p = r.points[i];
					if (p.y > ymin)
						continue;
					if (p.y == ymin) // just as low
						if (p.x < xmin) // and to left
							continue;
					rmin = i; // a new rightmost lowest vertex
					xmin = p.x;
					ymin = p.y;
				}

				// test orientation at the rmin vertex
				// ccw <=> the edge leaving V[rmin] is left of the entering edge
				if (rmin == 0 || rmin == r.points.Count - 1)
					ou[j] = isLeft(r.points[r.points.Count - 2], r.points[0], r.points[1]);
				else
					ou[j] = isLeft(r.points[rmin - 1], r.points[rmin], r.points[rmin + 1]);
			}
			return ou;
		}

		private bool PolyInPoly(Contour outer, Contour inner)
		{
			for (var p = 0; p < inner.points.Count; p++)
				if (!outer.ContainsPoint(inner.points[p]))
					return false;
			return true;
		}

		private List<Point> ReversePolygon(List<Point> s)
		{
			for (int i = 0, j = s.Count - 1; i < j; i++, j--)
			{
				var aux = s[i];
				s[i] = s[j];
				s[j] = aux;
			}
			return s;
		}

		// Change the winding direction of the outer and inner
		// rings so the outer ring is counter-clockwise and
		// nesting rings alternate directions.
		private void FixOrientation(Polygon g)
		{
			var p = g; //.(geom.Polygon)
			var o = Orientation(p);
			for (var i = 0; i < p.contours.Count; i++)
			{
				var inner = p.contours[i];
				var numInside = 0;
				for (var j = 0; j < p.contours.Count; j++)
				{
					var outer = p.contours[j];
					if (i != j)
						if (PolyInPoly(outer, inner))
							numInside++;
				}
				if (numInside % 2 == 1 && o[i] > 0)
					ReversePolygon(inner.points);
				else if (numInside % 2 == 0 && o[i] < 0)
					ReversePolygon(inner.points);
			}
		}
	}
}