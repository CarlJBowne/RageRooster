using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

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
    public Image blackout;

    private Animator animator;

    private void Awake()
    {
        ActiveOverlays.Add(intendedLayer, this);
        if (animator == null) animator = GetComponent<Animator>();
        if (blackout == null) blackout = transform.Find("Basic Fade").GetComponent<Image>();
    }

    public void BasicFadeOut(float duration = 1f)
    {
        animator.Play("BasicFadeOut", -1, 0f);
        animator.SetFloat("DurationSpeed", 1 / duration);
    }
    public void BasicFadeIn(float duration = 1f)
    {
        animator.Play("BasicFadeIn", -1, 0f);
        animator.SetFloat("DurationSpeed", 1 / duration);
    }

    public IEnumerator BasicFadeOutWait(float duration = 1f)
    {
        animator.Play("BasicFadeOut", -1, 0f);
        animator.SetFloat("DurationSpeed", 1 / duration);
        yield return new WaitForSecondsRealtime(duration);
    }
    public IEnumerator BasicFadeInWait(float duration = 1f)
    {
        animator.Play("BasicFadeIn", -1, 0f);
        animator.SetFloat("DurationSpeed", 1 / duration);
        yield return new WaitForSecondsRealtime(duration);
    }

    public IEnumerator GameOverAnim(float duration = 1f)
    {
        animator.Play("GameOverAnim", -1, 0f);
        animator.SetFloat("DurationSpeed", 1 / duration);
        yield return new WaitForSecondsRealtime(duration);
    }


    public void SetAlpha(float alpha) => blackout.color = new(blackout.color.r, blackout.color.g, blackout.color.b, alpha);

    public void Reset() => animator.Play("Null");

}
