using System.Collections.Generic;
using UnityEngine;

public class State : MonoBehaviour
{
    public List<Tile> tiles;

    public Nation Owner
    {
        get
        {
            Nation nation = tiles[0].owner;

            for (int i = 1; i < tiles.Count; i++)
            {
                if (tiles[i].owner != nation)
                {
                    return Nation.Local;
                }
            }

            return nation;
        }
    }
}