using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit.MapGenerator.Geom
{
	public class VoronoiCell
	{
		public List<Segment> segments;
		public Point center;
		public List<Point> top, left, bottom, right; // for cropping
		private static Connector connector;

		public VoronoiCell(Point center)
		{
			segments = new List<Segment>(16);
			this.center = center;
			left = new List<Point>();
			top = new List<Point>();
			bottom = new List<Point>();
			right = new List<Point>();
		}

		public void Init(Point center)
		{
			segments.Clear();
			this.center = center;
			left.Clear();
			top.Clear();
			bottom.Clear();
			right.Clear();
		}

//		public Polygon GetPolygon (int edgeSubdivisions, float curvature) {
//			if (connector == null) {
//				connector = new Connector ();
//			} else {
//				connector.Clear ();
//			}
//			int count = segments.Count;
//			for (int k=0; k<count; k++) {
//				Segment s = segments [k];
//				if (!s.deleted) {
//					if (edgeSubdivisions>1) {
//						connector.AddRange (s.Subdivide(edgeSubdivisions, curvature));
//					} else {
//						connector.Add (s);
//					}
//				}
//			}
//			return connector.ToPolygonFromLargestLineStrip ();
//		}

		public Polygon GetPolygon()
		{
			if (connector == null)
				connector = new Connector();
			else
				connector.Clear();
			var count = segments.Count;
			for (var k = 0; k < count; k++)
			{
				var s = segments[k];
				if (!s.deleted)
					connector.Add(s);
			}
			return connector.ToPolygonFromLargestLineStrip();
		}

		public Polygon GetPolygon(Point center, float edgeMaxLength, float edgeNoise)
		{
			if (connector == null)
				connector = new Connector();
			else
				connector.Clear();
			var count = segments.Count;
			for (var k = 0; k < count; k++)
			{
				var s = segments[k];
				if (!s.deleted)
					connector.AddRange(s.Subdivide(center, edgeMaxLength, edgeNoise));
			}
			return connector.ToPolygonFromLargestLineStrip();
		}

		public Point centroid
		{
			get
			{
				var point = Point.zero;
				var count = 0;
				var segmentsCount = segments.Count;
				for (var k = 0; k < segmentsCount; k++)
				{
					var s = segments[k];
					if (!s.deleted)
					{
						point += segments[k].start;
						point += segments[k].end;
						count += 2;
					}
				}
				if (count > 0)
					point /= count;
				return point;
			}
		}
	}
}