using UnityEngine;

namespace WorldMapStrategyKit
{
	public interface IFader
	{
		bool isFading { get; set; }
		Material customMaterial { get; set; }
	}
}