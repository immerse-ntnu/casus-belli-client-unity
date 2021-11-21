using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SeasonChanger : MonoBehaviour
{
    public void OnClick()
    {
        Game.game.isWinter = !Game.game.isWinter;
    }
}
