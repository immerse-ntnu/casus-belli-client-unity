﻿using System.Collections.Generic;

namespace Immerse.BfHClient
{
	public record Region(string Name)
	{
		public string Name { get; } = Name;
		public List<Region> Neighbours { get; internal set; }
	}
}