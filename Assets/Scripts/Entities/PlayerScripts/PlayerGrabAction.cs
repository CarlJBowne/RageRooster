using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using SLS.StateMachineV3;

public class PlayerGrabAction : PlayerStateBehavior
{
    public bool air;
    public string animationName;
    
    [HideProperty] public bool wasHeld;
    [HideProperty] public bool success;

    private IGrabbable selectedGrabbable;
    private PlayerRanged grabber;
    private PlayerMovementAnimator movementNegator;

    public override void OnAwake()
    {
        grabber = GetComponentFromMachine<PlayerRanged>();
        movementNegator = GetComponent<PlayerMovementAnimator>();
        movementNegator = GetComponent<PlayerMovementAnimator>();
    }

    public void BeginGrabAttempt(IGrabbable attempt, bool held)
    {
        state.TransitionTo();
        Machine.animator.CrossFade(animationName, .1f, -1, 0f);
        if (attempt != null)
        {
            selectedGrabbable = attempt;
            wasHeld = held;
            success = true;
            movementNegator.locked = false;
        }
        else
        {
            success = false;
            movementNegator.locked = true;
        }
    }

    public void GrabPoint()
    {
        if (!success || selectedGrabbable == null)
        {
            IGrabbable lastMinute = grabber.CheckForGrabbable();
            if(lastMinute == null) return;
            selectedGrabbable = lastMinute;
        }
        grabber.GrabPoint(selectedGrabbable);
        if (air && grabber.dropLaunchUpgrade && !Input.Grab.IsPressed()) grabber.BeginThrow(true);
        success = false;
        wasHeld = false;
        selectedGrabbable = null;
    }


}
