using UnityEngine;
using System;
using System.Collections;

namespace WorldMapStrategyKit
{
	[Serializable]
	public class TickerTextAnimator : MonoBehaviour
	{
		[SerializeField] public TickerText tickerText;

		private float startTime, fadeOutTime;
		private TextMesh tm, tmShadow;
		private bool fadeIn, fadeOut;
		private float alpha, alphaShadow;
		private bool blinking = false;
		private int blinkingCount;
		private float blinkingLapTime;
		private TickerBand tickerBand;
		private float scrollSpeed;
		private WMSK map;

		private void Start()
		{
			tm = GetComponent<TextMesh>();
			if (tickerText == null || tm == null)
				DestroyImmediate(this);

			tickerBand = WMSK.instance.GetComponent<WMSK_Ticker>().tickerBands[tickerText.tickerLine];
			scrollSpeed = tickerBand.scrollSpeed;

			var t = transform.Find("shadow");
			if (t != null)
				tmShadow = t.GetComponent<TextMesh>();
			map = WMSK.GetInstance(transform);
			startTime = map.time;
			fadeIn = tickerText.fadeDuration > 0;
			fadeOut = tickerText.fadeDuration > 0 &&
			          tickerText.duration > 0 &&
			          tickerBand.scrollSpeed == 0;
			if (fadeOut)
				fadeOutTime = tickerText.duration - tickerText.fadeDuration;
			alpha = tm.color.a;
			if (tmShadow != null)
				alphaShadow = tmShadow.color.a;
			blinkingCount = 0;
			blinkingLapTime = startTime;
			blinking = tickerText.blinkInterval > 0;
			Update();
		}

		public void Update()
		{
			if (tickerText == null || tickerBand == null)
			{
				DestroyImmediate(gameObject);
				return;
			}

			var elapsedTime = map.time - startTime;
			// check duration
			if (tickerBand.scrollSpeed == 0 &&
			    tickerText.duration > 0 &&
			    elapsedTime > tickerText.duration)
			{
				Destroy(gameObject);
				return;
			}

			float newAlpha = alpha, newAlphaShadow = alphaShadow;
			// fade
			if (fadeIn)
			{
				var a = Mathf.Clamp01(elapsedTime / tickerText.fadeDuration);
				newAlpha = a * alpha;
				newAlphaShadow = a * alphaShadow;
				if (elapsedTime > tickerText.fadeDuration)
					fadeIn = false;
			}
			if (fadeOut)
				if (elapsedTime >= fadeOutTime)
				{
					var a = 1.0f - Mathf.Clamp01((elapsedTime - fadeOutTime) / tickerText.fadeDuration);
					newAlpha = a * alpha;
					newAlphaShadow = a * alphaShadow;
				}

			// blinking
			if (blinking)
			{
				if (map.time - blinkingLapTime >= tickerText.blinkInterval)
				{
					blinkingLapTime = map.time;
					blinkingCount++;
					if (tickerText.blinkRepetitions > 0 && blinkingCount >= tickerText.blinkRepetitions)
						blinking = false;
				}
				if (blinkingCount % 2 == 0)
					newAlpha = newAlphaShadow = 0;
			}

			if (tm.color.a != newAlpha)
				tm.color = new Color(tm.color.r, tm.color.g, tm.color.b, newAlpha);
			;
			if (tmShadow != null && tmShadow.color.a != newAlphaShadow)
				tmShadow.color = new Color(tmShadow.color.r, tmShadow.color.g, tmShadow.color.b,
					newAlphaShadow);
			;

			// scrolling
			scrollSpeed = tickerBand.scrollSpeed;
			if (scrollSpeed != 0)
			{
				var x = transform.localPosition.x + Time.deltaTime * scrollSpeed;
				// exits overlay?
				var tmw = tickerText.textMeshSize.x / WMSK.mapWidth;
				var max = 0.5f + tmw * 0.5f;
				if (x > max && scrollSpeed > 0 || x < -max && scrollSpeed < 0)
					Destroy(gameObject);
				else
					transform.localPosition =
						new Vector3(x, transform.localPosition.y, transform.localPosition.z);
			}
		}
	}
}