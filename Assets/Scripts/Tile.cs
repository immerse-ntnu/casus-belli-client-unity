using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tile : MonoBehaviour
{
    public List<Tile> landNeighbours;
    public List<Tile> seaNeighbours;
    public String provinceCode;

    public bool hasCastle;
    [SerializeField] private bool hasForest;
    [SerializeField] private State state;

    private Image image;

    public TileType tileType;
    public Nation owner;
    public Unit unit;

    public Order order;


    private void Awake()
    {
        image = GetComponent<Image>();

        image.alphaHitTestMinimumThreshold = 0.1f;
        image.color = Color.clear;
    }

    public void OnClick()
    {
        Debug.Log(provinceCode);
    }

    public void SetColor(Color color = default)
    {
        image.color = color == default ? Color.clear : color;
    }
}

public enum TileType
{
    Land,
    Coast,
    Sea
}
