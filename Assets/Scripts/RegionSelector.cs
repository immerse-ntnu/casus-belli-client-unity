using UnityEngine;

namespace Hermannia
{
	public class RegionSelector : MonoBehaviour
	{
		private static readonly int Region = Shader.PropertyToID("_Region");
		private Texture2D _bitmap;
		private Material _material;
		private SpriteRenderer _renderer;
		private Rect _textureRect;
		private float _pixelsPerUnit;

		private void Awake()
		{
			_renderer = GetComponent<SpriteRenderer>();
			_material = _renderer.material;
			_bitmap = _material.GetTexture("_Bitmap") as Texture2D;
			var sprite = _renderer.sprite;
			_textureRect = sprite.textureRect;
			_pixelsPerUnit = sprite.pixelsPerUnit;
			_material.SetColor(Region, Color.white);
		}
		private void OnMouseDown()
		{
			var selectedCol = GetSpritePixelColorUnderMousePointer();
			if (selectedCol == Color.black)
				return;
			_material.SetColor(Region, selectedCol);
		}

		private Color GetSpritePixelColorUnderMousePointer()
		{
			if (!IsMousePosInLocalMatrix(out var matrixPos) ||
			    !WithinBoundsTexturePos(matrixPos, out var texPosX, out var texPosY))
					 return Color.black;
			return _bitmap.GetPixel(texPosX, texPosY);
		}

		private bool IsMousePosInLocalMatrix(out Vector3 matrixPos)
		{
			var ray = Camera.main.ScreenPointToRay(Input.mousePosition);
			var plane = new Plane(transform.forward, transform.position);
			if (!plane.Raycast(ray, out var rayIntersectDist))
			{
				matrixPos = default;
				return false;
			}
			matrixPos = _renderer.worldToLocalMatrix.MultiplyPoint3x4(ray.origin + ray.direction * rayIntersectDist);
			return true;
		}

		private bool WithinBoundsTexturePos(Vector3 matrixPos, out int texPosX, out int texPosY)
		{
			 texPosX = (int)(matrixPos.x * _pixelsPerUnit + _bitmap.width * 0.5f);
			 texPosY = (int)(matrixPos.y * _pixelsPerUnit + _bitmap.height * 0.5f);

			 return texPosX >= 0 && !(texPosX < _textureRect.x) && texPosX < Mathf.FloorToInt(_textureRect.xMax) &&
			        texPosY >= 0 && !(texPosY < _textureRect.y) && texPosY < Mathf.FloorToInt(_textureRect.yMax);
		}
	}
}