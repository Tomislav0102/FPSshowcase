using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LevelManager : MonoBehaviour
{

    private void Awake()
    {
        GameManager.Instance.levelManager = this;
    }
    private void Start()
    {
        SceneManager.SetActiveScene(gameObject.scene);
        
    }
}
