using UnityEngine;

namespace Immerse.BfhClient
{
    [CreateAssetMenu(fileName = "ProvinceName", menuName = "ProvinceInfo/Create Province Info Object")]
    public class ProvinceInfo : ScriptableObject
    {
        [field: SerializeField] public string ProvinceName { get; private set; }
        [field: SerializeField, TextArea] public string ProvinceInfoText { get; private set; }
        
    }
}
