using System.Collections.Generic;
using UnityEngine;

public class State : MonoBehaviour
{
    private List<Tile> _tiles;

    private void Awake()
    {
        GetComponentsInChildren(_tiles);
    }
}