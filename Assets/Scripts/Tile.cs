using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    [SerializeField]private string provinceCode;

    private Image _image;

    public string ProvinceCode => provinceCode;

    private void Awake()
    {
        _image = GetComponent<Image>();

        _image.alphaHitTestMinimumThreshold = 0.1f;
        _image.color = Color.clear;
    }

    public void OnClick()
    {
        Debug.Log(ProvinceCode);
    }

    public void SetColor(Color color = default)
    {
        _image.color = color;
    }
}