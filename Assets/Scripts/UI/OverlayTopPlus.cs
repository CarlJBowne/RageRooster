using System;
using System.Collections;
using System.Collections.Generic;
using RageRooster;
using RageRooster.Core;
using RageRooster.World;
using SLS.MenuCore;
using SLS.Singletons;
using UnityEngine;
using UnityEngine.UI;


public class OverlayTopPlus : Overlay, IOverlayTopPlus
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
        Services.OverlayTopPlus = this;

        if (animator == null) animator = GetComponent<Animator>();
        if (image == null) image = GetComponent<Image>();
        ResetState();
    }
    private void OnDestroy()
    {
        self.Deregister(this);
        Services.OverlayTopPlus = null;
    }

    public void LoadingPopup(bool value = true)
    {
        if (!Active) return;
        if (value)
        {
            Coroutine.Begin(ref activeRoutine, Enum(), true);
            static IEnumerator Enum()
            {
                yield return new WaitForSecondsRealtime(Get.showTime);
                if (RoomManager.CurrentlyTransitioning) Get.PlayAnimation(LoadingAnimationHash);
            }
        }
        else
        {
            Coroutine.Stop(ref activeRoutine);
            Get.ResetState();
            Get.transform.Find("CORN").gameObject.SetActive(false);
        }
    }

    public IEnumerator GameOverAnim(float duration = 1f)
    {
        PlayAnimation(GameOverAnimationHash);
        animator.SetFloat(DurationSpeedParamHash, 1 / duration);
        yield return new WaitForSecondsRealtime(duration);
    }

}
