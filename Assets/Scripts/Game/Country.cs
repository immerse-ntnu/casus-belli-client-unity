using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immerse.BfhClient.Game
{
    [System.Serializable]
    public class Country
    {
        public string Name { get; private set; }

        public Country(string name)
        {
            Name = name;
        }
    }
}
