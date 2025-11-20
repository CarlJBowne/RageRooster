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

    private MeleeTarget grabTarget;







    public void GrabThrowButton()
    {
        if (Player.SignalManager.Locked) return;
        if (Player.Grabber.currentGrabbed == null)
            BeginGrabAttempt();
        else BeginThrow();
    }

    void BeginGrabAttempt()
    {
        grabTarget = TargetingManager.GetMeleeTarget();
        if (grabTarget != null)
        {
            State.Enter();
            Player.MovementBody.QuickTurnLimited(grabTarget.position - Player.MovementBody.Position, .1f);
        }
        else noTargetState.Enter();
    }

    public void EndGrabAttempt()
    {
        IGrabbable targetGrabbable = grabTarget.GetComponent<IGrabbable>();

        if(targetGrabbable != null && targetGrabbable.IsGrabbable) //If object is grabbable.
        {
            Player.Grabber.OfficialGrab(targetGrabbable);
            successReturnState.Enter();
            if(dropLaunchState != null && Upgrades.Active.dropLaunch && Input.Grab.IsPressed()) BeginThrow();
        }
        else
        {
            blockedState.Enter();
            grabTarget = null;
        }
    }

    void BeginThrow()
    {
        if (dropLaunchState != null && Upgrades.Active.dropLaunch) throwState = dropLaunchState;
        throwState.Enter();
    }








    #region OLD

    public bool air;
    public string animationName;
    
    [HideProperty] public bool success;

    private IGrabbable selectedGrabbable;
    [SerializeField] private PlayerRanged ranged;
    [SerializeField] private PlayerMovementAnimator movementNegator;

    protected override void OnSetup()
    {
        base.OnSetup();
        ranged = GetComponentFromMachine<PlayerRanged>();
        movementNegator = GetComponentFromMachine<PlayerMovementAnimator>();
    }

    public void BeginGrabAttempt(IGrabbable attempt)
    {
        State.Enter();
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
            IGrabbable lastMinute = PlayerInteracter.Get().HasUsableGrabbable();
            if(lastMinute == null) return;
            selectedGrabbable = lastMinute;
        }
        ranged.GrabPoint(selectedGrabbable);
        if (air && Upgrades.Active.dropLaunch && Input.Grab.IsPressed()) ranged.TryGrabThrowAir(this);
        success = false;
        selectedGrabbable = null;
    }

    public void Finish(State successState, State failState)
    {
        (ranged.currentGrabbed != null ? successState : failState).Enter();
        Machine.animator.CrossFade("GroundBasic", .1f);
    }
    #endregion
}
