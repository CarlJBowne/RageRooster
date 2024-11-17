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
        AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        SceneManager.LoadScene("FarmHouse");
    }

    public void QuitGame()
    {
        AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        Application.Quit();
    }

    public void OpenOptionsMenu()
    {
        AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        optionsMenuPanel.SetActive(true);
    }

    public void CloseOptionsMenu()
    {
        AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        optionsMenuPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        creditsContent.anchoredPosition = new Vector2(creditsContent.anchoredPosition.x, 0);
        creditsPanel.SetActive(true);
    }

    public void HideCredits()
    {
        AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        creditsPanel.SetActive(false);
    }
}