using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DefaultExecutionOrder(ExecutionOrders.GameplaySystems)]
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


    public float BasicBlackout
    {
        get => blackout.color.a;
        set
        {
            blackout.color = new(blackout.color.r, blackout.color.g, blackout.color.b, value);
            blackout.raycastTarget = value > 0;
        }
    }

    private float blackoutRate = 0f;


    [SerializeField] protected Image blackout;
    [SerializeField] protected Animator animator;

    protected virtual void Awake()
    {
        ActiveOverlays.Add(intendedLayer, this);
        if (animator == null) animator = GetComponent<Animator>();
        if (blackout == null) blackout = transform.Find("Basic Fade").GetComponent<Image>();
    }

    private void Update()
    {
        if(blackoutRate > 0)
        {
            BasicBlackout += blackoutRate * Time.unscaledDeltaTime;
            if(BasicBlackout >= 1f)
            {
                BasicBlackout = 1f;
                blackoutRate = 0f;
            }
        }
        else if (blackoutRate < 0)
        {
            BasicBlackout += blackoutRate * Time.unscaledDeltaTime;
            if (BasicBlackout <= 0f)
            {
                BasicBlackout = 0f;
                blackoutRate = 0f;
            }
        }
    }

    public void BasicFadeOut(float duration = 1f) => blackoutRate = 1f / duration;
    public void BasicFadeIn(float duration = 1f) => blackoutRate = -1f / duration;

    public IEnumerator BasicFadeOutWait(float duration = 1f)
    {
        blackoutRate = 1f / duration;
        yield return new WaitUntil(()=> BasicBlackout == 1);
    }
    public IEnumerator BasicFadeInWait(float duration = 1f)
    {
        blackoutRate = -1f / duration;
        yield return new WaitUntil(() => BasicBlackout == 0);
    }





    public IEnumerator GameOverAnim(float duration = 1f)
    {
        SetAnimated(true);
        animator.Play("GameOverAnim", -1, 0f);
        animator.SetFloat("DurationSpeed", 1 / duration);
        yield return new WaitForSecondsRealtime(duration);
    }

    public void SetAnimated(bool value) => animator.enabled = value;
    public void SetAlpha(float alpha) => blackout.color = new(blackout.color.r, blackout.color.g, blackout.color.b, alpha);

    public void Reset() => animator.Play("Null");

}
