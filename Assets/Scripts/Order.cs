using System.Collections.Generic;

public struct Order
{
    private Tile _from;
    private Tile _to;
    private Tile _toto;
    private int _unitType;
    private OrderType _orderType;

    private static readonly Dictionary<string, int> ValidUnitInputs = new() {
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
            return false;

        switch (orders.Length)
        {
            case 2 when Game.provinceCodes.Contains(orders[1]):
                ordertype = OrderType.flytt;
                return true;
            case 2 when orders[1] == "H":
                ordertype = OrderType.hold;
                return true;
            case 2 when orders[1] == "T" && Game.game.GetTileFromCode(orders[0]).tileType == TileType.Sea:
                ordertype = OrderType.transport;
                return true;
            case 2 when orders[1] == "B" && Game.game.GetTileFromCode(orders[0]).hasCastle:
                ordertype = OrderType.beleire;
                return true;
            case 3 when Game.provinceCodes.Contains(orders[2]):
            {
                if (orders[1] == "S")
                {
                    ordertype = OrderType.støtt;
                    return true;
                }
                if (Game.provinceCodes.Contains(orders[1]))
                {
                    ordertype = OrderType.hestflytt;
                    return true;
                }
                break;
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

        if (Game.provinceCodes.Contains(orders[1]) && Game.game.GetTileFromCode(orders[1]))
        {
            ordertype = OrderType.flytt;
            return true;
        }
        if (ValidUnitInputs.ContainsKey(orders[1]))
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
