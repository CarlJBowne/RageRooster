using System;
using System.Collections;
using System.Collections.Generic;
using RageRooster;
using RageRooster.Core;
using RageRooster.Core.World;
using SLS.GeneralUtilities.Events;
using SLS.MenuCore;
using SLS.Singletons;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using static RageRooster.Services;

public class PauseMenu : RageRooster.Core.PauseMenu
{

    protected override void Awake()
    {
        S.Register(this);
        base.Awake();
        EventTicket.Set(ref canPauseTicket, Input.Pause.SubscribePerformed(Get.Open, true));
    }
    protected override void OnDestroy()
    {
        EventTicket.Destroy(ref canPauseTicket);
        S.Deregister(this);
        base.OnDestroy();
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        DoStatic.OnPause();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
    protected override void OnClose()
    {
        base.OnClose();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        DoStatic.OnUnPause();
    }

    public void QuitGame()
    {
        Enum().Begin();
        IEnumerator Enum()
        {
            yield return Overlay.OverALL.FadeAlpha(1);

            Time.timeScale = 1f;
            Close();
            Gameplay.EndGame();
            SceneManager.LoadScene("MainMenu");
            SceneManager.sceneLoaded += Done;
            void Done(Scene arg0, LoadSceneMode arg1)
            {
                Overlay.OverALL.FadeAlpha(0);
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
            FadeOutRoutine = Overlay.OverALL.FadeAlpha(1, 1f),
            FadeInRoutine = Overlay.OverALL.FadeAlpha(0, 1f),
            PreFadeInAction = TrueClose,
        };
        Player.Respawn();
    }
    public void ReloadSave()
    {
        RoomManager.TransitionStyle = new()
        {
            FadeOutRoutine = Overlay.OverALL.FadeAlpha(1, 1.2f),
            FadeInRoutine = Overlay.OverALL.FadeAlpha(0, 1.2f),
            PreFadeInAction = TrueClose
        };
        Gameplay.ReloadSave();
    }

    private void SetPause(bool value)
    {
        if (value) Open();
        else Close();
    }
}
