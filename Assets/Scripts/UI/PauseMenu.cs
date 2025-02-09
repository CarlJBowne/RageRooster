using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenu : MenuSingleton<PauseMenu>
{
    public static bool isPaused => Get().isActive;

    protected override void OnOpen()
    {
        base.OnOpen();
        Time.timeScale = 0f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    protected override void OnClose()
    {
        base.OnClose();
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void QuitGame()
    {
        SceneManager.LoadScene("MainMenu");
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
    }

    public void Respawn() => Gameplay.SpawnPlayer();
    public void ReloadSave() => Gameplay.Get().ResetToSaved();
}
