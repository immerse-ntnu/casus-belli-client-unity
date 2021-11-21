using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Unit : MonoBehaviour
{
    public Tile tile;

    [SerializeField] private GameObject prefab;

    public abstract List<Tile> GetPossibleOrders(); 

    private void UpdateColor()
    {
        var image = GetComponent<Image>();
        image.color = Game.game.GetNationColor(tile.owner);
    }

    public void Move(Tile newTile)
    {
        tile.unit = null;
        newTile.unit = this;
        tile = newTile;

        transform.SetParent(newTile.gameObject.transform);
    }

    public void SpawnUnit(Tile tile)
    {
        GameObject spawnedUnit = Instantiate(prefab, tile.gameObject.transform.position, Quaternion.identity);
        spawnedUnit.transform.SetParent(tile.gameObject.transform);

        tile.unit = this;
        this.tile = tile;
        UpdateColor();
    }
}
