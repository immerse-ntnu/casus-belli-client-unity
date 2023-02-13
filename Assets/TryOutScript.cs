using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;

namespace Immerse.BfhClient
{
    public class TryOutScript : MonoBehaviour
    {
        [SerializeField]private Vector3 myVector3;
        [SerializeField]private Player _player;

        private void Start()
        {
            myVector3.x = _player.Speed;
        }
    }
}
