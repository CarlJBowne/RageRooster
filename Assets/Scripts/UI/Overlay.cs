using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[DefaultExecutionOrder(-12)]
public class Overlay : MonoBehaviour
{
    public enum OverlayLayer
    {
        OverGameplay,
        OverHUD,
        OverMenus
    }
    public static Dictionary<OverlayLayer, Overlay> ActiveOverlays = new();

    public static Overlay OverGameplay => ActiveOverlays[OverlayLayer.OverGameplay];
    public static Overlay OverHUD => ActiveOverlays[OverlayLayer.OverHUD];
    public static Overlay OverMenus => ActiveOverlays[OverlayLayer.OverMenus];

    public OverlayLayer intendedLayer;

    private Animator animator;

    private void Awake()
    {
        ActiveOverlays.Add(intendedLayer, this);
        animator = GetComponent<Animator>();
    }

    public void BasicFadeOut(float duration = 1f)
    {
        animator.Play("BasicFadeOut");
        animator.SetFloat("DurationSpeed", 1 / duration);
    }
    public void BasicFadeIn(float duration = 1f)
    {
        animator.Play("BasicFadeIn");
        animator.SetFloat("DurationSpeed", 1 / duration);
    }

}
