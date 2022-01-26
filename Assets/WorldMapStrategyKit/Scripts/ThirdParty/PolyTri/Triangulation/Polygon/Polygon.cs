/* Poly2Tri
 * Copyright (c) 2009-2010, Poly2Tri Contributors
 * http://code.google.com/p/poly2tri/
 *
 * All rights reserved.
 *
 * Redistribution and use in source and binary forms, with or without modification,
 * are permitted provided that the following conditions are met:
 *
 * * Redistributions of source code must retain the above copyright notice,
 *   this list of conditions and the following disclaimer.
 * * Redistributions in binary form must reproduce the above copyright notice,
 *   this list of conditions and the following disclaimer in the documentation
 *   and/or other materials provided with the distribution.
 * * Neither the name of Poly2Tri nor the names of its contributors may be
 *   used to endorse or promote products derived from this software without specific
 *   prior written permission.
 *
 * THIS SOFTWARE IS PROVIDED BY THE COPYRIGHT HOLDERS AND CONTRIBUTORS
 * "AS IS" AND ANY EXPRESS OR IMPLIED WARRANTIES, INCLUDING, BUT NOT
 * LIMITED TO, THE IMPLIED WARRANTIES OF MERCHANTABILITY AND FITNESS FOR
 * A PARTICULAR PURPOSE ARE DISCLAIMED. IN NO EVENT SHALL THE COPYRIGHT OWNER OR
 * CONTRIBUTORS BE LIABLE FOR ANY DIRECT, INDIRECT, INCIDENTAL, SPECIAL,
 * EXEMPLARY, OR CONSEQUENTIAL DAMAGES (INCLUDING, BUT NOT LIMITED TO,
 * PROCUREMENT OF SUBSTITUTE GOODS OR SERVICES; LOSS OF USE, DATA, OR
 * PROFITS; OR BUSINESS INTERRUPTION) HOWEVER CAUSED AND ON ANY THEORY OF
 * LIABILITY, WHETHER IN CONTRACT, STRICT LIABILITY, OR TORT (INCLUDING
 * NEGLIGENCE OR OTHERWISE) ARISING IN ANY WAY OUT OF THE USE OF THIS
 * SOFTWARE, EVEN IF ADVISED OF THE POSSIBILITY OF SUCH DAMAGE.
 */

// Changes from the Java version
//   Polygon constructors sprused up, checks for 3+ polys
//   Naming of everything
//   getTriangulationMode() -> TriangulationMode { get; }
//   Exceptions replaced
// Future possibilities
//   We have a lot of Add/Clear methods -- we may prefer to just expose the container
//   Some self-explanatory methods may deserve commenting anyways

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace WorldMapStrategyKit.Poly2Tri
{
	public class Polygon : Point2DList, ITriangulatable,
		IList<TriangulationPoint>
	{
		// ITriangulatable Implementation
		protected Dictionary<uint, TriangulationPoint> mPointMap = new();

		public IList<TriangulationPoint> Points => this;

		protected List<DelaunayTriangle> mTriangles;

		public IList<DelaunayTriangle> Triangles => mTriangles;

		public TriangulationMode TriangulationMode => TriangulationMode.Polygon;

		public string FileName { get; set; }

		public bool DisplayFlipX { get; set; }

		public bool DisplayFlipY { get; set; }

		public float DisplayRotate { get; set; }

		private double mPrecision = TriangulationPoint.kVertexCodeDefaultPrecision;

		public double Precision { get => mPrecision; set => mPrecision = value; }

		public double MinX => mBoundingBox.MinX;

		public double MaxX => mBoundingBox.MaxX;

		public double MinY => mBoundingBox.MinY;

		public double MaxY => mBoundingBox.MaxY;

		public Rect2D Bounds => mBoundingBox;

		// Point2DList overrides
		public new TriangulationPoint this[int index]
		{
			get => mPoints[index] as TriangulationPoint;
			set => mPoints[index] = value;
		}

		// Polygon Implementation
		protected List<Polygon> mHoles;

		public IList<Polygon> Holes => mHoles;

		protected List<TriangulationPoint> mSteinerPoints;
		protected PolygonPoint _last;

		/// <summary>
		/// Create a polygon from a list of at least 3 points with no duplicates.
		/// </summary>
		/// <param name="points">A list of unique points</param>
		public Polygon(IList<PolygonPoint> points)
		{
			if (points.Count < 3)
				throw new ArgumentException("List has fewer than 3 points", nameof(points));

			AddRange(points, WindingOrderType.Unknown);
		}

		/// <summary>
		/// Create a polygon from a list of at least 3 points with no duplicates.
		/// </summary>
		/// <param name="points">A list of unique points</param>
		public Polygon(Vector2[] points)
		{
			if (points.Length < 3)
				throw new ArgumentException("List has fewer than 3 points", nameof(points));
			//			int pointCount = points.Count;
//			List<PolygonPoint> pp = new List<PolygonPoint> (pointCount);
//			for (int k = 0; k < pointCount; k++) {
//				Vector2 point = points [k];
//				pp.Add (new PolygonPoint (point.x, point.y));
//			}
//			AddRange (pp, WindingOrderType.Unknown);
			AddRange(points, WindingOrderType.Unknown);
		}

		/// <summary>
		/// Create a polygon from a list of at least 3 points with no duplicates.
		/// </summary>
		/// <param name="points">A list of unique points.</param>
		public Polygon(IEnumerable<PolygonPoint> points)
			: this(points as IList<PolygonPoint> ?? points.ToArray()) { }

		/// <summary>
		/// Create a polygon from a list of at least 3 points with no duplicates.
		/// </summary>
		/// <param name="points">A list of unique points.</param>
		public Polygon(params PolygonPoint[] points)
			: this((IList<PolygonPoint>)points) { }

		IEnumerator<TriangulationPoint> IEnumerable<TriangulationPoint>.GetEnumerator() =>
			new TriangulationPointEnumerator(mPoints);

		public int IndexOf(TriangulationPoint p) => mPoints.IndexOf(p);

		public override void Add(Point2D p)
		{
			Add(p, -1, true);
		}

		public void Add(TriangulationPoint p)
		{
			Add(p, -1, true);
		}

		public void Add(PolygonPoint p)
		{
			Add(p, -1, true);
		}

		protected override void Add(Point2D p, int idx, bool bCalcWindingOrderAndEpsilon)
		{
			if (p is not TriangulationPoint pt) // we only store TriangulationPoints and PolygonPoints in this class
				return;

			// do not insert duplicate points
			if (mPointMap.ContainsKey(pt.VertexCode))
				return;
			mPointMap.Add(pt.VertexCode, pt);

			base.Add(p, idx, bCalcWindingOrderAndEpsilon);

			if (p is not PolygonPoint pp)
				return;
			pp.Previous = _last;
			if (_last != null)
			{
				pp.Next = _last.Next;
				_last.Next = pp;
			}
			_last = pp;
		}

		public void AddRange(Vector2[] points, WindingOrderType windingOrder)
		{
			if (points == null)
				return;
			var numPoints = points.Length;
			if (numPoints < 1)
				return;

			if (mWindingOrder == WindingOrderType.Unknown && Count == 0)
				mWindingOrder = windingOrder;
			var bReverseReadOrder = WindingOrder != WindingOrderType.Unknown &&
			                        windingOrder != WindingOrderType.Unknown &&
			                        WindingOrder != windingOrder;
			for (var i = 0; i < numPoints; ++i)
			{
				var idx = i;
				if (bReverseReadOrder)
					idx = numPoints - i - 1;
				var point = new PolygonPoint(points[idx].x, points[idx].y);
				Add(point, -1, false);
			}
			if (mWindingOrder == WindingOrderType.Unknown)
				mWindingOrder = CalculateWindingOrder();
			mEpsilon = CalculateEpsilon();
		}

		public void AddRange(IList<PolygonPoint> points, WindingOrderType windingOrder)
		{
			if (points == null || points.Count < 1)
				return;

			if (mWindingOrder == WindingOrderType.Unknown && Count == 0)
				mWindingOrder = windingOrder;
			var numPoints = points.Count;
			var bReverseReadOrder = WindingOrder != WindingOrderType.Unknown &&
			                        windingOrder != WindingOrderType.Unknown &&
			                        WindingOrder != windingOrder;
			for (var i = 0; i < numPoints; ++i)
			{
				var idx = i;
				if (bReverseReadOrder)
					idx = points.Count - i - 1;
				Add(points[idx], -1, false);
			}
			if (mWindingOrder == WindingOrderType.Unknown)
				mWindingOrder = CalculateWindingOrder();
			mEpsilon = CalculateEpsilon();
		}

		public void AddRange(IList<TriangulationPoint> points, WindingOrderType windingOrder)
		{
			if (points == null || points.Count < 1)
				return;

			if (mWindingOrder == WindingOrderType.Unknown && Count == 0)
				mWindingOrder = windingOrder;

			var numPoints = points.Count;
			var bReverseReadOrder = WindingOrder != WindingOrderType.Unknown &&
			                        windingOrder != WindingOrderType.Unknown &&
			                        WindingOrder != windingOrder;
			for (var i = 0; i < numPoints; ++i)
			{
				var idx = i;
				if (bReverseReadOrder)
					idx = points.Count - i - 1;
				Add(points[idx], -1, false);
			}
			if (mWindingOrder == WindingOrderType.Unknown)
				mWindingOrder = CalculateWindingOrder();
			mEpsilon = CalculateEpsilon();
		}

		public void Insert(int idx, TriangulationPoint p)
		{
			Add(p, idx, true);
		}

		public bool Remove(TriangulationPoint p) => base.Remove(p);

		/// <summary>
		/// Removes a point from the polygon.  Note this can be a somewhat expensive operation
		/// as it must recalculate the bounding area from scratch.
		/// </summary>
		/// <param name="p"></param>
		public void RemovePoint(PolygonPoint p)
		{
			PolygonPoint next, prev;

			next = p.Next;
			prev = p.Previous;
			prev.Next = next;
			next.Previous = prev;
			mPoints.Remove(p);

			mBoundingBox.Clear();
			foreach (PolygonPoint tmp in mPoints)
				mBoundingBox.AddPoint(tmp);
		}

		public bool Contains(TriangulationPoint p) => mPoints.Contains(p);

		public void CopyTo(TriangulationPoint[] array, int arrayIndex)
		{
			var numElementsToCopy = Math.Min(Count, array.Length - arrayIndex);
			for (var i = 0; i < numElementsToCopy; ++i)
				array[arrayIndex + i] = mPoints[i] as TriangulationPoint;
		}

		public void AddSteinerPoint(TriangulationPoint point)
		{
			if (mSteinerPoints == null)
				mSteinerPoints = new List<TriangulationPoint>();
			mSteinerPoints.Add(point);
		}

		public void AddSteinerPoints(List<TriangulationPoint> points)
		{
			if (mSteinerPoints == null)
				mSteinerPoints = new List<TriangulationPoint>();
			mSteinerPoints.AddRange(points);
		}

		public void ClearSteinerPoints()
		{
			if (mSteinerPoints != null)
				mSteinerPoints.Clear();
		}

		/// <summary>
		/// Add a hole to the polygon.
		/// </summary>
		/// <param name="poly">A subtraction polygon fully contained inside this polygon.</param>
		public void AddHole(Polygon poly)
		{
			if (mHoles == null)
				mHoles = new List<Polygon>();
			mHoles.Add(poly);
			// XXX: tests could be made here to be sure it is fully inside
			//        addSubtraction( poly.getPoints() );
		}

		public void AddTriangle(DelaunayTriangle t)
		{
			mTriangles.Add(t);
		}

		public void AddTriangles(IEnumerable<DelaunayTriangle> list)
		{
			mTriangles.AddRange(list);
		}

		public void ClearTriangles()
		{
			if (mTriangles != null)
				mTriangles.Clear();
		}

		public bool IsPointInside(TriangulationPoint p) => PolygonUtil.PointInPolygon2D(this, p);

		/// <summary>
		/// Creates constraints and populates the context with points
		/// </summary>
		/// <param name="tcx">The context</param>
		public void Prepare(TriangulationContext tcx)
		{
			if (mTriangles == null)
				mTriangles = new List<DelaunayTriangle>(mPoints.Count);
			else
				mTriangles.Clear();

			// Outer constraints
			for (var i = 0; i < mPoints.Count - 1; i++) //tcx.NewConstraint(mPoints[i], mPoints[i + 1]);
				tcx.NewConstraint(this[i], this[i + 1]);
			tcx.NewConstraint(this[Count - 1], this[0]); // Changed by Kronnect Games
//			tcx.NewConstraint (this [0], this [Count - 1]);

			tcx.Points.AddRange(this);

			// Hole constraints
			if (mHoles != null)
				foreach (var p in mHoles)
				{
					for (var i = 0; i < p.mPoints.Count - 1; i++)
						tcx.NewConstraint(p[i], p[i + 1]);
					tcx.NewConstraint(p[p.Count - 1], p[0]); // Changed by Kronnect Games
//					tcx.NewConstraint (p [0], p [p.Count - 1]);
					tcx.Points.AddRange(p);
				}

			if (mSteinerPoints != null)
				tcx.Points.AddRange(mSteinerPoints);
		}
	}
}