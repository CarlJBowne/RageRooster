using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using SLS.StateMachineH;
using RageRooster.Systems.SaveSystem;

public class PlayerGrabAction : PlayerStateBehavior
{
    public UltEvents.UltEvent successReturn;
    public UltEvents.UltEvent failMissedReturn;
    public UltEvents.UltEvent failBlockedReturn;
    public UltEvents.UltEvent altSwitchReturn;

    TargetType.Melee selectedTarget;
    Grabbable selectedGrabbable;

    public void DoGrabAttempt()
    {
        selectedTarget = TargetingManager.MeleeChannel.CurrentTargetType<TargetType.Melee>();
        if (selectedTarget) Grabbable.Attempt(selectedTarget.This.gameObject, Succeed, FailMiss, FailBlock);
    }

    void Succeed(Grabbable G)
    {
        selectedGrabbable = G;
        State.Enter();
    }
    void FailMiss()
    {
        failMissedReturn?.Invoke();
        selectedTarget = null;
        selectedGrabbable = null;

    }
    void FailBlock() 
    { 
        failBlockedReturn?.Invoke();
        selectedTarget = null;
        selectedGrabbable = null;
    }

    public void FinishGrab()
    {
        Player.Grabber.Grab(selectedGrabbable);
        successReturn?.Invoke();
        selectedTarget = null;
        selectedGrabbable = null;
    }


}
