using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using SLS.StateMachineV3;

public class PlayerGrabAction : PlayerStateBehavior
{
    public bool air;
    public string animationName;
    
    [HideProperty] public bool success;

    private IGrabbable selectedGrabbable;
    private PlayerRanged ranged;
    private PlayerMovementAnimator movementNegator;

    public override void OnAwake()
    {
        ranged = GetComponentFromMachine<PlayerRanged>();
        movementNegator = GetComponent<PlayerMovementAnimator>();
        movementNegator = GetComponent<PlayerMovementAnimator>();
    }

    public void BeginGrabAttempt(IGrabbable attempt)
    {
        state.TransitionTo();
        Machine.animator.CrossFade(animationName, .1f, -1, 0f);
        if (attempt != null)
        {
            selectedGrabbable = attempt;
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
            IGrabbable lastMinute = ranged.CheckForGrabbable();
            if(lastMinute == null) return;
            selectedGrabbable = lastMinute;
        }
        ranged.GrabPoint(selectedGrabbable);
        if (air && ranged.dropLaunchUpgrade && Input.Grab.IsPressed()) ranged.TryGrabThrowAir(this);
        success = false;
        selectedGrabbable = null;
    }

    public void Finish(State successState, State failState)
    {
        (ranged.currentGrabbed != null ? successState : failState).TransitionTo();
        Machine.animator.CrossFade("GroundBasic", .1f);
    }
}
