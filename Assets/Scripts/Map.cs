using System;
using UnityEngine;

namespace Hermannia
{
	public class Map : MonoBehaviour
	{
		private Texture2D _bitmap;
		private Color _selectedCol;
		private Material _material;
		private static readonly int Region = Shader.PropertyToID("_Region");

		private void Awake()
		{
			_material = GetComponent<SpriteRenderer>().material;
			_bitmap = _material.GetTexture("_Bitmap") as Texture2D;
		}
		private void OnMouseDown()
		{
			print("doesnt work");
			var screenPosition = new Vector2(Input.mousePosition.x,Input.mousePosition.y);
			var worldPosition = Camera.main.ScreenToWorldPoint(screenPosition);
	
			
			var selectedCol = _bitmap.GetPixel(
				(int)(worldPosition.x + _bitmap.width / 2),
				(int)(worldPosition.y + _bitmap.height / 2));
			if (_selectedCol == selectedCol)
				return;
			_selectedCol = selectedCol;

			_material.SetColor(Region, _selectedCol);
		}
	}
}