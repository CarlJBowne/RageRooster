using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NewBehaviourScript : MonoBehaviour
{
    // Loads the Chicken Coop scene
    public void LoadChickenCoop()
    {
        SceneManager.LoadScene("ChickenCoop");
    }

    // Loads the FarmHouse scene
    public void LoadFarmHouse()
    {
        SceneManager.LoadScene("FarmHouse");
    }

    // Quits the game
    public void QuitGame()
    {
        Application.Quit();
    }
}