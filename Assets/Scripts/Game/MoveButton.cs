using System;
using UnityEngine;

namespace Immerse.BfhClient
{
    public class MoveButton : MonoBehaviour
    {
        public static event Action<MoveAction> Clicked; 
        public enum MoveAction
        {
            Attack,
            Assist
        }

        [SerializeField] private MoveAction action;

        public void Click()
        {
            Clicked?.Invoke(action);
        }
    }
}