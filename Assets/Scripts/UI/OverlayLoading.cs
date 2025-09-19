using RageRooster.RoomSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class OverlayLoading : Overlay
{
    public static OverlayLoading overlay;
    [SerializeField] float showTime;

    public static void ShowIfLong()
    {
        if (overlay != null || !overlay.isActiveAndEnabled) return;
        Enum().Begin(overlay);
        static IEnumerator Enum()
        {
            yield return new WaitForSecondsRealtime(overlay.showTime);
            if (RoomManager.loading) SetVisible(true);
        }
    }


    protected override void Awake()
    {
        overlay = this;
        if (animator == null) animator = GetComponent<Animator>();
        if (blackout == null) blackout = transform.Find("Basic Fade").GetComponent<Image>();
        animator.Play("Loading");
        gameObject.SetActive(false);
    }

    public static void SetVisible(bool value)
    {
        overlay.gameObject.SetActive(value);
        if(value) overlay.animator.Play("Loading");
    }

}
