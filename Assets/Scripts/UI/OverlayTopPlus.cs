using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using SLS.MenuCore;
using SLS.Singletons;
using RageRooster.World;


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

    public static void LoadingPopup()
    {
        if (!Active) return;
        Coroutine.Begin(ref Get.activeRoutine, Enum(), true);
        static IEnumerator Enum()
        {
            yield return new WaitForSecondsRealtime(Get.showTime);
            if (RoomManager.CurrentlyTransitioning) Get.PlayAnimation(LoadingAnimationHash);
        }
    }
    public static void EndLoadingPopup()
    {
        Coroutine.Stop(ref Get.activeRoutine);
        Get.ResetState();
        Get.transform.Find("CORN").gameObject.SetActive(false);
    }

    public IEnumerator GameOverAnim(float duration = 1f)
    {
        PlayAnimation(GameOverAnimationHash);
        animator.SetFloat(DurationSpeedParamHash, 1 / duration);
        yield return new WaitForSecondsRealtime(duration);
    }

}
