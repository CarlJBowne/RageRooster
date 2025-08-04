using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;
using SLS.ISingleton;

public class PauseMenu : MenuSingleton<PauseMenu>
{
    public static bool isPaused => Get().isActive;
    public static bool canPause = true;

    public static System.Action onPause;
    public static System.Action onUnPause;

    protected override void OnOpen()
    {
        base.OnOpen();
        onPause?.Invoke();
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
        onUnPause?.Invoke();
    }

    public void QuitGame()
    {
        Enum().Begin(Overlay.OverMenus);
        IEnumerator Enum()
        {
            yield return Overlay.OverMenus.BasicFadeOutWait();

            Time.timeScale = 1f;
            Close();
            Gameplay.musicEmitter.Stop();
            PlayerStateMachine.Get().HaveDestroyed();
            Gameplay.DESTROY(areYouSure: true);
            SceneManager.LoadScene("MainMenu");
            SceneManager.sceneLoaded += Done;
            void Done(Scene arg0, LoadSceneMode arg1)
            {
                Overlay.OverMenus.BasicFadeIn();
                SceneManager.sceneLoaded -= Done;
            }
        }
    }

    public void ReturnToMainMenu()
    {
        Time.timeScale = 1f;
        Close();
        SceneManager.LoadScene("MainMenu");
    }

    public void Respawn()
    {
        SpawnPlayer_CR().Begin(Gameplay.Get());
        IEnumerator SpawnPlayer_CR()
        {
            yield return Overlay.OverMenus.BasicFadeOutWait(1f);

            yield return Gameplay.SpawnPlayer();

            TrueClose();
            Overlay.OverMenus.BasicFadeIn(1f);
        }
    }
    public void ReloadSave()
    {
        Enum().Begin(Gameplay.Get());
        IEnumerator Enum()
        {
            Gameplay.PreReloadSave?.Invoke();
            yield return Overlay.OverMenus.BasicFadeOutWait(1.2f);

            yield return Gameplay.DoReloadSave();

            yield return Gameplay.SpawnPlayer();

            TrueClose();
            Overlay.OverMenus.BasicFadeIn(1.2f);
        }
    }
}
