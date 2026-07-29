using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using SLS.Physics3D;

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

    public override bool VerticalMovement(out float result)
    {
        if (!isVentGlide || transform.position.y > targetHeight)
        {
            result = ApplyGravity(gravity, terminalVelocity, flatGravity);
            Player.MovementBody.Ground.UnLand(GroundState.Values.Falling);
        }
        else if (transform.position.y < targetHeight)
        {
            result = raiseRate/* * currentVent.transform.up.y*/;
            Player.MovementBody.Ground.UnLand(GroundState.Values.Hangtime);
        }
        else result = 0;

        if (!Input.Jump.IsPressed()) Fall(ref result);
        return true;
    }



    protected override void Fall(ref float Y)
    {
        Y = Y.Max(0);

        Player.MovementBody.Ground.UnLand(GroundState.Values.Falling);
        if (fallState != null) fallState.Enter();
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        if (!isFinal) return;

        Player.MovementBody.Ground.UnLand();

        Player.MovementBody.Velocity.y = Player.MovementBody.Velocity.y.Max(0);

        if (isVentGlide)
        {
            currentVent = Player.MovementBody.CurrentVent;
            targetHeight = currentVent.transform.position.y + (currentVent.glideHeight/* * currentVent.transform.up.y*/);
        }
    }

    public override void BeginJump() => throw new System.Exception("Don't Use This Method.");
    public override void BeginJump(float power, float height, float minHeight) => throw new System.Exception("Don't Use This Method.");
    public override void BeginJump(GroundState.Values newState) => throw new System.Exception("Don't Use This Method.");
}
