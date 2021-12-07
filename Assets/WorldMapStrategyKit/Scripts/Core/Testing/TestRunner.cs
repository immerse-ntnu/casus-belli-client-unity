#if UNITY_EDITOR

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace WorldMapStrategyKit
{
	public class TestRunner : MonoBehaviour
	{
		// Use this for initialization
		private void Start()
		{
			var map = WMSK.instance;
			map.ExecuteTests();
		}
	}
}

#endif