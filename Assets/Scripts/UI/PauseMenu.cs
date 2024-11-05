using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

public class PauseMenu : MonoBehaviour
{
    public GameObject pauseMenuUI;
    public GameObject optionsMenuUI;
    private bool isPaused = false;

    private void OnEnable()
    {
        var playerInput = new PlayerInput();
        playerInput.UI.Pause.performed += OnPause;
        playerInput.UI.Enable();
    }

    private void OnDisable()
    {
        var playerInput = new PlayerInput();
        playerInput.UI.Pause.performed -= OnPause;
        playerInput.UI.Disable();
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
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        pauseMenuUI.SetActive(false);
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        isPaused = false;
    }

    void Pause()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        pauseMenuUI.SetActive(true);
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        isPaused = true;
    }

    public void QuitGame()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        Application.Quit();
    }

    public void OpenOptions()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        optionsMenuUI.SetActive(true);
        pauseMenuUI.SetActive(false);
    }

    public void CloseOptions()
    {
        AudioManager.instance.PlayOneShot(FMODEvents.instance.buttonPressed, this.transform.position);
        optionsMenuUI.SetActive(false);
        pauseMenuUI.SetActive(true);
    }
}