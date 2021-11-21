using System;
using System.Collections.Generic;
using UnityEngine;

public struct Order
{
    private Tile from;
    private Tile to;
    private Tile toto;
    private int unitType;
    private OrderType orderType;

    private static readonly Dictionary<string, int> validUnitInputs = new Dictionary<string, int>() {
        {"S",           0 },
        {"Skip",        0 },
        {"Ship",        0 },
        {"F",           1 },
        {"Footman",     1 },
        {"Fotfolk",     1 },
        {"R",           2 },
        {"Rytter",      2 },
        {"Rider" ,      2 },
        {"K",           3 },
        {"Katapult",    3 },
        {"C",           3 },
        {"Catapult",    3 },
    };

    public static bool IsValidOrderString(string input, out OrderType ordertype)
    {
        ordertype = OrderType.hold;

        return !Game.game.isWinter
               && IsValidSeasonOrderString(input, out ordertype)
               || Game.game.isWinter
               && IsValidWinterOrderString(input, out ordertype);
    }



    private static bool IsValidSeasonOrderString(string input, out OrderType ordertype)
    {
        var orders = input.ToUpper().Split(' ');

        ordertype = OrderType.hold;

        if (!Game.provinceCodes.Contains(orders[0]))
        {
            return false;
        }

        if (orders.Length == 2)
        {
            if (Game.provinceCodes.Contains(orders[1]))
            {
                ordertype = OrderType.flytt;
                return true;
            }

            else if (orders[1] == "H")
            {
                ordertype = OrderType.hold;
                return true;
            }

            else if(orders[1] == "T" && Game.game.GetTileFromCode(orders[0]).tileType == TileType.Sea)
            {
                ordertype = OrderType.transport;
                return true;
            }

            else if(orders[1] == "B" && Game.game.GetTileFromCode(orders[0]).hasCastle)
            {
                ordertype = OrderType.beleire;
                return true;
            }
        }

        else if (orders.Length == 3 && Game.provinceCodes.Contains(orders[2]))
        {
            if (orders[1] == "S")
            {
                ordertype = OrderType.støtt;
                return true;
            }
            else if (Game.provinceCodes.Contains(orders[1]))
            {
                ordertype = OrderType.hestflytt;
                return true;
            }
        }

        return false;
    }

    private static bool IsValidWinterOrderString(string input, out OrderType ordertype)
    {
        var orders = input.ToUpper().Split(' ');

        ordertype = OrderType.hold;

        if (orders.Length != 2)
        {
            return false;
        }

        if (!Game.provinceCodes.Contains(orders[0]))
        {
            return false;
        }

        else if (Game.provinceCodes.Contains(orders[1]) && Game.game.GetTileFromCode(orders[1]))
        {
            ordertype = OrderType.flytt;
            return true;
        }

        else if (validUnitInputs.ContainsKey(orders[1]))
        {
            ordertype = OrderType.muster;
            return true;
        }

        return false;
    }
}

public enum OrderType
{
    hold,
    flytt,
    hestflytt,
    støtt,
    transport,
    beleire,
    muster
}
