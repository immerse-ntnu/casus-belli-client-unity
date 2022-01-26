using System;
using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit.MapGenerator.Geom
{
	public struct Point : IEqualityComparer<Point>
	{
		public const double PRECISION = 1e-8;
		public static Point zero = new(0, 0);
		public double x, y;

		public Point(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		public Point(Vector3 p)
		{
			x = p.x;
			y = p.y;
		}

		public void SetXY(double x, double y)
		{
			this.x = x;
			this.y = y;
		}

		public Vector3 vector3
		{
			get
			{
				var xf = (float)x; //(float)Math.Round (x, 7);
				var yf = (float)y; // (float)Math.Round (y, 7);
				return new Vector3(xf, yf);
			}
		}

		public double magnitude => Math.Sqrt(x * x + y * y);

		public static double Distance(Point p1, Point p2) =>
			Math.Sqrt((p2.x - p1.x) * (p2.x - p1.x) + (p2.y - p1.y) * (p2.y - p1.y));

		public override string ToString() => "x:" + x.ToString("F5") + " y:" + y.ToString("F5");

		public bool Equals(Point p0, Point p1) =>
			p0.x - p1.x < PRECISION &&
			p0.x - p1.x > -PRECISION &&
			p0.y - p1.y < PRECISION &&
			p0.y - p1.y > -PRECISION;

		public static bool EqualsBoth(Point p0, Point p1) =>
			p0.x - p1.x < PRECISION &&
			p0.x - p1.x > -PRECISION &&
			p0.y - p1.y < PRECISION &&
			p0.y - p1.y > -PRECISION;

		public override bool Equals(object o)
		{
			if (o is Point)
			{
				var p = (Point)o;
				return p.x == x && p.y == y;
			}
			return false;
		}

		public override int GetHashCode() =>
			//			return (int)(10e7 * Math.Round(x, 7) + 10e9 * Math.Round (y, 7));
			(int)(10e7 * (float)x + 10e9 * (float)y);

		public int GetHashCode(Point p) =>
			//			return (int)(10e7 * Math.Round(p.x, 7) + 10e9 * Math.Round (p.y, 7));
			(int)(10e7 * (float)(p.x + 10e9 * (float)p.y));

		public static bool operator ==(Point p1, Point p2) => p1.x == p2.x && p1.y == p2.y;

		public static bool operator !=(Point p1, Point p2) => p1.x != p2.x || p1.y != p2.y;

		public static Point operator -(Point p1, Point p2) => new(p1.x - p2.x, p1.y - p2.y);

		public static Point operator -(Point p) => new(-p.x, -p.y);

		public static Point operator +(Point p1, Point p2) => new(p1.x + p2.x, p1.y + p2.y);

		public static Point operator *(Point p, double scalar) => new(p.x * scalar, p.y * scalar);

		public static Point operator /(Point p, double scalar) => new(p.x / scalar, p.y / scalar);

		public double sqrMagnitude => x * x + y * y;

		public static Point operator -(Vector2 p1, Point p2) => new(p1.x - p2.x, p1.y - p2.y);

		public static Point Lerp(Point start, Point end, double t)
		{
			var x = start.x + (end.x - start.x) * t;
			var y = start.y + (end.y - start.y) * t;
			return new Point(x, y);
		}

		public void Normalize()
		{
			var d = Math.Sqrt(x * x + y * y);
			x /= d;
			y /= d;
		}

		public Point normalized
		{
			get
			{
				var d = Math.Sqrt(x * x + y * y);
				return new Point(x / d, y / d);
			}
		}

		public Point Offset(double deltax, double deltay) => new(x + deltax, y + deltay);

		public void CropBottom()
		{
			if (y < -0.5)
				y = -0.5;
		}

		public void CropRight()
		{
			if (x > 0.5)
				x = 0.5;
		}
	}
}