using System;
using UnityEngine;

namespace Hermannia
{
	public class Map : MonoBehaviour
	{
		private Texture2D _bitmap;
		private Material _material;
		private static readonly int Region = Shader.PropertyToID("_Region");
		private SpriteRenderer _renderer;

		private void Awake()
		{
			_renderer = GetComponent<SpriteRenderer>();
			_material = _renderer.material;
			_bitmap = _material.GetTexture("_Bitmap") as Texture2D;
		}
		private void OnMouseDown()
		{
			var selectedCol = GetSpritePixelColorUnderMousePointer();
			_material.SetColor(Region, selectedCol);
		}

		//Todo: make less obvious steal
		private Color GetSpritePixelColorUnderMousePointer()
		 {
             var cam = Camera.main;
             Vector2 viewportPos = cam.ScreenToViewportPoint(Input.mousePosition);
             var ray = cam.ViewportPointToRay(viewportPos);
             var sprite = _renderer.sprite;
             if(sprite.packed && sprite.packingMode == SpritePackingMode.Tight) // Cannot use textureRect on tightly packed sprites
	             Debug.LogError("SpritePackingMode.Tight atlas packing is not supported!");
             
             var plane = new Plane(transform.forward, transform.position);
             if (!plane.Raycast(ray, out var rayIntersectDist))
	             return Color.black; // no intersection
             
             var spritePos = _renderer.worldToLocalMatrix.MultiplyPoint3x4(ray.origin + (ray.direction * rayIntersectDist));
             var textureRect = sprite.textureRect;
             var pixelsPerUnit = sprite.pixelsPerUnit;
             var halfRealTexWidth = _bitmap.width * 0.5f; // use the real texture width here because center is based on this -- probably won't work right for atlases
             var halfRealTexHeight = _bitmap.height * 0.5f;
             var texPosX = (int)(spritePos.x * pixelsPerUnit + halfRealTexWidth);
             var texPosY = (int)(spritePos.y * pixelsPerUnit + halfRealTexHeight);
             
             if(texPosX < 0 || texPosX < textureRect.x || texPosX >= Mathf.FloorToInt(textureRect.xMax) || 
                texPosY < 0 || texPosY < textureRect.y || texPosY >= Mathf.FloorToInt(textureRect.yMax)) 
					return Color.black; // out of bounds
             return _bitmap.GetPixel(texPosX, texPosY);
         }
	}
}