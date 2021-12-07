using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit.MapGenerator.Geom
{
	public class VoronoiFortune
	{
		public VoronoiCell[] cells;

		// this is the output
		private List<Event> eventQueue;
		private int eventQueueTop;
		private Arc root;
		private Dictionary<Point, bool> hit;
		private const double X0 = -0.5;
		private const double Y0 = -0.5;
		private const double X1 = 0.5;
		private const double Y1 = 0.5;

		public void AssignData(Point[] centers)
		{
			if (cells == null || cells.Length != centers.Length)
				cells = new VoronoiCell[centers.Length];
			for (var k = 0; k < cells.Length; k++)
				if (cells[k] == null)
					cells[k] = new VoronoiCell(centers[k]);
				else
					cells[k].Init(centers[k]);

			if (eventQueue == null)
				eventQueue = new List<Event>(cells.Length);
			else
				eventQueue.Clear();
			root = null;
			eventQueueTop = 0;

			if (hit == null)
				hit = new Dictionary<Point, bool>(cells.Length);
			else
				hit.Clear();
			for (var k = 0; k < cells.Length; k++)
			{
				var p = cells[k].center;
				// Checks that p is not near than PRECISION from other point
				var inc = p.x > 0 ? Point.PRECISION * -2.0 : Point.PRECISION * 2.0;
				while (hit.ContainsKey(p))
					p.x += inc;
				hit[p] = true;
				var siteEvent = new Event(EVENT_TYPE.SiteEvent);
				siteEvent.p = p;
				siteEvent.x = p.x;
				siteEvent.cell = cells[k];
				eventQueue.Add(siteEvent);
			}

			// Initial sort

			eventQueue.Sort((Event e1, Event e2) =>
			{
				if (e1.x < e2.x - Point.PRECISION)
					return -1;
				else if (e1.x > e2.x + Point.PRECISION)
					return 1;
				else if (e1.p.y < e2.p.y - Point.PRECISION)
					return -1;
				else if (e1.p.y > e2.p.y + Point.PRECISION)
					return 1;
				else
					return 0;
			});
		}

		private void AddEvent(Event ev)
		{
			// Binary sort insert
			int max = eventQueue.Count, min = eventQueueTop;
			for (;;)
			{
				if (max == min)
				{
					if (eventQueueTop > 0 && max == eventQueueTop)
					{
						// optimization - reuse memory slot from older events
						eventQueueTop--;
						eventQueue[eventQueueTop] = ev;
					}
					else
						eventQueue.Insert(max, ev);
					return;
				}
				var midPoint = (max + min) / 2;
				var ev2 = eventQueue[midPoint];
//				if (ev2.x<ev.x) {
//					min = midPoint + 1;
//				} else if (ev2.x==ev.x && ev2.p.y<ev.p.y) {
//					min = midPoint + 1;
//				} else {
//					max = midPoint;
//				}

				if (ev2.x < ev.x - Point.PRECISION)
					min = midPoint + 1;
				else if (ev2.x - ev.x < Point.PRECISION &&
				         ev2.x - ev.x > -Point.PRECISION &&
				         ev2.p.y < ev.p.y - Point.PRECISION)
					min = midPoint + 1;
				else
					max = midPoint;
			}
		}

		public void DoVoronoi()
		{
			eventQueueTop = 0;
			while (eventQueueTop < eventQueue.Count)
			{
				var ev = eventQueue[eventQueueTop++];
				if (ev.type == EVENT_TYPE.SiteEvent)
					HandleSiteEvent(ev);
				else
					HandleCircleEvent(ev);
			}
			FinishEdges();
		}

		private void HandleSiteEvent(Event ev)
		{
			var p = ev.p;

			if (root == null)
			{
				root = new Arc(ev.cell, p);
				return;
			}

			Arc i;
			// Find the current arc(s) at height p.y (if there are any).
			for (i = root; i != null; i = i.next)
			{
				Point z, zz;
				if (Intersect(p, i, out z))
				{
					// New parabola intersects arc i.  If necessary, duplicate i.
					if (i.next != null && !Intersect(p, i.next, out zz))
					{
						i.next.prev = new Arc(i.cell, i.p, i, i.next);
						i.next = i.next.prev;
					}
					else
						i.next = new Arc(i.cell, i.p, i);
					i.next.s1 = i.s1;

					// Add p between i and i->next.
					i.next.prev = new Arc(ev.cell, p, i, i.next);
					i.next = i.next.prev;

					i = i.next; // Now i points to the new arc.

					// Add new half-edges connected to i's endpoints.
					i.prev.s1 = i.s0 = new Segment(z);
					i.prev.cell.segments.Add(i.prev.s1);
					i.cell.segments.Add(i.prev.s1);

					i.next.s0 = i.s1 = new Segment(z);
					i.next.cell.segments.Add(i.next.s0);
					i.cell.segments.Add(i.next.s0);

					// Check for new circle events around the new arc:
					CheckCircleEvent(i, p.x);
					CheckCircleEvent(i.prev, p.x);
					CheckCircleEvent(i.next, p.x);
					return;
				}
			}

			// Special case: If p never intersects an arc, append it to the list.
			for (i = root; i.next != null; i = i.next)
				; // Find the last node.
			i.next = new Arc(ev.cell, p, i);

			// Insert segment between p and i
			var start = new Point(X0 - 1, (i.next.p.y + i.p.y) / 2);
			i.next.s0 = i.s1 = new Segment(start);
			i.next.cell.segments.Add(i.next.s0);
			i.cell.segments.Add(i.next.s0);
		}

		/// <summary>
		/// Look for a new circle event for arc i.
		/// </summary>
		private void HandleCircleEvent(Event e)
		{
			if (e.valid)
			{
				var s = new Segment(e.p);

				var a = e.a;
				if (a.prev != null)
				{
					a.prev.next = a.next;
					a.prev.s1 = s;
					a.prev.cell.segments.Add(s);
				}
				if (a.next != null)
				{
					a.next.prev = a.prev;
					a.next.s0 = s;
					a.next.cell.segments.Add(s);
				}

				if (a.s0 != null)
					a.s0.Finish(e.p);
				if (a.s1 != null)
					a.s1.Finish(e.p);

				if (a.prev != null)
					CheckCircleEvent(a.prev, e.x);
				if (a.next != null)
					CheckCircleEvent(a.next, e.x);
			}
		}

		private void CheckCircleEvent(Arc i, double x0)
		{
			// Invalidate any old event.
			if (i.e != null && (i.e.x - x0 < Point.PRECISION || i.e.x - x0 > Point.PRECISION))
				i.e.valid = false;
			i.e = null;

			if (i.prev == null || i.next == null)
				return;

			double x;
			Point o;

			if (Circle(i.prev.p, i.p, i.next.p, out x, out o) && x >= x0 - Point.PRECISION)
			{
				// Create new event.
				i.e = new Event(EVENT_TYPE.CircleEvent, x, o, i);
				AddEvent(i.e);
			}
		}

		// Find the rightmost point on the circle through a,b,c.
		private bool Circle(Point a, Point b, Point c, out double x, out Point o)
		{
			// Check that bc is a "right turn" from ab.
			x = 0;
			o = Point.zero;
			var cv = (b.x - a.x) * (c.y - a.y) - (c.x - a.x) * (b.y - a.y);
			if (cv >= 0)
				return false;

			// Algorithm from O'Rourke 2ed p. 189.
			var A = b.x - a.x;
			var B = b.y - a.y;
			var C = c.x - a.x;
			var D = c.y - a.y;
			var E = A * (a.x + b.x) + B * (a.y + b.y);
			var F = C * (a.x + c.x) + D * (a.y + c.y);
			var G = 2 *
			        (A * (c.y - b.y) - B * (c.x - b.x)); // changed to double to prevent precision problems

			if (G < Point.PRECISION && G > -Point.PRECISION)
				return false; // Points are co-linear.

			// Point o is the center of the circle.
			o.x = (D * E - B * F) / G;
			o.y = (A * F - C * E) / G;

			// o.x plus radius equals max x coordinate.
			x = o.x + Math.Sqrt((a.x - o.x) * (a.x - o.x) + (a.y - o.y) * (a.y - o.y));
			return true;
		}

		// Will a new parabola at point p intersect with arc i?
		private bool Intersect(Point p, Arc i, out Point res)
		{
			res = Point.zero;
			if (i.p.x - p.x < Point.PRECISION && i.p.x - p.x > -Point.PRECISION)
				return false;

			double a = 0, b = 0;
			if (i.prev != null) // Get the intersection of i->prev, i.
				a = Intersection(i.prev.p, i.p, p.x).y;
			if (i.next != null) // Get the intersection of i->next, i.
				b = Intersection(i.p, i.next.p, p.x).y;

			if ((i.prev == null || a <= p.y) && (i.next == null || p.y < b))
			{
				res.y = p.y;
				// Plug it back into the parabola equation.
				res.x = (i.p.x * i.p.x + (i.p.y - res.y) * (i.p.y - res.y) - p.x * p.x) /
				        (2 * i.p.x - 2 * p.x);

				return true;
			}
			return false;
		}

		// Where do two parabolas intersect?
		private Point Intersection(Point p0, Point p1, double l)
		{
			Point res, p = p0;

			if (p0.x == p1.x)
				res.y = (p0.y + p1.y) / 2;
			else if (p1.x == l)
				res.y = p1.y;
			else if (p0.x == l)
			{
				res.y = p0.y;
				p = p1;
			}
			else
			{
				// Use the quadratic formula.
				var z0 = 2 * (p0.x - l);
				var z1 = 2 * (p1.x - l);

				var a = 1 / z0 - 1 / z1;
				var b = -2 * (p0.y / z0 - p1.y / z1);
				var c = (p0.y * p0.y + p0.x * p0.x - l * l) / z0 -
				        (p1.y * p1.y + p1.x * p1.x - l * l) / z1;

				res.y = (-b - Math.Sqrt(b * b - 4 * a * c)) / (2 * a);
			}
			// Plug back into one of the parabola equations.
			res.x = (p.x * p.x + (p.y - res.y) * (p.y - res.y) - l * l) / (2 * p.x - 2 * l);
			return res;
		}

		private void FinishEdges()
		{
			// Advance the sweep line so no parabolas can cross the bounding box.
			var l = X1 + (X1 - X0) + (Y1 - Y0);

			// Extend each remaining segment to the new parabola intersections.
			for (var i = root; i.next != null; i = i.next)
				if (i.s1 != null)
					i.s1.Finish(Intersection(i.p, i.next.p, l * 2));

			// Merge collinear segments
			for (var c = 0; c < cells.Length; c++)
			{
				var cell = cells[c];
				var segmentCount = cell.segments.Count;
				var s0 = cell.segments[0];
				for (var k = 1; k < segmentCount; k++)
				{
					var s1 = cell.segments[k];
					if (s0.start == s1.start)
					{
						// Merge s1 into s0
						s0.start = s1.end;
						s1.deleted = true;
					}
					s0 = s1;
				}
			}

			// Crop output
			for (var c = 0; c < cells.Length; c++)
			{
				var cell = cells[c];
				var cropped = false;
				// Crop segments if needed
				var segmentCount = cell.segments.Count;
				for (var k = 0; k < segmentCount; k++)
				{
					var s = cell.segments[k];
					// is the segment completely outside?
					if (!s.done ||
					    s.start.x < X0 && s.end.x < X0 ||
					    s.start.y < Y0 && s.end.y < Y0 ||
					    s.start.x > X1 && s.end.x > X1 ||
					    s.start.y > Y1 && s.end.y > Y1 ||
					    Point.EqualsBoth(s.start, s.end))
					{
						s.deleted = true;
						continue;
					}

					// is any endpoint outside of the canvas?
					var p1inside = PointInsideRect(s.start);
					if (!p1inside)
					{
						s.start = CropPoint(s.start, s, cell);
						cropped = true;
					}
					var p2inside = PointInsideRect(s.end);
					if (!p2inside)
					{
						s.end = CropPoint(s.end, s, cell);
						cropped = true;
					}
				}

				if (cropped)
				{
					// join borders with 2 points
					if (cell.top.Count > 1)
						cell.segments.Add(new Segment(cell.top[0], cell.top[1], true));
					if (cell.bottom.Count > 1)
						cell.segments.Add(new Segment(cell.bottom[0], cell.bottom[1], true));
					if (cell.left.Count > 1)
						cell.segments.Add(new Segment(cell.left[0], cell.left[1], true));
					if (cell.right.Count > 1)
						cell.segments.Add(new Segment(cell.right[0], cell.right[1], true));
				}
			}

			// 2nd step - snap to nearest corners
			var corners = new Point[4];
			corners[0] = new Point(X0, Y0);
			corners[1] = new Point(X0, Y1);
			corners[2] = new Point(X1, Y0);
			corners[3] = new Point(X1, Y1);
			Point np;
			for (var cornerIndex = 0; cornerIndex < corners.Length; cornerIndex++)
			{
				var corner = corners[cornerIndex];
				// Get the nearest point of the segments
				var nearestCell = GetNearestCellFrom(corner);
				// this territory is the nearest to the corner so now we can snap the nearest segment safely
				if (GetNearestSegmentPointToCorner(corner, nearestCell.segments, true, out np))
					nearestCell.segments.Add(new Segment(np, corner, true));
				if (GetNearestSegmentPointToCorner(corner, nearestCell.segments, false, out np))
					nearestCell.segments.Add(new Segment(np, corner, true));
			}
		}

		private bool GetNearestSegmentPointToCorner(Point corner, List<Segment> segments,
			bool leftOrRightSides, out Point nearest)
		{
			double dist, minDist = double.MaxValue;
			Point p;
			nearest = Point.zero;
			for (var k = 0; k < segments.Count; k++)
			{
				var s = segments[k];
				if (s.deleted)
					continue;
				p = s.start;
				if (Point.EqualsBoth(p, corner))
					continue;
				if (Math.Abs(corner.x - p.x) < Point.PRECISION && leftOrRightSides ||
				    Math.Abs(corner.y - p.y) < Point.PRECISION && !leftOrRightSides)
				{
					dist = (p - corner).sqrMagnitude;
					if (dist < minDist)
					{
						minDist = dist;
						nearest = p;
					}
				}
				p = segments[k].end;
				if (Point.EqualsBoth(p, corner))
					continue;
				if (Math.Abs(corner.x - p.x) < Point.PRECISION && leftOrRightSides ||
				    Math.Abs(corner.y - p.y) < Point.PRECISION && !leftOrRightSides)
				{
					dist = (p - corner).sqrMagnitude;
					if (dist < minDist)
					{
						minDist = dist;
						nearest = p;
					}
				}
			}
			return minDist < double.MaxValue;
		}

		private VoronoiCell GetNearestCellFrom(Point point)
		{
			var minDist = double.MaxValue;
			var nearest = -1;
			Point center;
			for (var k = 0; k < cells.Length; k++)
			{
				center.x = cells[k].center.x;
				center.y = cells[k].center.y;
				var dist = (center.x - point.x) * (center.x - point.x) +
				           (center.y - point.y) * (center.y - point.y);
				if (dist < minDist)
				{
					minDist = dist;
					nearest = k;
				}
			}
			return cells[nearest];
		}

		private bool PointInsideRect(Point p) => p.x > X0 && p.x < X1 && p.y > Y0 && p.y < Y1;

		private Point CropPoint(Point point, Segment s, VoronoiCell cell)
		{
			// Get line parameters
			Point start = s.start, end = s.end;

			var dy = (end.y - start.y) / (end.x - start.x);
			if (point.x < X0)
			{
				point.y += dy * (X0 - point.x);
				point.x = X0;
			}
			else if (point.x > X1)
			{
				point.y += dy * (X1 - point.x);
				point.x = X1;
			}
			if (point.y < Y0)
			{
				point.x += (Y0 - point.y) * (1.0f / dy);
				point.y = Y0;
			}
			else if (point.y > Y1)
			{
				point.x += (Y1 - point.y) * (1.0f / dy);
				point.y = Y1;
			}
			if (DoublesAreEqual(point.x, X0))
				cell.left.Add(point);
			if (DoublesAreEqual(point.x, X1))
				cell.right.Add(point);
			if (DoublesAreEqual(point.y, Y0))
				cell.bottom.Add(point);
			if (DoublesAreEqual(point.y, Y1))
				cell.top.Add(point);
			return point;
		}

		private bool DoublesAreEqual(double d1, double d2) =>
			d1 - d2 > -Point.PRECISION && d1 - d2 < Point.PRECISION;
	}
}