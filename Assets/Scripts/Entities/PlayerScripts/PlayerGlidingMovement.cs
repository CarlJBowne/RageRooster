using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;

public class PlayerGlidingMovement : PlayerAirborneMovement
{
    public bool isVentGlide;
    public float raiseRate;

    VolcanicVent currentVent = null;

    /*
     Parameters inherited from PlayerAirborneMovement that are irrelevant and should be hidden:
     *isDash
     *minSpeed
     *defaultPhase
     *fallStateThreshold
     */

    //Only change from PlayerAirborneMovement is the removal of HorizontalCharge.
    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = playerMovementBody.CurrentSpeed;
        Vector3 currentDirection = playerMovementBody.direction;

        HorizontalMain(ref currentSpeed, ref currentDirection, playerController.camAdjustedMovement, Time.fixedDeltaTime * 50);

        playerMovementBody.CurrentSpeed = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        resultX = literalDirection.x;
        resultZ = literalDirection.z;
    }
    public override void VerticalMovement(out float? result)
    {
        if(!isVentGlide || transform.position.y > targetHeight)
        {
            result = ApplyGravity(gravity, terminalVelocity, flatGravity);
            playerMovementBody.JumpState = JumpState.Falling;
        }  
        else if (transform.position.y < targetHeight)
        {
            result = raiseRate/* * currentVent.transform.up.y*/;
            playerMovementBody.JumpState = JumpState.Hangtime;
        }  
        else result = 0;

        if(!Input.Jump.IsPressed()) Fall(ref result);

    }



    protected override void Fall(ref float? Y)
    {
        Y = Y.Value.Max(0);

        playerMovementBody.JumpState = JumpState.Falling;
        if (fallState != null) fallState.Enter();
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        if (!isFinal) return;

        playerMovementBody.UnLand();

        playerMovementBody.VelocitySet(y: playerMovementBody.velocity.y.Max(0));

        if (isVentGlide)
        {
            currentVent = playerMovementBody.currentVent;
            targetHeight = currentVent.transform.position.y + (currentVent.glideHeight/* * currentVent.transform.up.y*/);
        }
    }

    public override void BeginJump() => throw new System.Exception("Don't Use This Method.");
    public override void BeginJump(float power, float height, float minHeight) => throw new System.Exception("Don't Use This Method.");
    public override void BeginJump(JumpState newState) => throw new System.Exception("Don't Use This Method.");
}
