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

using System;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit.Poly2Tri
{
	public class PointSet : Point2DList, ITriangulatable, IEnumerable<TriangulationPoint>,
		IList<TriangulationPoint>
	{
		protected Dictionary<uint, TriangulationPoint> mPointMap = new();

		public IList<TriangulationPoint> Points { get => this; private set { } }

		public IList<DelaunayTriangle> Triangles { get; private set; }

		public string FileName { get; set; }

		public bool DisplayFlipX { get; set; }

		public bool DisplayFlipY { get; set; }

		public float DisplayRotate { get; set; }

		protected double mPrecision = TriangulationPoint.kVertexCodeDefaultPrecision;

		public double Precision { get => mPrecision; set => mPrecision = value; }

		public double MinX => mBoundingBox.MinX;

		public double MaxX => mBoundingBox.MaxX;

		public double MinY => mBoundingBox.MinY;

		public double MaxY => mBoundingBox.MaxY;

		public Rect2D Bounds => mBoundingBox;

		public virtual TriangulationMode TriangulationMode => TriangulationMode.Unconstrained;

		public new TriangulationPoint this[int index]
		{
			get => mPoints[index] as TriangulationPoint;
			set => mPoints[index] = value;
		}

		public PointSet(List<TriangulationPoint> bounds)
		{
			//Points = new List<TriangulationPoint>();
			foreach (var p in bounds)
			{
				Add(p, -1, false);

				// Only the initial points are counted toward min/max x/y as they 
				// are considered to be the boundaries of the point-set
				mBoundingBox.AddPoint(p);
			}
			mEpsilon = CalculateEpsilon();
			mWindingOrder = WindingOrderType.Unknown; // not valid for a point-set
		}

		IEnumerator<TriangulationPoint> IEnumerable<TriangulationPoint>.GetEnumerator() =>
			new TriangulationPointEnumerator(mPoints);

		public int IndexOf(TriangulationPoint p) => mPoints.IndexOf(p);

		public override void Add(Point2D p)
		{
			Add(p as TriangulationPoint, -1, false);
		}

		public virtual void Add(TriangulationPoint p)
		{
			Add(p, -1, false);
		}

		protected override void Add(Point2D p, int idx, bool constrainToBounds)
		{
			Add(p as TriangulationPoint, idx, constrainToBounds);
		}

		protected bool Add(TriangulationPoint p, int idx, bool constrainToBounds)
		{
			if (p == null)
				return false;

			if (constrainToBounds)
				ConstrainPointToBounds(p);

			// if we already have an instance of the point, then don't bother inserting it again as duplicate points
			// will actually cause some real problems later on.   Still return true though to indicate that the point
			// is successfully "added"
			if (mPointMap.ContainsKey(p.VertexCode))
				return true;
			mPointMap.Add(p.VertexCode, p);

			if (idx < 0)
				mPoints.Add(p);
			else
				mPoints.Insert(idx, p);

			return true;
		}

		public override void AddRange(IEnumerator<Point2D> iter, WindingOrderType windingOrder)
		{
			if (iter == null)
				return;

			iter.Reset();
			while (iter.MoveNext())
				Add(iter.Current);
		}

		public virtual bool AddRange(List<TriangulationPoint> points)
		{
			var bOK = true;
			foreach (var p in points)
				bOK = Add(p, -1, false) && bOK;

			return bOK;
		}

		public bool TryGetPoint(double x, double y, out TriangulationPoint p)
		{
			var vc = TriangulationPoint.CreateVertexCode(x, y, Precision);
			if (mPointMap.TryGetValue(vc, out p))
				return true;

			return false;
		}

		//public override void Insert(int idx, Point2D item)
		//{
		//    Add(item, idx, true);
		//}

		public void Insert(int idx, TriangulationPoint item)
		{
			mPoints.Insert(idx, item);
		}

		public override bool Remove(Point2D p) => mPoints.Remove(p);

		public bool Remove(TriangulationPoint p) => mPoints.Remove(p);

		public override void RemoveAt(int idx)
		{
			if (idx < 0 || idx >= Count)
				return;
			mPoints.RemoveAt(idx);
		}

		public bool Contains(TriangulationPoint p) => mPoints.Contains(p);

		public void CopyTo(TriangulationPoint[] array, int arrayIndex)
		{
			var numElementsToCopy = Math.Min(Count, array.Length - arrayIndex);
			for (var i = 0; i < numElementsToCopy; ++i)
				array[arrayIndex + i] = mPoints[i] as TriangulationPoint;
		}

		// returns true if the point is changed, false if the point is unchanged
		protected bool ConstrainPointToBounds(Point2D p)
		{
			var oldX = p.X;
			var oldY = p.Y;
			p.X = Math.Max(MinX, p.X);
			p.X = Math.Min(MaxX, p.X);
			p.Y = Math.Max(MinY, p.Y);
			p.Y = Math.Min(MaxY, p.Y);

			return p.X != oldX || p.Y != oldY;
		}

		protected bool ConstrainPointToBounds(TriangulationPoint p)
		{
			var oldX = p.X;
			var oldY = p.Y;
			p.X = Math.Max(MinX, p.X);
			p.X = Math.Min(MaxX, p.X);
			p.Y = Math.Max(MinY, p.Y);
			p.Y = Math.Min(MaxY, p.Y);

			return p.X != oldX || p.Y != oldY;
		}

		public virtual void AddTriangle(DelaunayTriangle t)
		{
			Triangles.Add(t);
		}

		public void AddTriangles(IEnumerable<DelaunayTriangle> list)
		{
			foreach (var tri in list)
				AddTriangle(tri);
		}

		public void ClearTriangles()
		{
			Triangles.Clear();
		}

		public virtual bool Initialize() => true;

		public virtual void Prepare(TriangulationContext tcx)
		{
			if (Triangles == null)
				Triangles = new List<DelaunayTriangle>(Points.Count);
			else
				Triangles.Clear();
			tcx.Points.AddRange(Points);
		}
	}
}