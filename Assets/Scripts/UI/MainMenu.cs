using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MonoBehaviour
{
    public GameObject optionsMenuPanel;
    public GameObject creditsPanel;
    public RectTransform creditsContent;

    private void Start()
    {
        optionsMenuPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    public void PlayGame()
    {
        SceneManager.LoadScene("FarmHouse");
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

    public void ShowCredits()
    {
        creditsContent.anchoredPosition = new Vector2(creditsContent.anchoredPosition.x, 0);
        creditsPanel.SetActive(true);
    }

    public void HideCredits()
    {
        creditsPanel.SetActive(false);
    }
}