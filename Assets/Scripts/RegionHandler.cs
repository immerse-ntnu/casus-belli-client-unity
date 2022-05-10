using System.Collections.Generic;
using Newtonsoft.Json;
using UnityEngine;

namespace Hermannia
{
	public class RegionHandler
	{
		private Dictionary<string, Region> _regions;
		public Region GetRegionFromColor(Color32 color) => _regions["(" + color.r + "," + color.g + "," + color.b + ")"];

		public RegionHandler(TextAsset asset) => 
			_regions = JsonConvert.DeserializeObject<Dictionary<string, Region>>(asset.text);

		public (Color color, Region region) GetRegionFromName(string name)
		{
			foreach (var pair in _regions)
			{
				if (pair.Value.name == name)
				{
					return (GetColorFromString(pair.Key), pair.Value);
				}
			}
			return (Color.black, null);
		}

		private static Color GetColorFromString(string colorString)
		{
			var strings = colorString.Split(',');
			byte.TryParse(strings[0], out var r);
			byte.TryParse(strings[1], out var g);
			byte.TryParse(strings[2], out var b);
			return new Color32(r, g, b, 255);
		}
	}
}