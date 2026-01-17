using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using SLS.StateMachineH;
using RageRooster.Systems.SaveSystem;

public class PlayerGrabAction : PlayerStateBehavior
{
    public State noTargetState;
    public State blockedState;
    public State throwState;
    public State dropLaunchState;
    public UltEvents.UltEvent successReturn;

    private MeleeTarget target;


    public void GrabThrowButton()
    {
        if (Player.SignalManager.Locked) return;
        //if (Player.Grabber.currentGrabbed == null)
        BeginGrabAttempt();
    }

    void BeginGrabAttempt()
    {
        target = TargetingManager.GetMeleeTarget();
        if (target != null)
        {
            State.Enter();
            //Player.MovementBody.QuickTurnLimited(target.position - Player.MovementBody.Position, .1f);
        }
        else noTargetState.Enter();
    }

    public void EndGrabAttempt()
    {
        if (Grabbable.IsGrabbable(target, out Grabbable targetGrabbable))
        {
            Player.Grabber.Grab(targetGrabbable);
            successReturn?.Invoke();
            if (dropLaunchState != null && Upgrades.Active.dropLaunch && Input.Grab.IsPressed())
            {
                if (dropLaunchState != null && Upgrades.Active.dropLaunch) throwState = dropLaunchState;
                throwState.Enter();
            }
        }
        else blockedState.Enter();
        target = null;
    }

    void BeginThrow()
    {
        
    }
}
