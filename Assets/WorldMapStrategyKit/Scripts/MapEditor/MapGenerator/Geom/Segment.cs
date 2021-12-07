using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit.MapGenerator.Geom
{
	public class Segment
	{
		public Point start, end;
		public bool done, deleted;

		public bool border;

		// this border is result of a border crop
		public List<Segment> subdivisions;
		public int territoryIndex;
		public int cellIndex;

		public Vector2 startToVector3 => new Vector3((float)start.x, (float)start.y, 0);

		public Vector2 endToVector3 => new Vector3((float)end.x, (float)end.y, 0);

		public Segment(Point start, Point end) : this(start, end, false) { }

		public Segment(Point start, Point end, bool border)
		{
			this.start = start;
			this.end = end;
			this.border = border;
			done = true;
		}

		public Segment(Point p) => start = p;

		public void Finish(Point p)
		{
			if (done)
				return;
			end = p;
			done = true;
		}

		public double sqrMagnitude
		{
			get
			{
				var dx = end.x - start.x;
				var dy = end.y - start.y;
				return dx * dx + dy * dy;
			}
		}

		public double magnitude
		{
			get
			{
				var dx = end.x - start.x;
				var dy = end.y - start.y;
				return Math.Sqrt(dx * dx + dy * dy);
			}
		}

//		public List<Segment> Subdivide (int divisions, double waveAmount) {
//			if (subdivisions != null)
//				return subdivisions;
//			
//			// Divide and add random displacement
//			if (subdivisions == null) {
//				subdivisions = new List<Segment> (divisions);
//			} else {
//				subdivisions.Clear ();
//			}
//			Point normal = Point.zero;
//			double l = 0;
//			waveAmount *= 5f;
//			if (!border && waveAmount > 0 && divisions > 1) {
//				// safety check - length must be > 0.01f;
//				l = waveAmount * Math.Sqrt (sqrMagnitude);
//				normal = new Point (-(end.y - start.y), end.x - start.x);
//				normal = normal.normalized * l;
//				if (UnityEngine.Random.value > 0.5f)
//					normal *= -1;
//			}
//			Point d0 = start;
//			for (int d = 1; d < divisions; d++) {
//				Point d1 = Point.Lerp (start, end, (double)d / divisions);
//				if (!border && waveAmount > 0) {
//					double s = 1 - Math.Abs (d - (double)divisions / 2) / ((double)divisions / 2);
//					d1 += normal * (UnityEngine.Random.value - 0.5f) * s; // Math.Sin (d * Math.PI / divisions) * s;
//				}
//				subdivisions.Add (new Segment (d0, d1, border));
//				d0 = d1;
//			}
//			subdivisions.Add (new Segment (d0, end, border));
//			return subdivisions;
//		}

		public List<Segment> Subdivide(Point center, float edgeMaxLength, float edgeNoise)
		{
			if (subdivisions != null)
				return subdivisions;

			// Divide and add random displacement
			if (subdivisions == null)
				subdivisions = new List<Segment>();
			else
				subdivisions.Clear();

			var dx = start.x - end.x;
			var dy = start.y - end.y;
			var length = Math.Sqrt(dx * dx + dy * dy);
			var divisions = (int)Math.Ceiling(length / edgeMaxLength);
			var d0 = start;

			float t = 0;
			for (var d = 1; d < divisions; d++)
			{
				var d1 = Point.Lerp(start, end, (double)d / divisions);
				if (!border)
				{
					t += (UnityEngine.Random.value - 0.5f) * edgeNoise;
					if (t < 0)
						t = 0;
					else if (t > 0.9f)
						t = 0.9f;
					var r = Math.Sin(d * Math.PI / divisions);
					d1 = Point.Lerp(d1, center, r * t);
				}
				subdivisions.Add(new Segment(d0, d1, border));
				d0 = d1;
			}
			subdivisions.Add(new Segment(d0, end, border));
			return subdivisions;
		}

		public override string ToString() =>
			string.Format("start:" + start.ToString() + ", end:" + end.ToString());

		public void CropBottom()
		{
			start.CropBottom();
			end.CropBottom();

			if (Point.EqualsBoth(start, end))
				deleted = true;
		}

		public void CropRight()
		{
			start.CropRight();
			end.CropRight();

			if (Point.EqualsBoth(start, end))
				deleted = true;
		}
	}
}