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

/*
 * The Following notice applies to the Methods CheckPolygon and 
 * MergeParallelEdges.   Both are altered only enough to convert to C#
 * and take advantage of some of C#'s language features.   Any errors
 * are thus mine from the conversion and not Eric's.
 * 
 * Copyright (c) 2007 Eric Jordan
 * 
 * This software is provided 'as-is', without any express or implied
 * warranty.  In no event will the authors be held liable for any damages
 * arising from the use of this software.
 * Permission is granted to anyone to use this software for any purpose,
 * including commercial applications, and to alter it and redistribute it
 * freely, subject to the following restrictions:
 * 1. The origin of this software must not be misrepresented; you must not
 *    claim that you wrote the original software. If you use this software
 *    in a product, an acknowledgment in the product documentation would be
 *    appreciated but is not required.
 * 2. Altered source versions must be plainly marked as such, and must not be
 *    misrepresented as being the original software.
 * 3. This notice may not be removed or altered from any source distribution.
 * */

using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;

namespace WorldMapStrategyKit.Poly2Tri
{
	public class Point2DList : IEnumerable<Point2D>, IList<Point2D>
	{
		// : List<Point2D>
		public static readonly int kMaxPolygonVertices = 100000; // adjust to suit...

		/// A small length used as a collision and constraint tolerance. Usually it is
		/// chosen to be numerically significant, but visually insignificant.
		public static readonly double kLinearSlop = 0.005;

		/// A small angle used as a collision and constraint tolerance. Usually it is
		/// chosen to be numerically significant, but visually insignificant.
		public static readonly double kAngularSlop = 2.0 / (180.0 * Math.PI);

		public enum WindingOrderType
		{
			CW,
			CCW,
			Unknown,

			Default = CCW
		}

		[Flags]
		public enum PolygonError : uint
		{
			None = 0,
			NotEnoughVertices = 1 << 0,
			NotConvex = 1 << 1,
			NotSimple = 1 << 2,
			AreaTooSmall = 1 << 3,
			SidesTooCloseToParallel = 1 << 4,
			TooThin = 1 << 5,
			Degenerate = 1 << 6,
			Unknown = 1 << 30
		}

		protected List<Point2D> mPoints = new();
		protected Rect2D mBoundingBox = new();
		protected WindingOrderType mWindingOrder = WindingOrderType.Unknown;

		protected double
			mEpsilon = MathUtil.EPSILON; // Epsilon is a function of the size of the bounds of the polygon

		public Rect2D BoundingBox => mBoundingBox;

		public WindingOrderType WindingOrder
		{
			get => mWindingOrder;
			set
			{
				if (mWindingOrder == WindingOrderType.Unknown)
					mWindingOrder = CalculateWindingOrder();
				if (value != mWindingOrder)
				{
					mPoints.Reverse();
					mWindingOrder = value;
				}
			}
		}

		public double Epsilon => mEpsilon;

		public Point2D this[int index] { get => mPoints[index]; set => mPoints[index] = value; }

		public int Count => mPoints.Count;

		public virtual bool IsReadOnly => false;

		public Point2DList() { }

		public Point2DList(int capacity) => mPoints.Capacity = capacity;

		public Point2DList(IList<Point2D> l)
		{
			AddRange(l.GetEnumerator(), WindingOrderType.Unknown);
		}

		public Point2DList(Point2DList l)
		{
			var numPoints = l.Count;
			for (var i = 0; i < numPoints; ++i)
				mPoints.Add(l[i]);
			mBoundingBox.Set(l.BoundingBox);
			mEpsilon = l.Epsilon;
			mWindingOrder = l.WindingOrder;
		}

		public override string ToString()
		{
			var builder = new StringBuilder();
			for (var i = 0; i < Count; i++)
			{
				builder.Append(this[i].ToString());
				if (i < Count - 1)
					builder.Append(" ");
			}
			return builder.ToString();
		}

		IEnumerator IEnumerable.GetEnumerator() => mPoints.GetEnumerator();

		IEnumerator<Point2D> IEnumerable<Point2D>.GetEnumerator() => new Point2DEnumerator(mPoints);

		public void Clear()
		{
			mPoints.Clear();
			mBoundingBox.Clear();
			mEpsilon = MathUtil.EPSILON;
			mWindingOrder = WindingOrderType.Unknown;
		}

		public int IndexOf(Point2D p) => mPoints.IndexOf(p);

		public virtual void Add(Point2D p)
		{
			Add(p, -1, true);
		}

		protected virtual void Add(Point2D p, int idx, bool bCalcWindingOrderAndEpsilon)
		{
			if (idx < 0)
				mPoints.Add(p);
			else
				mPoints.Insert(idx, p);
			mBoundingBox.AddPoint(p);
			if (bCalcWindingOrderAndEpsilon)
			{
				if (mWindingOrder == WindingOrderType.Unknown)
					mWindingOrder = CalculateWindingOrder();
				mEpsilon = CalculateEpsilon();
			}
		}

		public virtual void AddRange(Point2DList l)
		{
			AddRange(l.mPoints.GetEnumerator(), l.WindingOrder);
		}

		public virtual void AddRange(IEnumerator<Point2D> iter, WindingOrderType windingOrder)
		{
			if (iter == null)
				return;

			if (mWindingOrder == WindingOrderType.Unknown && Count == 0)
				mWindingOrder = windingOrder;
			var bReverseReadOrder = WindingOrder != WindingOrderType.Unknown &&
			                        windingOrder != WindingOrderType.Unknown &&
			                        WindingOrder != windingOrder;
			var bAddedFirst = true;
			var startCount = mPoints.Count;
			iter.Reset();
			while (iter.MoveNext())
			{
				if (!bAddedFirst)
				{
					bAddedFirst = true;
					mPoints.Add(iter.Current);
				}
				else if (bReverseReadOrder)
					mPoints.Insert(startCount, iter.Current);
				else
					mPoints.Add(iter.Current);
				mBoundingBox.AddPoint(iter.Current);
			}
			if (mWindingOrder == WindingOrderType.Unknown && windingOrder == WindingOrderType.Unknown)
				mWindingOrder = CalculateWindingOrder();
			mEpsilon = CalculateEpsilon();
		}

		public virtual void Insert(int idx, Point2D item)
		{
			Add(item, idx, true);
		}

		public virtual bool Remove(Point2D p)
		{
			if (mPoints.Remove(p))
			{
				CalculateBounds();
				mEpsilon = CalculateEpsilon();
				return true;
			}

			return false;
		}

		public virtual void RemoveAt(int idx)
		{
			if (idx < 0 || idx >= Count)
				return;
			mPoints.RemoveAt(idx);
			CalculateBounds();
			mEpsilon = CalculateEpsilon();
		}

		public virtual void RemoveRange(int idxStart, int count)
		{
			if (idxStart < 0 || idxStart >= Count)
				return;
			if (count == 0)
				return;

			mPoints.RemoveRange(idxStart, count);
			CalculateBounds();
			mEpsilon = CalculateEpsilon();
		}

		public bool Contains(Point2D p) => mPoints.Contains(p);

		public void CopyTo(Point2D[] array, int arrayIndex)
		{
			var numElementsToCopy = Math.Min(Count, array.Length - arrayIndex);
			for (var i = 0; i < numElementsToCopy; ++i)
				array[arrayIndex + i] = mPoints[i];
		}

		public void CalculateBounds()
		{
			mBoundingBox.Clear();
			foreach (var pt in mPoints)
				mBoundingBox.AddPoint(pt);
		}

		public double CalculateEpsilon() =>
			//			return Point2D.PRECISION; // TODO: Changed by Kronnect Games Math.Max (Math.Min (mBoundingBox.Width, mBoundingBox.Height) * 0.001f, MathUtil.EPSILON);
			Math.Max(Math.Min(mBoundingBox.Width, mBoundingBox.Height) * 0.001f, MathUtil.EPSILON);

		public WindingOrderType CalculateWindingOrder()
		{
			// the sign of the 'area' of the polygon is all we are interested in.
			var area = GetSignedArea();
			if (area < 0.0)
				return WindingOrderType.CW;
			else if (area > 0.0)
				return WindingOrderType.CCW;

			// error condition - not even verts to calculate, non-simple poly, etc.
			return WindingOrderType.Unknown;
		}

		public int NextIndex(int index)
		{
			if (index == Count - 1)
				return 0;
			return index + 1;
		}

		/// <summary>
		/// Gets the previous index.
		/// </summary>
		/// <param name="index">The index.</param>
		/// <returns></returns>
		public int PreviousIndex(int index)
		{
			if (index == 0)
				return Count - 1;
			return index - 1;
		}

		/// <summary>
		/// Gets the signed area.
		/// </summary>
		/// <returns></returns>
		public double GetSignedArea()
		{
			var area = 0.0;
			for (var i = 0; i < Count; i++)
			{
				var j = (i + 1) % Count;
				area += this[i].X * this[j].Y;
				area -= this[i].Y * this[j].X;
			}
			area /= 2.0f;

			return area;
		}

		/// <summary>
		/// Gets the area.
		/// </summary>
		/// <returns></returns>
		public double GetArea()
		{
			int i;
			double area = 0;

			for (i = 0; i < Count; i++)
			{
				var j = (i + 1) % Count;
				area += this[i].X * this[j].Y;
				area -= this[i].Y * this[j].X;
			}
			area /= 2.0f;
			return area < 0 ? -area : area;
		}

		/// <summary>
		/// Gets the centroid.
		/// </summary>
		/// <returns></returns>
		public Point2D GetCentroid()
		{
			// Same algorithm is used by Box2D

			var c = new Point2D();
			double area = 0.0f;

			const double inv3 = 1.0 / 3.0;
			var pRef = new Point2D();
			for (var i = 0; i < Count; ++i)
			{
				// Triangle vertices.
				var p1 = pRef;
				var p2 = this[i];
				var p3 = i + 1 < Count ? this[i + 1] : this[0];

				var e1 = p2 - p1;
				var e2 = p3 - p1;

				var D = Point2D.Cross(e1, e2);

				var triangleArea = 0.5f * D;
				area += triangleArea;

				// Area weighted centroid
				c += triangleArea * inv3 * (p1 + p2 + p3);
			}

			// Centroid
			c *= 1.0f / area;
			return c;
		}

		//    /// <summary>
		/// Translates the vertices with the specified vector.
		/// </summary>
		/// <param name="vector">The vector.</param>
		public void Translate(Point2D vector)
		{
			for (var i = 0; i < Count; i++)
				this[i] += vector;
		}

		/// <summary>
		/// Scales the vertices with the specified vector.
		/// </summary>
		/// <param name="value">The Value.</param>
		public void Scale(Point2D value)
		{
			for (var i = 0; i < Count; i++)
				this[i] *= value;
		}

		/// <summary>
		/// Rotate the vertices with the defined value in radians.
		/// </summary>
		/// <param name="value">The amount to rotate by in radians.</param>
		public void Rotate(double radians)
		{
			// kickin' it old-skool since I don't want to create a Matrix class for now.
			var cosr = Math.Cos(radians);
			var sinr = Math.Sin(radians);
			foreach (var p in mPoints)
			{
				var xold = p.X;
				p.X = xold * cosr - p.Y * sinr;
				p.Y = xold * sinr + p.Y * cosr;
			}
		}

		// A degenerate polygon is one in which some vertex lies on an edge joining two other vertices. 
		// This can happen in one of two ways: either the vertices V(i-1), V(i), and V(i+1) can be collinear or
		// the vertices V(i) and V(i+1) can overlap (fail to be distinct). In either of these cases, our polygon of
		// n vertices will appear to have n - 1 or fewer -- it will have "degenerated" from an n-gon to an (n-1)-gon.
		// (In the case of triangles, this will result in either a line segment or a point.) 
		public bool IsDegenerate()
		{
			if (Count < 3)
				return false;
			if (Count < 3)
				return false;
			for (var k = 0; k < Count; ++k)
			{
				var j = PreviousIndex(k);
				if (mPoints[j].Equals(mPoints[k], Epsilon))
					return true;
				var i = PreviousIndex(j);
				var orientation = TriangulationUtil.Orient2d(mPoints[i], mPoints[j], mPoints[k]);
				if (orientation == Orientation.Collinear)
					return true;
			}

			return false;
		}

		/// <summary>
		/// Assuming the polygon is simple; determines whether the polygon is convex.
		/// </summary>
		/// <returns>
		///   <c>true</c> if it is convex; otherwise, <c>false</c>.
		/// </returns>
		public bool IsConvex()
		{
			var isPositive = false;

			for (var i = 0; i < Count; ++i)
			{
				var lower = i == 0 ? Count - 1 : i - 1;
				var middle = i;
				var upper = i == Count - 1 ? 0 : i + 1;

				var dx0 = this[middle].X - this[lower].X;
				var dy0 = this[middle].Y - this[lower].Y;
				var dx1 = this[upper].X - this[middle].X;
				var dy1 = this[upper].Y - this[middle].Y;

				var cross = dx0 * dy1 - dx1 * dy0;

				// Cross product should have same sign
				// for each vertex if poly is convex.
				var newIsP = cross >= 0 ? true : false;
				if (i == 0)
					isPositive = newIsP;
				else if (isPositive != newIsP)
					return false;
			}

			return true;
		}

		/// <summary>
		/// Check for edge crossings
		/// </summary>
		/// <returns></returns>
		public bool IsSimple()
		{
			for (var i = 0; i < Count; ++i)
			{
				var iplus = NextIndex(i);
				for (var j = i + 1; j < Count; ++j)
				{
					var jplus = NextIndex(j);
					Point2D temp = null;
					if (TriangulationUtil.LinesIntersect2D(mPoints[i], mPoints[iplus], mPoints[j],
						mPoints[jplus], ref temp, mEpsilon))
						return false;
				}
			}
			return true;
		}

		/// <summary>
		/// Checks if polygon is valid for use in Box2d engine.
		/// Last ditch effort to ensure no invalid polygons are
		/// added to world geometry.
		///
		/// Performs a full check, for simplicity, convexity,
		/// orientation, minimum angle, and volume.  This won't
		/// be very efficient, and a lot of it is redundant when
		/// other tools in this section are used.
		///
		/// From Eric Jordan's convex decomposition library
		/// </summary>
		/// <param name="printErrors"></param>
		/// <returns></returns>
		public PolygonError CheckPolygon()
		{
			var error = PolygonError.None;
			if (Count < 3 || Count > kMaxPolygonVertices)
			{
				error |= PolygonError.NotEnoughVertices;
				// no other tests will be valid at this point, so just return
				return error;
			}
			if (IsDegenerate())
				error |= PolygonError.Degenerate;
			//bool bIsConvex = IsConvex();
			//if (!IsConvex())
			//{
			//    error |= PolygonError.NotConvex;
			//}
			if (!IsSimple())
				error |= PolygonError.NotSimple;
			if (GetArea() < MathUtil.EPSILON)
				error |= PolygonError.AreaTooSmall;

			// the following tests don't make sense if the polygon is not simple
			if ((error & PolygonError.NotSimple) != PolygonError.NotSimple)
			{
				var bReversed = false;
				var expectedWindingOrder = WindingOrderType.CCW;
				var reverseWindingOrder = WindingOrderType.CW;
				if (WindingOrder == reverseWindingOrder)
				{
					WindingOrder = expectedWindingOrder;
					bReversed = true;
				}

				//Compute normals
				var normals = new Point2D[Count];
				var vertices = new Point2DList(Count);
				for (var i = 0; i < Count; ++i)
				{
					vertices.Add(new Point2D(this[i].X, this[i].Y));
					var i1 = i;
					var i2 = NextIndex(i);
					var edge = new Point2D(this[i2].X - this[i1].X, this[i2].Y - this[i1].Y);
					normals[i] = Point2D.Perpendicular(edge, 1.0);
					normals[i].Normalize();
				}

				//Required side checks
				for (var i = 0; i < Count; ++i)
				{
					var iminus = PreviousIndex(i);

					//Parallel sides check
					var cross = Point2D.Cross(normals[iminus], normals[i]);
					cross = MathUtil.Clamp(cross, -1.0f, 1.0f);
					var angle = (float)Math.Asin(cross);
					if (Math.Abs(angle) <= kAngularSlop)
					{
						error |= PolygonError.SidesTooCloseToParallel;
						break;
					}

					// For some reason, the following checks do not seem to work
					// correctly in all cases - they return false positives.
					//    //Too skinny check - only valid for convex polygons
					//    if (bIsConvex)
					//    {
					//        for (int j = 0; j < Count; ++j)
					//        {
					//            if (j == i || j == NextIndex(i))
					//            {
					//                continue;
					//            }
					//            Point2D testVector = vertices[j] - vertices[i];
					//            testVector.Normalize();
					//            double s = Point2D.Dot(testVector, normals[i]);
					//            if (s >= -Point2DList.kLinearSlop)
					//            {
					//                error |= PolygonError.TooThin;
					//            }
					//        }

					//        Point2D centroid = vertices.GetCentroid();
					//        Point2D n1 = normals[iminus];
					//        Point2D n2 = normals[i];
					//        Point2D v = vertices[i] - centroid;

					//        Point2D d = new Point2D();
					//        d.X = Point2D.Dot(n1, v); // - toiSlop;
					//        d.Y = Point2D.Dot(n2, v); // - toiSlop;

					//        // Shifting the edge inward by toiSlop should
					//        // not cause the plane to pass the centroid.
					//        if ((d.X < 0.0f) || (d.Y < 0.0f))
					//        {
					//            error |= PolygonError.TooThin;
					//        }
					//    }
				}

				if (bReversed)
					WindingOrder = reverseWindingOrder;
			}

			//if (error != PolygonError.None)
			//{
			//    Console.WriteLine("Found invalid polygon: {0} {1}\n", Point2DList.GetErrorString(error), this.ToString());
			//}

			return error;
		}

		public static string GetErrorString(PolygonError error)
		{
			var sb = new StringBuilder(256);
			if (error == PolygonError.None)
				sb.AppendFormat("No errors.\n");
			else
			{
				if ((error & PolygonError.NotEnoughVertices) == PolygonError.NotEnoughVertices)
					sb.AppendFormat("NotEnoughVertices: must have between 3 and {0} vertices.\n",
						kMaxPolygonVertices);
				if ((error & PolygonError.NotConvex) == PolygonError.NotConvex)
					sb.AppendFormat("NotConvex: Polygon is not convex.\n");
				if ((error & PolygonError.NotSimple) == PolygonError.NotSimple)
					sb.AppendFormat("NotSimple: Polygon is not simple (i.e. it intersects itself).\n");
				if ((error & PolygonError.AreaTooSmall) == PolygonError.AreaTooSmall)
					sb.AppendFormat("AreaTooSmall: Polygon's area is too small.\n");
				if ((error & PolygonError.SidesTooCloseToParallel) == PolygonError.SidesTooCloseToParallel)
					sb.AppendFormat(
						"SidesTooCloseToParallel: Polygon's sides are too close to parallel.\n");
				if ((error & PolygonError.TooThin) == PolygonError.TooThin)
					sb.AppendFormat(
						"TooThin: Polygon is too thin or core shape generation would move edge past centroid.\n");
				if ((error & PolygonError.Degenerate) == PolygonError.Degenerate)
					sb.AppendFormat(
						"Degenerate: Polygon is degenerate (contains collinear points or duplicate coincident points).\n");
				if ((error & PolygonError.Unknown) == PolygonError.Unknown)
					sb.AppendFormat("Unknown: Unknown Polygon error!.\n");
			}

			return sb.ToString();
		}

		/// <summary>
		/// Removes duplicate points that lie next to each other in the list
		/// </summary>
		public void RemoveDuplicateNeighborPoints()
		{
			var numPoints = Count;
			var i = numPoints - 1;
			var j = 0;
			while (numPoints > 1 && j < numPoints)
				if (mPoints[i].Equals(mPoints[j]))
				{
					var idxToRemove = Math.Max(i, j);
					mPoints.RemoveAt(idxToRemove);
					--numPoints;
					if (i >= numPoints) // can happen if first element in list is deleted...
						i = numPoints - 1;
					// don't increment i, j in this case because we want to check i against the new value at j
				}
				else
				{
					i = NextIndex(i);
					++j; // intentionally not wrapping value of j so we have a valid end-point for the loop
				}
		}

		/// <summary>
		/// Removes all collinear points on the polygon.
		/// Has a default bias of 0
		/// </summary>
		/// <param name="polygon">The polygon that needs simplification.</param>
		/// <returns>A simplified polygon.</returns>
		public void Simplify()
		{
			Simplify(0.0);
		}

		/// <summary>
		/// Removes all collinear points on the polygon.   Note that this is NOT safe to run on a complex
		/// polygon as it will remove points that it should not.   For example, consider this polygon:
		/// 
		///           2
		///           +
		///          / \
		///         /   \
		///        /     \
		/// 0 +---+-------+
		///       3       1
		/// 
		/// This algorithm would delete point 3, leaving you with the polygon 0,1,2 - definitely NOT the correct
		/// polygon.  Caveat Emptor!
		/// 
		/// </summary>
		/// <param name="polygon">The polygon that needs simplification.</param>
		/// <param name="bias">The distance bias between points. Points closer than this will be 'joined'.</param>
		/// <returns>A simplified polygon.</returns>
		public void Simplify(double bias)
		{
			//We can't simplify polygons under 3 vertices
			if (Count < 3)
				return;

			//#if DEBUG
//            if (!IsSimple())
//            {
//                throw new Exception("Do not run Simplify on a non-simple polygon!");
//            }
//#endif

			var curr = 0;
			var numVerts = Count;
			var biasSquared = bias * bias;
			while (curr < numVerts && numVerts >= 3)
			{
				var prevId = PreviousIndex(curr);
				var nextId = NextIndex(curr);

				var prev = this[prevId];
				var current = this[curr];
				var next = this[nextId];

				//If they are closer than the bias, continue
				if ((prev - current).MagnitudeSquared() <= biasSquared)
				{
					RemoveAt(curr);
					--numVerts;
					continue;
				}

				//If they collinear, continue
				var orientation = TriangulationUtil.Orient2d(prev, current, next);
				if (orientation == Orientation.Collinear)
				{
					RemoveAt(curr);
					--numVerts;
					continue;
				}

				++curr;
			}
		}

		// From Eric Jordan's convex decomposition library
		/// <summary>
		/// Merges all parallel edges in the list of vertices
		/// </summary>
		/// <param name="tolerance"></param>
		public void MergeParallelEdges(double tolerance)
		{
			if (Count <= 3) // Can't do anything useful here to a triangle
				return;

			var mergeMe = new bool[Count];
			var newNVertices = Count;

			//Gather points to process
			for (var i = 0; i < Count; ++i)
			{
				var lower = i == 0 ? Count - 1 : i - 1;
				var middle = i;
				var upper = i == Count - 1 ? 0 : i + 1;

				var dx0 = this[middle].X - this[lower].X;
				var dy0 = this[middle].Y - this[lower].Y;
				var dx1 = this[upper].Y - this[middle].X;
				var dy1 = this[upper].Y - this[middle].Y;
				var norm0 = Math.Sqrt(dx0 * dx0 + dy0 * dy0);
				var norm1 = Math.Sqrt(dx1 * dx1 + dy1 * dy1);

				if (!(norm0 > 0.0 && norm1 > 0.0) && newNVertices > 3)
				{
					//Merge identical points
					mergeMe[i] = true;
					--newNVertices;
				}

				dx0 /= norm0;
				dy0 /= norm0;
				dx1 /= norm1;
				dy1 /= norm1;
				var cross = dx0 * dy1 - dx1 * dy0;
				var dot = dx0 * dx1 + dy0 * dy1;

				if (Math.Abs(cross) < tolerance && dot > 0 && newNVertices > 3)
				{
					mergeMe[i] = true;
					--newNVertices;
				}
				else
					mergeMe[i] = false;
			}

			if (newNVertices == Count || newNVertices == 0)
				return;

			var currIndex = 0;

			// Copy the vertices to a new list and clear the old
			var oldVertices = new Point2DList(this);
			Clear();

			for (var i = 0; i < oldVertices.Count; ++i)
			{
				if (mergeMe[i] || newNVertices == 0 || currIndex == newNVertices)
					continue;

				if (currIndex >= newNVertices)
					throw new Exception("Point2DList::MergeParallelEdges - currIndex[ " +
					                    currIndex.ToString() +
					                    "] >= newNVertices[" +
					                    newNVertices +
					                    "]");

				mPoints.Add(oldVertices[i]);
				mBoundingBox.AddPoint(oldVertices[i]);
				++currIndex;
			}
			mWindingOrder = CalculateWindingOrder();
			mEpsilon = CalculateEpsilon();
		}

		/// <summary>
		/// Projects to axis.
		/// </summary>
		/// <param name="axis">The axis.</param>
		/// <param name="min">The min.</param>
		/// <param name="max">The max.</param>
		public void ProjectToAxis(Point2D axis, out double min, out double max)
		{
			// To project a point on an axis use the dot product
			var dotProduct = Point2D.Dot(axis, this[0]);
			min = dotProduct;
			max = dotProduct;

			for (var i = 0; i < Count; i++)
			{
				dotProduct = Point2D.Dot(this[i], axis);
				if (dotProduct < min)
					min = dotProduct;
				else
				{
					if (dotProduct > max)
						max = dotProduct;
				}
			}
		}
	}
}