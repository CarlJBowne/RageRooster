using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenuController : MonoBehaviour
{
    public void MechPrototype()
    {
        SceneManager.LoadScene("TheTestingChamber");    
    }

    public void ArtPrototype()
    {
        SceneManager.LoadScene("ChickenCoop");
    }

	public void MainPrototype()
	{
		SceneManager.LoadScene("MainMenu");
	}
}
