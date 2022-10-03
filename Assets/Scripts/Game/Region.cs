using System.Collections.Generic;
using UnityEngine;

namespace Immerse.BfHClient
{
	public class Region : MonoBehaviour
	{
		public static Region BuildRegionFrom(SerializableRegion jsonRegion)
		{
			var region = new GameObject(jsonRegion.name).AddComponent<Region>();
			region.Name = jsonRegion.name;
			region.IsLand = jsonRegion.isLand;
			region.IsDockable = jsonRegion.isDockable;
			region.transform.position = jsonRegion.position;
			return region;
		}

		public string Name { get; private set; }
		public bool IsDockable { get; private set; }
		public bool IsLand { get; private set; }
		public List<Region> Neighbours { get; internal set; }
	}
}