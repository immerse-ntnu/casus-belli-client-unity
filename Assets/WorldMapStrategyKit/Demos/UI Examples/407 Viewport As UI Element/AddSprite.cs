using UnityEngine;
using WorldMapStrategyKit;

public class AddSprite : MonoBehaviour
{
	public GameObject sprite;

	private void Start()
	{
		var map = WMSK.instance;
		Vector3 pos = map.GetCountry("France").center;
		var go = Instantiate(sprite);
		map.AddMarker2DSprite(go, pos, 0.01f);
	}
}