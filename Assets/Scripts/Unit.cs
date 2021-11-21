using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public abstract class Unit : MonoBehaviour
{
    [SerializeField] private Unit prefab;

    public abstract List<Tile> GetPossibleOrders(); 

    private void UpdateColor(Tile tile)
    {
        var image = GetComponent<Image>();
        image.color = tile.owner.GetNationColor();
    }

    public void Move(Tile newTile)
    {
        transform.SetParent(newTile.transform);
    }

    public void SpawnUnit(Tile tile)
    {
        var spawnedUnit = Instantiate(prefab, tile.transform.position, Quaternion.identity);
        spawnedUnit.transform.SetParent(tile.transform);

        tile.unit = this;
        UpdateColor(tile);
    }
}
