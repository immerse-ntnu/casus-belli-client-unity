using System.Collections;
using System.Collections.Generic;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Immerse.BfHClient
{
    public class ActionButton : MonoBehaviour
    {
        [SerializeField] private GameObject unit;

        [SerializeField] private ActionPopup popup;

        public void SpawnUnit()
        {
            var pos = transform.parent.GetComponent<ActionPopup>().Position;
            var spawnedUnit = Instantiate(unit);
            spawnedUnit.transform.position = pos;
            Debug.Log(unit.name);
        }
    }
}
