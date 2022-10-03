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

        public RegionComponent RegionComponent;
        
        [SerializeField] private List<ActionButton> actionButtons;
        
        public void SetActions(params string[] actions)
        {
            foreach (var actionButton in actionButtons)
            {
                var transform = actionButton.transform;
                
                // TODO do shit
                bool visible = actions.Contains(transform.gameObject.name);
                transform.gameObject.SetActive(visible);
            }
        }
    }
}
