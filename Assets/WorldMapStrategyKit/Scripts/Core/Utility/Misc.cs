using UnityEngine;
using System.Collections;
using System.Globalization;

namespace WorldMapStrategyKit
{
	public static class Misc
	{
		public static Vector4 Vector4back = new(0f, 0f, -1f, 0f);
		public static Vector4 Vector4one = new(1f, 1f, 1f, 1f);

		public static Vector3 Vector3one = new(1f, 1f, 1f);
		public static Vector3 Vector3zero = new(0f, 0f, 0f);
		public static Vector3 Vector3forward = new(0f, 0f, 1f);
		public static Vector3 Vector3back = new(0f, 0f, -1f);
		public static Vector3 Vector3right = new(1f, 0f, 0f);
		public static Vector3 Vector3left = new(-1f, 0f, 0f);
		public static Vector3 Vector3up = new(0f, 1f, 0f);
		public static Vector3 Vector3down = new(0f, -1f, 0f);
		public static Vector3 Vector3max = Vector3.one * float.MaxValue;
		public static Vector3 Vector3min = Vector3.one * float.MinValue;

		public static Vector2 Vector2one = new(1f, 1f);
		public static Vector2 Vector2zero = new(0f, 0f);
		public static Vector2 Vector2left = new(-1f, 0f);
		public static Vector2 Vector2right = new(1f, 0f);
		public static Vector2 Vector2down = new(0f, -1f);
		public static Vector2 Vector2up = new(0f, 1f);
		public static Vector2 Vector2max = new(float.MaxValue, float.MaxValue);
		public static Vector2 Vector2min = new(float.MinValue, float.MinValue);

		public static Vector2 ViewportCenter = new(0.5f, 0.5f);

		public static Color ColorWhite = new(1, 1, 1, 1);
		public static Color ColorBlack = new(0, 0, 0, 1);
		public static Color ColorClear = new(0, 0, 0, 0);

		public static Quaternion QuaternionZero = Quaternion.Euler(Vector3zero);
		public static Quaternion QuaternionX90 = Quaternion.Euler(90, 0, 0);

		public static float DistanceToLine(this Vector2 p, Vector2 a, Vector2 b)
		{
			var ab = b - a;
			var l2 = ab.sqrMagnitude;
			var u = Vector2.Dot(p - a, ab) / l2;
			var proj = a + u * ab;
			return Vector2.Distance(proj, p);
		}

		public static float DistanceToLineSqr(this Vector2 p, Vector2 a, Vector2 b)
		{
			var ab = b - a;
			var l2 = ab.sqrMagnitude;
			var u = Vector2.Dot(p - a, ab) / l2;
			var proj = a + u * ab;
			return Vector2.Dot(proj, p);
		}

		public static float DistanceToSegment(this Vector2 p, Vector2 a, Vector2 b)
		{
			var ab = b - a;
			var l2 = ab.sqrMagnitude;
			var u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
			var proj = a + u * ab;
			return Vector2.Distance(proj, p);
		}

		public static float DistanceToSegmentSqr(this Vector2 p, Vector2 a, Vector2 b)
		{
			var ab = b - a;
			var l2 = ab.sqrMagnitude;
			var u = Mathf.Clamp01(Vector2.Dot(p - a, ab) / l2);
			var proj = a + u * ab;
			return Vector2.Dot(proj, p);
		}

		public static CultureInfo InvariantCulture = CultureInfo.InvariantCulture;
	}
}