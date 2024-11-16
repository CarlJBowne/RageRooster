using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject optionsMenuUI;
    public EventSystem eventSystem;
    private bool isPaused = false;
    private PlayerActions playerAction;

    private void OnEnable()
    {
        playerAction = new PlayerActions();
        playerAction.UI.PauseGame.performed += OnPause;
        playerAction.UI.Enable();
    }

    private void OnDisable()
    {
        playerAction.UI.PauseGame.performed -= OnPause;
        playerAction.UI.Disable();
    }

    private void Start()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    private void OnPause(InputAction.CallbackContext context)
    {
        if (optionsMenuUI.activeSelf)
        {
            CloseOptions();
        }
        else if (isPaused)
        {
            Resume();
        }
        else
        {
            Pause();
        }
    }

    public void Resume()
    {
        if (AudioManager.Get() != null && FMODEvents.instance != null)
        {
            AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        }
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        eventSystem.enabled = false;
        isPaused = false;
    }

    void Pause()
    {
        if (AudioManager.Get() != null && FMODEvents.instance != null)
        {
            AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        }
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        eventSystem.enabled = true;
        isPaused = true;
    }

    public void QuitGame()
    {
        if (AudioManager.Get() != null && FMODEvents.instance != null)
        {
            AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        }
        Application.Quit();
    }

    public void OpenOptions()
    {
        if (AudioManager.Get() != null && FMODEvents.instance != null)
        {
            AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        }
        optionsMenuUI.SetActive(true);
        pauseMenuUI.SetActive(false);
        eventSystem.enabled = true;
    }

    public void CloseOptions()
    {
        if (AudioManager.Get() != null && FMODEvents.instance != null)
        {
            AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        }
        optionsMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
        eventSystem.enabled = true;
    }

    public void ReturnToMainMenu()
    {
        if (AudioManager.Get() != null && FMODEvents.instance != null)
        {
            AudioManager.Get().PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
        eventSystem.enabled = false;
    }
}