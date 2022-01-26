using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public class Province : AdminEntity
	{
		private int[] _neighbours;

		/// <summary>
		/// Custom array of provinces that could be reached from this province. Useful for Province path-finding.
		/// It defaults to natural neighbours of the province but you can modify its contents and add your own potential destinations per province.
		/// </summary>
		public override int[] neighbours
		{
			get
			{
				if (_neighbours == null)
				{
					var cc = 0;
					var nn = new List<Province>();
					if (regions != null)
					{
						regions.ForEach(r =>
						{
							if (r != null && r.neighbours != null)
								r.neighbours.ForEach(n =>
									{
										if (n != null)
										{
											var otherProvince = (Province)n.entity;
											if (!nn.Contains(otherProvince))
												nn.Add(otherProvince);
										}
									}
								);
						});
						cc = nn.Count;
					}
					_neighbours = new int[cc];
					for (var k = 0; k < cc; k++)
						_neighbours[k] = WMSK.instance.GetProvinceIndex(nn[k]);
				}
				return _neighbours;
			}
			set => _neighbours = value;
		}

		#region internal fields

		// Used internally. Don't change fields below.
		public string packedRegions;
		public int countryIndex;

		#endregion

		public Province(string name, int countryIndex, int uniqueId)
		{
			this.name = name;
			this.countryIndex = countryIndex;
			regions = null; // lazy load during runtime due to size of data
			center = Misc.Vector2zero;
			this.uniqueId = uniqueId;
			attrib = new JSONObject();
			mainRegionIndex = -1;
		}

		public Province Clone()
		{
			var p = new Province(name, countryIndex, uniqueId);
			p.countryIndex = countryIndex;
			if (regions != null)
			{
				p.regions = new List<Region>(regions.Count);
				for (var k = 0; k < regions.Count; k++)
					p.regions.Add(regions[k].Clone());
			}
			p.center = center;
			p.mainRegionIndex = mainRegionIndex;
			p.attrib = new JSONObject();
			p.attrib.Absorb(attrib);
			p.regionsRect2D = regionsRect2D;
			return p;
		}
	}
}