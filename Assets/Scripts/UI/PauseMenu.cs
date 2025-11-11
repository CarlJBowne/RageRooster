using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;
using SLS.ISingleton;
using RageRooster.Systems;
using RageRooster.RoomSystem;

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
        Gameplay.GameState = Gameplay.GameStates.Paused;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    protected override void OnClose()
    {
        base.OnClose();
        Gameplay.GameState = Gameplay.GameStates.Active;
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
            Music.StopAllMusic();
            Player.StateMachine.HaveDestroyed();
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
        RoomManager.TransitionStyle = new()
        {
            FadeOutRoutine = Overlay.OverMenus.BasicFadeOutWait(1f),
            FadeInRoutine = Overlay.OverMenus.BasicFadeInWait(1f),
            PreFadeInAction = TrueClose,
        };
        Gameplay.Respawn();
    }
    public void ReloadSave()
    {
        RoomManager.TransitionStyle = new()
        {
            FadeOutRoutine = Overlay.OverMenus.BasicFadeOutWait(1.2f),
            FadeInRoutine = Overlay.OverMenus.BasicFadeInWait(1.2f),
            PreFadeInAction = TrueClose
        };
        Gameplay.ReloadSave();
    }
}
