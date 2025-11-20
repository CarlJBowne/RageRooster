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
    public State successReturnState;

    private MeleeTarget target;


    public void GrabThrowButton()
    {
        if (Player.SignalManager.Locked) return;
        if (Player.Grabber.currentGrabbed == null)
            BeginGrabAttempt();
        else BeginThrow();
    }

    void BeginGrabAttempt()
    {
        target = TargetingManager.GetMeleeTarget();
        if (target != null)
        {
            State.Enter();
            Player.MovementBody.QuickTurnLimited(target.position - Player.MovementBody.Position, .1f);
        }
        else noTargetState.Enter();
    }

    public void EndGrabAttempt()
    {
        IGrabbable targetGrabbable = target.GetComponent<IGrabbable>();

        if(targetGrabbable != null && targetGrabbable.IsGrabbable) //If object is grabbable.
        {
            Player.Grabber.Grab(targetGrabbable);
            successReturnState.Enter();
            if(dropLaunchState != null && Upgrades.Active.dropLaunch && Input.Grab.IsPressed()) BeginThrow();
        }
        else blockedState.Enter();
        target = null;
    }

    void BeginThrow()
    {
        if (dropLaunchState != null && Upgrades.Active.dropLaunch) throwState = dropLaunchState;
        throwState.Enter();
    }
}
