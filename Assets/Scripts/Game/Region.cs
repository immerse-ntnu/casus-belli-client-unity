using System;
using System.Collections.Generic;
using UnityEditor.UIElements;
using UnityEngine;
using UnityEngine.UI;

namespace Immerse.BfhClient
{
	public class Region : MonoBehaviour
	{
		public static Region BuildRegionFrom(SerializableRegion jsonRegion, IDictionary<string, Country> countries)
		{
			var region = new GameObject(jsonRegion.name).AddComponent<Region>();
			region.Name = jsonRegion.name;
			region.IsLand = jsonRegion.isLand;
			region.IsDockable = jsonRegion.isDockable;
			region.transform.position = jsonRegion.position;
			region.Country = countries[jsonRegion.country];
			region.PlayerColor = GetColorFromName(jsonRegion.name);
			return region;
		}

		private void Start()
		{
			var flag = Instantiate(Resources.Load<SpriteRenderer>("flag"), transform);
			flag.color = PlayerColor;
			flag.transform.position = transform.position;
			
			Debug.Log(Name);
		}

		public string Name { get; private set; }
		public bool IsDockable { get; private set; }
		public bool IsLand { get; private set; }
		public Country Country { get; private set; }
		public List<Region> Neighbours { get; internal set; }
		public Color PlayerColor { get; internal set; }


		private static Color GetColorFromName(string name)
		{
			return name switch
			{
				"Monté" or "Morone" => Color.red,
				"Pesth" or "Purth" => Color.yellow,
				"Winde" or "Worp" => Color.green,
				"Dordel" or "Dalom" => Color.white,
				"Erren" or "Emman" => Color.gray,
				_ => new Color(0, 0, 0, 0)
			};
		}
	}
}