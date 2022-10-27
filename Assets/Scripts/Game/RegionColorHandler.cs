using UnityEngine;

namespace Immerse.BfhClient
{
	public class RegionColorHandler
	{

		private readonly SpriteRenderer _renderer;
		private readonly Transform _transform;
		private readonly Texture2D _bitmap;
		private readonly float _pixelsPerUnit;
		private Rect _textureRect;
		public RegionColorHandler(SpriteRenderer renderer)
		{
			_renderer = renderer;
			var material = renderer.material;
			_bitmap = material.GetTexture("_Bitmap") as Texture2D;
			var sprite = renderer.sprite;
			_textureRect = sprite.textureRect;
			_pixelsPerUnit = sprite.pixelsPerUnit;
			_transform = renderer.transform;
		}

		public Color GetSpritePixelColorUnderMousePointer()
		{
			if (!IsMousePosInLocalMatrix(out var matrixPos) ||
			    !WithinBoundsTexturePos(matrixPos, out var texPosX, out var texPosY))
				return Color.black;
			return _bitmap.GetPixel(texPosX, texPosY);
		}

		private bool IsMousePosInLocalMatrix(out Vector3 matrixPos)
		{
			var ray = Camera.main!.ScreenPointToRay(Input.mousePosition);
			var plane = new Plane(_transform.forward, _transform.position);
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