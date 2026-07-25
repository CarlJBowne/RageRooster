using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SLS.MenuCore;
using SLS.Singletons;
using RageRooster.RoomSystem;


public class OverlayTopPlus : Overlay
{
    static Singleton<OverlayTopPlus> self;

    public static OverlayTopPlus Get => self.Get;
    public static bool TryGet(out OverlayTopPlus overlay) => self.TryGet(out overlay);
    public static bool Active => self.Active;

    public float showTime = 3f;

    protected static int LoadingAnimationHash = Animator.StringToHash("Loading");
    protected static int GameOverAnimationHash = Animator.StringToHash("GameOver");
    protected static int DurationSpeedParamHash = Animator.StringToHash("DurationSpeed");    

    protected override void Awake()
    {
        self.Register(this);

        if (animator == null) animator = GetComponent<Animator>();
        if (image == null) image = GetComponent<Image>();
        ResetState();
    }

    public static void LoadingScreenIfLong()
    {
        if (!Active) return;
        Coroutine.Begin(ref activeAnimationRoutine, Enum(), true);
        static IEnumerator Enum()
        {
            yield return new WaitForSecondsRealtime(Get.showTime);
            if (RoomManager.CurrentlyTransitioning)
            {
                Get.SetAnimated(true);
                Get.animator.Play(LoadingAnimationHash, -1, 0f);
            }
        }
    }
    static Coroutine activeAnimationRoutine;

    public IEnumerator GameOverAnim(float duration = 1f)
    {
        SetAnimated(true);
        animator.Play(GameOverAnimationHash, -1, 0f);
        animator.SetFloat(DurationSpeedParamHash, 1 / duration);
        yield return new WaitForSecondsRealtime(duration);
    }

    public override void ResetState()
    {
        base.ResetState();
        Coroutine.Stop(ref activeAnimationRoutine);
    }

}
