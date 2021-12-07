using UnityEngine;
using System.Collections;

namespace WorldMapStrategyKit
{
	public class MarkerBlinker : MonoBehaviour
	{
		public float duration = 4.0f;
		public float speed = 0.25f;
		public bool destroyWhenFinished;
		public float stopBlinkAfter = 0;
		private WMSK map;

		/// <summary>
		/// Adds a blinker to the given marker
		/// </summary>
		/// <param name="marker">Marker.</param>
		/// <param name="duration">Duration.</param>
		/// <param name="speed">Blinking interval.</param>
		/// <param name="stopBlinkAfter">Stop blinking after x seconds (pass 0 to blink for the entire duration).</param>
		/// <param name="destroyWhenFinised">If set to <c>true</c> destroy when finised.</param>
		public static void AddTo(GameObject marker, float duration, float speed, float stopBlinkAfter = 0,
			bool destroyWhenFinised = false)
		{
			var mb = marker.AddComponent<MarkerBlinker>();
			mb.duration = duration;
			mb.speed = speed;
			mb.destroyWhenFinished = destroyWhenFinised;
			mb.stopBlinkAfter = stopBlinkAfter;
		}

		private float startTime, lapTime;
		private Vector3 startingScale;
		private bool phase;

		private void Start()
		{
			map = WMSK.GetInstance(transform);
			startTime = map.time;
			lapTime = startTime - speed;
			startingScale = transform.localScale;
			if (stopBlinkAfter <= 0)
				stopBlinkAfter = float.MaxValue;
		}

		// Update is called once per frame
		private void Update()
		{
			var elapsed = map.time - startTime;
			if (elapsed > duration)
			{
				// Restores material
				transform.localScale = startingScale;
				if (destroyWhenFinished)
					Destroy(gameObject);
				else
					Destroy(this);
				return;
			}
			if (map.time - lapTime > speed)
			{
				lapTime = Time.time;
				phase = !phase;
				if (phase && elapsed < stopBlinkAfter)
					transform.localScale = Misc.Vector3zero;
				else
					transform.localScale = startingScale;
			}
		}
	}
}