using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class Triangulator
	{
		private Vector3[] m_points;
		private int m_numPoints;

		public static int[] GetPoints(Vector3[] points)
		{
			var triangulator = new Triangulator(points);
			return triangulator.Triangulate();
		}

		public Triangulator(Vector3[] points)
		{
			m_points = points;
			m_numPoints = points.Length;
		}

		public int[] Triangulate()
		{
			var n = m_numPoints;
			if (n < 3)
				return new int[0];
			var indices = new List<int>(n * 3);
			var V = new int[n];
			if (Area() > 0)
				for (var v = 0; v < n; v++)
					V[v] = v;
			else
				for (var v = 0; v < n; v++)
					V[v] = n - 1 - v;
			var nv = n;
			var count = 2 * nv;
			var sizeofInt = sizeof(int);

			for (var v = nv - 1; nv > 2;)
			{
				if (count-- <= 0)
					return indices.ToArray();

				var u = v;
				if (nv <= u)
					u = 0;
				v = u + 1;
				if (nv <= v)
					v = 0;
				var w = v + 1;
				if (nv <= w)
					w = 0;

				if (Snip(u, v, w, nv, V))
				{
					int a, b, c;
					a = V[u];
					b = V[v];
					c = V[w];
					indices.Add(a);
					indices.Add(b);
					indices.Add(c);
					Buffer.BlockCopy(V, (v + 1) * sizeofInt, V, v * sizeofInt,
						(nv - v - 1) * sizeofInt); // fast shift array to the left one position
					nv--;
					count = 2 * nv;
				}
			}
			indices.Reverse();
			return indices.ToArray();
		}

		private bool Snip(int u, int v, int w, int n, int[] V)
		{
			var A = m_points[V[u]];
			var B = m_points[V[v]];
			var C = m_points[V[w]];
			if (Mathf.Epsilon > (B.x - A.x) * (C.y - A.y) - (B.y - A.y) * (C.x - A.x))
				return false;
			for (var p = 0; p < n; p++)
				if (InsideTriangle(A, B, C, m_points[V[p]]))
				{
					if (p == u || p == v || p == w)
						continue;
					return false;
				}
			return true;
		}

		private bool InsideTriangle(Vector3 A, Vector3 B, Vector3 C, Vector3 P)
		{
			float ax, ay, bx, by, cx, cy, apx, apy, bpx, bpy, cpx, cpy;
			float cCROSSap, bCROSScp, aCROSSbp;

			ax = C.x - B.x;
			bpy = P.y - B.y;
			ay = C.y - B.y;
			bpx = P.x - B.x;
			aCROSSbp = ax * bpy - ay * bpx;
			if (aCROSSbp < 0.0f)
				return false;

			bx = A.x - C.x;
			by = A.y - C.y;
			cpx = P.x - C.x;
			cpy = P.y - C.y;
			bCROSScp = bx * cpy - by * cpx;
			if (bCROSScp < 0.0f)
				return false;

			cx = B.x - A.x;
			cy = B.y - A.y;
			apx = P.x - A.x;
			apy = P.y - A.y;
			cCROSSap = cx * apy - cy * apx;
			return cCROSSap >= 0.0f;
		}

		private float Area()
		{
			var n = m_numPoints;
			var A = 0.0f;
			for (int p = n - 1, q = 0; q < n; p = q++)
			{
				Vector2 pval = m_points[p];
				Vector2 qval = m_points[q];
				A += pval.x * qval.y - qval.x * pval.y;
			}
			return A * 0.5f;
		}
	}
}