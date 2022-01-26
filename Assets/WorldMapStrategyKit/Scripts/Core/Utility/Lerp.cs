using UnityEngine;

namespace WorldMapStrategyKit
{
	public static class Lerp
	{
		public static float EaseIn(float t)
		{
			if (t < 0)
				t = 0;
			else if (t > 1)
				t = 1;
			return 1f - Mathf.Cos(t * Mathf.PI * 0.5f);
		}

		public static float EaseOut(float t)
		{
			if (t < 0)
				t = 0;
			else if (t > 1)
				t = 1;
			return Mathf.Sin(t * Mathf.PI * 0.5f);
		}

		public static float Exponential(float t)
		{
			if (t < 0)
				t = 0;
			else if (t > 1)
				t = 1;
			return t * t;
		}

		public static float SmoothStep(float t) => t * t * (3f - 2f * t);

		public static float SmootherStep(float t)
		{
			if (t < 0)
				t = 0;
			else if (t > 1)
				t = 1;
			return t * t * t * (t * (6f * t - 15f) + 10f);
		}
	}
}