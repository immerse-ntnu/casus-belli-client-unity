using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{
    private static GameManager _Instance;

    [SerializeField] private List<Tile> tiles;
    [SerializeField] private InputField orderInputField;
    [SerializeField] private ConsoleUI console;
    [SerializeField] private bool isWinter;

    public static readonly List<string> ProvinceCodes = new();

    public static bool IsWinter => _Instance.isWinter;

    private void Update()
    {
        if (!Input.GetKeyDown(KeyCode.C))
            return;
        
        console.Toggle();
    }

    private void Awake()
    {
        _Instance = this;

        foreach (var tile in tiles)
            ProvinceCodes.Add(tile.ProvinceCode);
    }

    public void OnInputChange()
    {
        orderInputField.image.color = Order.IsValidOrderString(orderInputField.text) ? 
            Color.white : Color.red;
    }

    public static Tile GetTileFromCode(string code)
    {
        foreach (var tile in _Instance.tiles)
            if (tile.ProvinceCode == code)
                return tile;
        
        Debug.Log("Found no tile with code: " + code);

        return null;
    }

    public static void FlipSeasons()
    {
        _Instance.isWinter = !_Instance.isWinter;
    }
}

// public enum Nation
// {
//     Local,
//     Black,
//     White,
//     Yellow,
//     Red,
//     Green
// }
//
// public static class Extensions
// {
//     public static Color GetNationColor(this Nation nation)
//     {
//         return nation switch
//         {
//             Nation.Local => Color.clear,
//             Nation.Black => new Color(0.2f, 0.2f, 0.2f, 1f),
//             Nation.White => Color.white,
//             Nation.Yellow => Color.yellow,
//             Nation.Red => Color.red,
//             Nation.Green => Color.green,
//             _ => Color.clear,
//         };
//     }
// }
