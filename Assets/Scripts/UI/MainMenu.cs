using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public string mainMenuMusicName = "MainMenu";
    public GameObject optionsMenuPanel;

    private void Start()
    {
        AudioManager.Instance.PlayMusic(mainMenuMusicName);
        optionsMenuPanel.SetActive(false);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("FarmHouse");
        AudioManager.Instance.PlayMusic("FarmHouse");
    }

    public void QuitGame()
    {
        Application.Quit();
    }

    public void OpenOptionsMenu()
    {
        optionsMenuPanel.SetActive(true);
    }

    public void CloseOptionsMenu()
    {
        optionsMenuPanel.SetActive(false);
    }
}