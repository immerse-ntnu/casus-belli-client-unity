using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;
using UnityEngine;

namespace Immerse.BfHClient
{
	public class RegionLookUp
	{
		private Dictionary<Color32, Region> _regions;
		public Region GetRegionFromColor(Color32 color) => 
			!_regions.TryGetValue(color, out var region) ? null : region;

		public RegionLookUp(string regionData)
		{
			var deserializedRegions = GetDeserializedDictionary(regionData);
			_regions = RegionsFromDeserializedRegions(deserializedRegions);
		}

		public Region GetRegionFromName(string name)
		{
			foreach (var pair in _regions)
				if (pair.Value.Name == name)
					return pair.Value;
			return null;
		}

		#region RegionsSetup

		private static Dictionary<Color32, SerializableRegion> GetDeserializedDictionary(string json)
		{
			var stringRegions = JsonConvert.DeserializeObject<Dictionary<string, SerializableRegion>>(json);
			var serializedRegions = new Dictionary<Color32, SerializableRegion>();
			foreach (var pair in stringRegions!)
				serializedRegions[GetColorFromString(pair.Key)] = pair.Value;
			return serializedRegions;
		}

		private static Color GetColorFromString(string colorString)
		{
			var strings = colorString.Split(',');
			byte.TryParse(strings[0], out var r);
			byte.TryParse(strings[1], out var g);
			byte.TryParse(strings[2], out var b);
			return new Color32(r, g, b, 255);
		}

		private static Dictionary<Color32, Region> RegionsFromDeserializedRegions(Dictionary<Color32, SerializableRegion> serializedRegions)
		{
			Dictionary<string, Country> countries = new();
			foreach (var region in serializedRegions.Values)
            {
				if (!countries.ContainsKey(region.country))
                {
					countries.Add(region.country, new Country(region.country));
                }
            }
			// loop through all 

			var regionsDictionary = new Dictionary<Color32, Region>();
			var regions = serializedRegions.Select(pair => Region.BuildRegionFrom(pair.Value, countries)).ToArray();
			foreach (var pair in serializedRegions)
			{
				var neighbours = new List<Region>();
				foreach (var neighbour in pair.Value.neighbours)
					neighbours.AddRange(regions.Where(region => region.Name == neighbour));
				var foundRegion = regions.FirstOrDefault(r => r.Name == pair.Value.name);
				foundRegion!.Neighbours = neighbours;
				
				regionsDictionary[pair.Key] = foundRegion;
			}
			return regionsDictionary;
		}

		#endregion
	}
}