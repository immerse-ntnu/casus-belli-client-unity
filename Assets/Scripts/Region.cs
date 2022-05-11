using System.Collections.Generic;

namespace Hermannia
{
	public class Region
	{
		public string Name { get; private set; }
		public List<Region> Neighbours { get; internal set; }

		public Region(string name)
		{
			Name = name;
		}
	}
}