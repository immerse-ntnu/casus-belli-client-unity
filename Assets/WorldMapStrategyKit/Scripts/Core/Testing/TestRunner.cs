#if UNITY_EDITOR

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