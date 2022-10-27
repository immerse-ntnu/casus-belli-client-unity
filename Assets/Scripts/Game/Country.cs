using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Immerse.BfhClient
{
    [System.Serializable]
    public class Country
    {
        [SerializeField] public string Name { get; private set; }

        public Country(string name)
        {
            Name = name;
        }
    }
}
