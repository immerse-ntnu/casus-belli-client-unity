using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immerse.BfHClient
{
	public class Region : MonoBehaviour
	{
		public static Region BuildRegion(string name)
		{
			var region = new GameObject(name).AddComponent<Region>();

			region.Name = name;

			return region;
		}

		public string Name { get; private set; }
		public bool IsDockable { get; internal set; }
		public bool IsLand { get; internal set; }
		public List<Region> Neighbours { get; internal set; }
	}
}