using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class MainMenu : MenuSingleton<MainMenu>
{
    public GameObject optionsMenuPanel;
    public GameObject creditsPanel;
    //public RectTransform creditsContent;

    private void Start()
    {
        optionsMenuPanel.SetActive(false);
        creditsPanel.SetActive(false);
    }

    private void PlaySound() => AudioManager.Get().PlayOneShot(FMODEvents.instance.selectionConfirm, this.transform.position);

    public void PlayGame()
    {
        //PlaySound();
        SceneManager.LoadScene("GameplayScene");
        Close();
    }

    public void QuitGame()
    {
        //PlaySound();
        Application.Quit();
    }

    public void OpenOptionsMenu()
    {
        //PlaySound();
        optionsMenuPanel.SetActive(true);
    }

    public void CloseOptionsMenu()
    {
        //PlaySound();
        optionsMenuPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        //PlaySound();
        //creditsContent.anchoredPosition = new Vector2(creditsContent.anchoredPosition.x, 0);
        creditsPanel.SetActive(true);
    }

    public void HideCredits()
    {
        //PlaySound();
        creditsPanel.SetActive(false);
    }
}