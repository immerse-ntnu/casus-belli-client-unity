using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController Instance { get; private set; }
    public enum Scene
    {
        Mainmenu,
        Joinlobby,
        Hermannia,
    }

    private void Awake()
    {
        Instance = this;
    }
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
    public void Load(Scene scene)
    {
        if (scene == Scene.Mainmenu)
        {
            SceneManager.LoadScene("Mainmenu");
        }
        else if (scene == Scene.Joinlobby)
        {
            SceneManager.LoadScene("Joinlobby");
        }
        else if (scene == Scene.Hermannia)
        {
            SceneManager.LoadScene("Hermannia");
        }
    }
}
