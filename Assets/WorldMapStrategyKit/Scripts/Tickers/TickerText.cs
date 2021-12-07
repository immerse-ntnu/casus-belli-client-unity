using UnityEngine;
using System;
using System.Collections;

namespace WorldMapStrategyKit
{
	[Serializable]
	public class TickerText : ICloneable
	{
		/// <summary>
		/// On which ticker line should the text be put (0..NUM_TICKERS)
		/// </summary>
		public int tickerLine;

		public string text = "Ticker";

		/// <summary>
		/// Fade in/out duration in seconds. Set it to zero to disable fade effect.
		/// </summary>
		public float fadeDuration = 0.5f;

		/// <summary>
		/// If set to 0, text will last for ever or until it finishes scrolling.
		/// </summary>
		public float duration = 5.0f;

		[SerializeField] private float
			_horizontalOffset;

		/// <summary>
		/// Starting position (-0.5..0.5).
		/// Only used if scrollSpeed is zero, otherwise it will be ignored and new texts will automatically enter from the corresponding initial edge.
		/// </summary>
		public float horizontalOffset
		{
			get => _horizontalOffset;
			set
			{
				if (_horizontalOffset != value)
				{
					_horizontalOffset = value;
					if (gameObject != null)
						gameObject.transform.localPosition = new Vector3(_horizontalOffset,
							gameObject.transform.localPosition.y, gameObject.transform.localPosition.z);
				}
			}
		}

		/// <summary>
		/// Optional scaling factor for text, useful if using different fonts.
		/// </summary>
		public float textScale = 1.0f;

		/// <summary>
		/// Set it to 0 to disable blinking effect.
		/// </summary>
		public float blinkInterval = 0;

		/// <summary>
		/// The blink repetitions. A 0 will blink indefinitely (as long as blinkInterval>0)
		/// </summary>
		public int blinkRepetitions = 0;

		[SerializeField] private Color
			_textColor = Color.white;

		public Color textColor
		{
			get => _textColor;
			set
			{
				if (_textColor != value)
				{
					_textColor = value;
					if (gameObject != null)
						gameObject.GetComponent<TextMesh>().color = _textColor;
				}
			}
		}

		public bool drawTextShadow = true;

		[SerializeField] private Color
			_shadowColor = Color.black;

		public Color shadowColor
		{
			get => _shadowColor;
			set
			{
				if (_shadowColor != value)
				{
					_shadowColor = value;
					if (gameObject != null)
						gameObject.transform.Find("shadow").GetComponent<TextMesh>().color = _shadowColor;
				}
			}
		}

		[SerializeField] private Font
			_font;

		/// <summary>
		/// The font for the ticket text (null will use the default, which is Lato and is provided in Resources/Font)
		/// </summary>
		public Font font
		{
			get => _font;
			set
			{
				if (_font != value)
				{
					_font = value;
					if (_font != null)
					{
						var fontMaterial =
							UnityEngine.Object.Instantiate(
								Resources.Load<Material>(
									"WMSK/Materials/Font")); // this material is linked to a shader that has into account zbuffer
						fontMaterial.mainTexture = _font.material.mainTexture;
						//fontMaterial.hideFlags = HideFlags.DontSave;
						fontMaterial.renderQueue += 5;
						_font.material = fontMaterial;
						_shadowMaterial = UnityEngine.Object.Instantiate(_font.material);
						//_shadowMaterial.hideFlags = HideFlags.DontSave;
						_shadowMaterial.renderQueue--;
					}
					else
						_shadowMaterial = null;
				}
			}
		}

		/// <summary>
		/// Text anchor
		/// </summary>
		public TextAnchor anchor = TextAnchor.MiddleCenter;

		[SerializeField] private Material
			_shadowMaterial;

		/// <summary>
		/// The shadow font material. It null, it will create it automatically.
		/// </summary>
		public Material shadowMaterial => _shadowMaterial;

		/// <summary>
		/// Reference to the TextMesh object once the ticker has been created on the scene
		/// </summary>
		[NonSerialized] public GameObject
			gameObject;

		/// <summary>
		/// The size of the text mesh once created.
		/// </summary>
		[NonSerialized] public Vector3
			textMeshSize;

		public TickerText() { }

		public TickerText(int tickerLine, string text)
		{
			this.tickerLine = tickerLine;
			this.text = text;
		}

		public object Clone()
		{
			var clone = new TickerText();
			clone.blinkInterval = blinkInterval;
			clone.blinkRepetitions = blinkRepetitions;
			clone.drawTextShadow = drawTextShadow;
			clone.duration = duration;
			clone.fadeDuration = fadeDuration;
			clone.font = font;
			clone.horizontalOffset = horizontalOffset;
			clone.shadowColor = shadowColor;
			clone.text = text;
			clone.textColor = textColor;
			clone.tickerLine = tickerLine;
			clone.anchor = anchor;
			return clone;
		}
	}
}