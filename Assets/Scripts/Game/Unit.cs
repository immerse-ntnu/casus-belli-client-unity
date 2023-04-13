using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immerse.BfhClient.Game
{
    public class Unit : MonoBehaviour
    {
        [field: SerializeField] public string Name { get; private set; } = "Catapult";
    }
}
