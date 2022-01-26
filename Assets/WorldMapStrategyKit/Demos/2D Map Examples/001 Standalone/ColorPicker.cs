using UnityEngine;

// relies on: http://forum.unity3d.com/threads/12031-create-random-colors?p=84625&viewfull=1#post84625
namespace WorldMapStrategyKit
{
	public class ColorPicker : MonoBehaviour
	{
		// the solid texture which everything is compared against
		public Texture2D colorPicker;

		// the picker being displayed
		private Texture2D displayPicker;

		// the color that has been chosen
		public Color setColor;
		private Color lastSetColor;
		public bool useDefinedSize;
		public int textureWidth = 360;
		public int textureHeight = 120;
		private float saturationSlider;
		private Texture2D saturationTexture;
		private Texture2D styleTexture;
		public bool showPicker;

		private void Awake()
		{
			// if a default color picker texture hasn't been assigned, make one dynamically
			if (!colorPicker)
			{
				colorPicker = new Texture2D(textureWidth, textureHeight, TextureFormat.ARGB32, false);
				ColorHSV hsvColor;
				for (var i = 0; i < textureWidth; i++)
				{
					for (var j = 0; j < textureHeight; j++)
					{
						hsvColor = new ColorHSV(i, 1.0f / j * textureHeight, 1.0f);
						colorPicker.SetPixel(i, j, hsvColor.ToColor());
					}
				}
			}
			colorPicker.Apply();
			displayPicker = colorPicker;

			if (!useDefinedSize)
			{
				textureWidth = colorPicker.width;
				textureHeight = colorPicker.height;
			}

			var v = 0.0F;
			var diff = 1.0f / textureHeight;
			saturationTexture = new Texture2D(20, textureHeight);
			for (var i = 0; i < saturationTexture.width; i++)
			{
				for (var j = 0; j < saturationTexture.height; j++)
				{
					saturationTexture.SetPixel(i, j, new Color(v, v, v));
					v += diff;
				}
				v = 0.0F;
			}
			saturationTexture.Apply();

			// small color picker box texture
			styleTexture = new Texture2D(1, 1);
			styleTexture.SetPixel(0, 0, setColor);
		}

		private void OnGUI()
		{
			if (!showPicker)
				return;

			GUIResizer.AutoResize();

			var positionLeft = GUIResizer.authoredScreenWidth / 2 - textureWidth / 2;
			var positionTop = GUIResizer.authoredScreenHeight / 2 - textureHeight / 2;

			GUI.Box(new Rect(positionLeft - 3, positionTop - 3, textureWidth + 60, textureHeight + 60),
				"");

			if (GUI.RepeatButton(new Rect(positionLeft, positionTop, textureWidth, textureHeight),
				displayPicker))
			{
				var a = (int)Input.mousePosition.x;
				var b = Screen.height - (int)Input.mousePosition.y;

				setColor = displayPicker.GetPixel(a - positionLeft, -(b - positionTop));
				lastSetColor = setColor;
			}

			saturationSlider =
				GUI.VerticalSlider(
					new Rect(positionLeft + textureWidth + 3, positionTop, 10, textureHeight),
					saturationSlider, 1, -1);
			setColor = lastSetColor + new Color(saturationSlider, saturationSlider, saturationSlider);
			GUI.Box(new Rect(positionLeft + textureWidth + 20, positionTop, 20, textureHeight),
				saturationTexture);

			if (GUI.Button(
				new Rect(positionLeft + textureWidth - 60, positionTop + textureHeight + 10, 60, 25),
				"Close"))
			{
				setColor = styleTexture.GetPixel(0, 0);

				// hide picker
				showPicker = false;
			}

			// color display
			var style = new GUIStyle();
			styleTexture.SetPixel(0, 0, setColor);
			styleTexture.Apply();

			style.normal.background = styleTexture;
			GUI.Box(new Rect(positionLeft + textureWidth + 10, positionTop + textureHeight + 10, 30, 30),
				new GUIContent(""), style);
		}
	}
}