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
        if (AudioManager.Get() != null && FMODEvents.instance != null)
        {
            AudioManager.Get().PlayOneShot(FMODEvents.instance.selectionConfirm, this.transform.position);
        }
        SceneManager.LoadScene("MainMenu");
    }

    //public void OpenOptions()
    //{
    //    if (AudioManager.Get() != null && FMODEvents.instance != null)
    //    {
    //        AudioManager.Get().PlayOneShot(FMODEvents.instance.selectionConfirm, this.transform.position);
    //    }
    //    optionsMenuUI.SetActive(true);
    //    pauseMenuUI.SetActive(false);
    //    eventSystem.enabled = true;
    //}

    //public void CloseOptions()
    //{
    //    if (AudioManager.Get() != null && FMODEvents.instance != null)
    //    {
    //        AudioManager.Get().PlayOneShot(FMODEvents.instance.selectionConfirm, this.transform.position);
    //    }
    //    optionsMenuUI.SetActive(false);
    //    pauseMenuUI.SetActive(true);
    //    eventSystem.enabled = true;
    //}

    public void ReturnToMainMenu()
    {
        if (AudioManager.Get() != null && FMODEvents.instance != null)
        {
            AudioManager.Get().PlayOneShot(FMODEvents.instance.selectionConfirm, this.transform.position);
        }
        Time.timeScale = 1f;
        SceneManager.LoadScene("MainMenu");
        //eventSystem.enabled = false;
    }

    public void Respawn() => Gameplay.SpawnPlayer();
    public void ReloadSave() => Gameplay.Get().ResetToSaved();

}