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
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        SceneManager.LoadScene("FarmHouse");
    }

    public void QuitGame()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        Application.Quit();
    }

    public void OpenOptionsMenu()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        optionsMenuPanel.SetActive(true);
    }

    public void CloseOptionsMenu()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        optionsMenuPanel.SetActive(false);
    }

    public void ShowCredits()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        creditsContent.anchoredPosition = new Vector2(creditsContent.anchoredPosition.x, 0);
        creditsPanel.SetActive(true);
    }

    public void HideCredits()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        creditsPanel.SetActive(false);
    }
}