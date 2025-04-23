using AYellowpaper.SerializedCollections;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

public class EndingScene : MonoBehaviour
{
    public TMPro.TextMeshProUGUI endingText;
    public WorldChange[] hens;
    public EndingValue[] possibleEndings;
    public InputActionReference continueToTitle;
    public Animator animator;
    //public MusicPlayerUtility music;

    [System.Serializable]
    public struct EndingValue
    {
        public int henRequirement;
        public string endingText;
    }


    private void Start()
    {
        endingText.text = GetEnding(HensCollected());
        animator.Play("EndingAnimation");
        //music.PlayMusic();
    }


    public int HensCollected()
    {
        int result = 0;
        for (int i = 0; i < hens.Length; i++)
            if (hens[i].Enabled) result++;
        return result;
    }

    public string GetEnding(int hens)
    {
        int i = 0;
        while (possibleEndings[i + 1].henRequirement <= hens) i++;
        return possibleEndings[i].endingText;
    }

    public void ActivateReturnButton() => continueToTitle.action.performed += Return;

    public void Return(InputAction.CallbackContext no)
    {
        continueToTitle.action.performed -= Return;
        Enum().Begin(Overlay.OverMenus);
        IEnumerator Enum()
        {
            yield return Overlay.OverMenus.BasicFadeOutWait(2f);
            AsyncOperation S = SceneManager.LoadSceneAsync("MainMenu", LoadSceneMode.Single);
            yield return new WaitUntil(() => S.isDone);
            yield return new WaitForSecondsRealtime(.5f);
            yield return Overlay.OverMenus.BasicFadeInWait(1f);
        }
    }


}
