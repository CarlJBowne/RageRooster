using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;

public class PauseMenu : MenuSingleton<PauseMenu>
{
    public static bool isPaused => Get().isActive;
    public static bool canPause = true;

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
        Time.timeScale = 1f;
        Close();
        Gameplay.musicEmitter.Stop();
        Gameplay.DESTROY(areYouSure: true);
        SceneManager.LoadScene("MainMenu");

    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        Close();
        SceneManager.LoadScene("MainMenu");
    }

    public void Respawn() => Gameplay.RespawnFromMenu();
    public void ReloadSave() => Gameplay.Get().ResetToLastSave();
}
