using UnityEngine;

namespace WorldMapStrategyKit
{
	[ExecuteInEditMode]
	public class MiniMapTrigger : MonoBehaviour
	{
		private WMSKMiniMap miniMap;

		private void OnEnable()
		{
			if (miniMap == null)
			{
				miniMap = WMSKMiniMap.Show(new Vector4(0, 0, 1, 1));
				miniMap.UIParent = GetComponent<RectTransform>();
			}
		}

		private void OnDisable()
		{
			if (miniMap != null)
			{
				DestroyImmediate(miniMap.gameObject);
				miniMap = null;
			}
		}
	}
}