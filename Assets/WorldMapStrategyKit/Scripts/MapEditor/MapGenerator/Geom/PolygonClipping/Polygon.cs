using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit.MapGenerator.Geom
{
	public class Polygon
	{
		public List<Contour> contours;
		public Rectangle bounds;

		public Polygon()
		{
			contours = new List<Contour>();
			bounds = null;
		}

		public Polygon Clone()
		{
			var g = new Polygon();
			for (var k = 0; k < contours.Count; k++)
				g.AddContour(contours[k].Clone());
			return g;
		}

		public Rectangle boundingBox
		{
			get
			{
				if (bounds != null)
					return bounds;

				Rectangle bb = null;
				foreach (var c in contours)
				{
					var cBB = c.boundingBox;
					if (bb == null)
						bb = cBB;
					else
						bb = bb.Union(cBB);
				}
				bounds = bb;
				return bounds;
			}
		}

		public void AddContour(Contour c)
		{
			contours.Add(c);
		}
	}
}