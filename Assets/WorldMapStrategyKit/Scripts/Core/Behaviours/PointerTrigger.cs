using UnityEngine;
using System.Collections;

namespace WorldMapStrategyKit
{
	public class PointerTrigger : MonoBehaviour
	{
		public WMSK map;

		private void Awake()
		{
			if (GetComponent<TerrainCollider>())
				return;
			if (GetComponent<MeshCollider>() == null)
				gameObject.AddComponent<MeshCollider>();
		}

		private void OnMouseEnter()
		{
			if (map == null)
				return;
			map.OnMouseEnter();
		}

		private void OnMouseExit()
		{
			if (map == null)
				return;
			map.OnMouseExit();
		}

		// Support for NGUI
		private void OnHover(bool isOver)
		{
			if (map == null)
				return;
			if (isOver)
				map.OnMouseEnter();
			else
				map.OnMouseExit();
		}

		private void OnPress(bool isPressed)
		{
			if (map == null)
				return;

			if (isPressed)
				map.DoOnMouseClick();
			else
				map.DoOnMouseRelease();
		}
	}
}