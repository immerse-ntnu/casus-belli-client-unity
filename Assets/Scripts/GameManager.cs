using UnityEngine;

public class GameManager : MonoBehaviour
{
    private static GameManager _Instance;

    private void Awake()
    {
        _Instance = this;
    }
    
}
