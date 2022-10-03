using System.Collections.Generic;

namespace Immerse.BfHClient
{
	public record Region(string Name)
	{
		public string Name { get; } = Name;
		public bool IsDockable { get; internal set; }
		public bool IsLand { get; internal set; }
		public List<Region> Neighbours { get; internal set; }
	}
}