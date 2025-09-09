using EditorAttributes;
using FMOD.Studio;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScript : MonoBehaviour
{

    private void Awake()
    {
        Debug.Log("Testing"); 
        var S = new SceneReference("TestRoom1");
    }

}
