using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Immerse.BfHClient
{
    public class ActionPopup : MonoBehaviour
    {
        public Vector2 Position
        {
            get => transform.position;
            set => transform.position = value;
        }
        [SerializeField] private List<Transform> popups;
    
        void Awake()
        {
            foreach (Transform transform in GetComponentsInChildren<Transform>())
            {
                if (transform == this.transform) continue;
                popups.Add(transform);
            }
        }

        public void SetActions(params string[] actions)
        {
            foreach (Transform transform in popups)
            {
                // TODO do shit
                bool visible = actions.Contains(transform.gameObject.name);
                transform.gameObject.SetActive(visible);
            }
        }
    }
}
