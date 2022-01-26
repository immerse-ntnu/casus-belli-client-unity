using UnityEngine;

namespace WorldMapStrategyKit
{
	public class CityBlinker : MonoBehaviour
	{
		public float duration;
		public Color color1, color2;
		public float speed;
		public Material blinkMaterial;
		private Material oldMaterial;
		private float startTime, lapTime;
		private bool whichColor;
		private WMSK map;

		private void Start()
		{
			oldMaterial = GetComponent<Renderer>().sharedMaterial;
			GenerateMaterial();
			map = WMSK.GetInstance(transform);
			startTime = map.time;
			lapTime = startTime - speed;
		}

		// Update is called once per frame
		private void Update()
		{
			var elapsed = map.time - startTime;
			if (elapsed > duration)
			{
				GetComponent<Renderer>().sharedMaterial = oldMaterial;
				Destroy(this);
				return;
			}
			if (map.time - lapTime > speed)
			{
				lapTime = map.time;
				var mat = GetComponent<Renderer>().sharedMaterial;
				if (mat != blinkMaterial)
					GenerateMaterial();
				whichColor = !whichColor;
				if (whichColor)
					blinkMaterial.color = color1;
				else
					blinkMaterial.color = color2;
			}
		}

		private void GenerateMaterial()
		{
			blinkMaterial = Instantiate(blinkMaterial);
			//blinkMaterial.hideFlags = HideFlags.DontSave;
			GetComponent<Renderer>().sharedMaterial = blinkMaterial;
		}
	}
}