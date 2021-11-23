using System.Collections.Generic;

public struct Order
{
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

    public static bool IsValidOrderString(string input) =>
        !GameManager.IsWinter
        && IsValidSeasonOrderString(input)
        || GameManager.IsWinter
        && IsValidWinterOrderString(input);

    private static bool IsValidSeasonOrderString(string input)
    {
        var orders = input.ToUpper().Split(' ');

        if (!GameManager.ProvinceCodes.Contains(orders[0]))
            return false;
        //Todo: Find a better solution
        // switch (orders.Length)
        // {
        //     case 2 when GameManager.ProvinceCodes.Contains(orders[1]):
        //         orderType = OrderType.flytt;
        //         return true;
        //     case 2 when orders[1] == "H":
        //         orderType = OrderType.hold;
        //         return true;
        //     case 2 when orders[1] == "T" && GameManager.GetTileFromCode(orders[0]).tileType == TileType.Sea:
        //         orderType = OrderType.transport;
        //         return true;
        //     case 2 when orders[1] == "B" && GameManager.GetTileFromCode(orders[0]).hasCastle:
        //         orderType = OrderType.beleire;
        //         return true;
        //     case 3 when GameManager.ProvinceCodes.Contains(orders[2]):
        //     {
        //         if (orders[1] == "S")
        //         {
        //             orderType = OrderType.støtt;
        //             return true;
        //         }
        //         if (GameManager.ProvinceCodes.Contains(orders[1]))
        //         {
        //             orderType = OrderType.hestflytt;
        //             return true;
        //         }
        //         break;
        //     }
        // }

        return false;
    }

    private static bool IsValidWinterOrderString(string input)
    {
        var orders = input.ToUpper().Split(' ');

        if (orders.Length != 2)
            return false;
        if (!GameManager.ProvinceCodes.Contains(orders[0]))
            return false;

        if (GameManager.ProvinceCodes.Contains(orders[1]) && GameManager.GetTileFromCode(orders[1]))
            return true;
        if (ValidUnitInputs.ContainsKey(orders[1]))
            return true;

        return false;
    }
}