using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public void PlayGame()
    {
       SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex + 1);

    }

    public void GoToOptionsMenu()
       {
        SceneManager.LoadScene("Options");
    
       }

  public void GoToAboutMenu()
       {
        SceneManager.LoadScene("About");
    
       }


    public void QuitGame()
    {

        Application.Quit();
    }
}