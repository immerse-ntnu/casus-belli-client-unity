using UnityEngine;
using System.Collections;
using System.Collections.Generic;

namespace WorldMapStrategyKit
{
	public interface IFader
	{
		bool isFading { get; set; }
		Material customMaterial { get; set; }
	}
}