using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ScreenTransition
{
    public void Start(MonoBehaviour owner = null) => ActiveCoroutine = new(Enum(), owner != null ? owner : Overlay.OverMenus);

    public Action PreOutTransitionEvent;
    public IEnumerator OutTransition;
    public Action PostOutTransitionEvent;
    public IEnumerator MidTransitionWait;
    public Action PreInTransitionEvent;
    public IEnumerator InTransition;
    public Action PostInTransitionEvent;

    public Coroutine ActiveCoroutine { get; private set; }

    public IEnumerator Enum()
    {
        PreOutTransitionEvent?.Invoke();
        if (OutTransition != null) yield return OutTransition;
        PostOutTransitionEvent?.Invoke();
        if (MidTransitionWait != null) yield return MidTransitionWait;
        PreInTransitionEvent?.Invoke();
        if (InTransition != null) yield return InTransition;
        PostInTransitionEvent?.Invoke();
    }
}