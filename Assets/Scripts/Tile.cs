using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    private Image _image;

    private void Awake()
    {
        _image = GetComponent<Image>();

        _image.alphaHitTestMinimumThreshold = 0.1f;
        _image.color = Color.clear;
    }
}