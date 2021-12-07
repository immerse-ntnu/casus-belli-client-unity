using UnityEngine;

/**
* A color in HSV space
*/

namespace WorldMapStrategyKit
{
	internal class ColorHSV : object
	{
		private float h;
		private float s;
		private float v;
		private float a;

		/**
    * Construct without alpha (which defaults to 1)
    */
		public ColorHSV(float h, float s, float v)
		{
			this.h = h;
			this.s = s;
			this.v = v;
			a = 1.0f;
		}

		/**
    * Construct with alpha
    */
		public ColorHSV(float h, float s, float v, float a)
		{
			this.h = h;
			this.s = s;
			this.v = v;
			this.a = a;
		}

		/**
    * Create from an RGBA color object
    */
		public ColorHSV(Color color)
		{
			var min = Mathf.Min(Mathf.Min(color.r, color.g), color.b);
			var max = Mathf.Max(Mathf.Max(color.r, color.g), color.b);
			var delta = max - min;
			// value is our max color
			v = max;
			// saturation is percent of max

			if (!Mathf.Approximately(max, 0))
				s = delta / max;
			else
			{
				// all colors are zero, no saturation and hue is undefined
				s = 0;
				h = -1;
				return;
			}

			// grayscale image if min and max are the same
			if (Mathf.Approximately(min, max))
			{
				v = max;
				s = 0;
				h = -1;
				return;
			}
			// hue depends which color is max (this creates a rainbow effect)
			if (color.r == max)
				h = (color.g - color.b) / delta; // between yellow  magenta
			else if (color.g == max)
				h = 2 + (color.b - color.r) / delta; // between cyan  yellow
			else
				h = 4 + (color.r - color.g) / delta; // between magenta  cyan

			// turn hue into 0-360 degrees
			h *= 60;
			if (h < 0)
				h += 360;
		}

		/**
    * Return an RGBA color object
    */
		public Color ToColor()
		{
			// no saturation, we can return the value across the board (grayscale)
			if (s == 0)
				return new Color(this.v, this.v, this.v, a);
			// which chunk of the rainbow are we in?
			var sector = h / 60;
			// split across the decimal (ie 3.87f into 3 and 0.87f)
			int i;
			i = (int)Mathf.Floor(sector);
			var f = sector - i;
			var v = this.v;
			var p = v * (1 - s);
			var q = v * (1 - s * f);
			var t = v * (1 - s * (1 - f));
			// build our rgb color
			var color = new Color(0, 0, 0, a);
			switch (i)
			{
				case 0:
					color.r = v;
					color.g = t;
					color.b = p;
					break;
				case 1:
					color.r = q;
					color.g = v;
					color.b = p;
					break;
				case 2:
					color.r = p;
					color.g = v;
					color.b = t;
					break;
				case 3:
					color.r = p;
					color.g = q;
					color.b = v;
					break;
				case 4:
					color.r = t;
					color.g = p;
					color.b = v;
					break;
				default:
					color.r = v;
					color.g = p;
					color.b = q;
					break;
			}
			return color;
		}

		public static Color GetRandomColor(float h, float s, float v)
		{
			var col = new ColorHSV(h, s, v);
			return col.ToColor();
		}
	}
}