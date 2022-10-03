using System;
using System.Collections.Generic;
using UnityEngine;

namespace Immerse.BfHClient
{
	[Serializable]
	public class SerializableRegion
	{
		public List<string> neighbours;
		public string name;
		public bool isDockable;
		public bool isLand;
		public Vector2 position;
	}
}