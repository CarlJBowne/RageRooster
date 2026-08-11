using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using System;
using SLS.Singletons;
using SLS.MenuCore;
using RageRooster.World;
using RageRooster.Core;
using RageRooster;

public class PauseMenu : Menu, IPauseMenu
{
    static Singleton<PauseMenu> S;
    public static PauseMenu Get => S.Get;
    public static bool TryGet(out PauseMenu instance) => S.TryGet(out instance);
    public static bool Present => S.Active;

    public static bool isPaused => S.Get.isActive;

    public static bool canPause
    {
        get => Services.UI.canPause;
        set => Services.UI.canPause = value;
    }

    public static System.Action onPause;
    public static System.Action onUnPause;

    protected override void Awake()
    {
        S.Register(this);
        Services.UI.SetPause += SetPause;
        base.Awake();
    }
    protected override void OnDestroy()
    {
        S.Deregister(this);
        Services.UI.SetPause -= SetPause;
        base.OnDestroy();
    }

    protected override void OnOpen()
    {
        base.OnOpen();
        onPause?.Invoke();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Services.UI.OnPause?.Invoke(true);
    }
    protected override void OnClose()
    {
        base.OnClose();
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        onUnPause?.Invoke();
        Services.UI.OnPause?.Invoke(false);
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
        Gameplay.Respawn();
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
