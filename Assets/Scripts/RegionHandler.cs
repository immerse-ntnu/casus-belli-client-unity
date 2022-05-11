using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Hermannia
{
	public class RegionHandler
	{
		private readonly Dictionary<Color32, Region> _regions;
		public Region GetRegionFromColor(Color32 color) => _regions[color];

		//Todo clean this up
		public RegionHandler(TextAsset asset)
		{
			var stringRegions = JsonConvert.DeserializeObject<Dictionary<string, SerializableRegion>>(asset.text);
			var serializedRegions = new Dictionary<Color32, SerializableRegion>();
			foreach (var pair in stringRegions!)
				serializedRegions[GetColorFromString(pair.Key)] = pair.Value;

			_regions = new Dictionary<Color32, Region>();
			var regions = serializedRegions.Select(pair => new Region(pair.Value.name)).ToList();
			foreach (var pair in serializedRegions)
			{
				var neighbours = new List<Region>();
				foreach (var neighbour in pair.Value.neighbours)
					neighbours.AddRange(regions.Where(region => region.Name == neighbour));
				var foundRegion = regions.Find(r => r.Name == pair.Value.name);
				foundRegion.Neighbours = neighbours;
				_regions[pair.Key] = foundRegion;
			}
		}

		public (Color color, Region region) GetRegionFromName(string name)
		{
			foreach (var pair in _regions)
				if (pair.Value.Name == name)
					return (pair.Key, pair.Value);
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