using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using SLS.Physics3D;
using SLS.StateMachineH;
using UnityEngine;

public class PlayerHellcopterMovement : PlayerAirborneMovement
{

    //public new PlayerHellcopterMovement fallState;
    VolcanicVent currentVent = null;

    /*
     Parameters inherited from PlayerAirborneMovement that are irrelevant and should be hidden:
     *jumpHeight
     *jumpPower
     *jumpMinHeight
     *forceDownwards
     *allowMidFall
     */


    public override bool VerticalMovement(out float result)
    {
        result = ApplyGravity(gravity, terminalVelocity, flatGravity);
        VerticalUpwards(ref result);
        if (Player.MovementBody.Velocity.y <= fallStateThreshold) Fall(ref result);
        return true;
    }

    protected override void VerticalUpwards(ref float Y)
    {
        if (Player.MovementBody.Ground.value == GroundState.Values.Decelerating)
        {
            Y = currentVent.hellcopterSpeed;
            if (transform.position.y >= targetHeight) Player.MovementBody.UnLand(GroundState.Values.Falling);
        }
        else if (Player.MovementBody.Ground.value == GroundState.Values.Falling && Player.MovementBody.Velocity.y <= fallStateThreshold) Fall(ref Y);

    }

    protected override void Fall(ref float Y)
    {
        if (Player.MovementBody.Velocity.y > fallStateThreshold) Y = fallStateThreshold;
        Player.MovementBody.UnLand(GroundState.Values.Falling);
        if (fallState != null) fallState.Enter();
    }


    protected override void StartFrom_Jump()
    {
        if (!isUpward) return;

        currentVent = Player.MovementBody.CurrentVent;
        targetHeight = currentVent.transform.position.y + currentVent.hellcopterTargetHeight;

        Player.MovementBody.Velocity.y = currentVent.hellcopterSpeed;
        targetHeight -= (currentVent.hellcopterSpeed.P()) / (2 * gravity * Time.deltaTime);
        if (targetHeight <= transform.position.y)
        {
            Player.MovementBody.Velocity.y = Mathf.Sqrt(2 * gravity * currentVent.hellcopterTargetHeight);
        }

#if UNITY_EDITOR
        Player.MovementBody.Debug.PlaceJumpMarker(targetHeight, 0);
#endif
    }

    protected override void StartFrom_Decel()
    {

    }

    protected override void StartFrom_Falling()
    {
        Player.MovementBody.Velocity.y = Player.MovementBody.Velocity.y.Max(0);
    }

    public override void BeginJump() => throw new System.Exception("Don't Use This Method.");
    public override void BeginJump(float power, float height, float minHeight) => throw new System.Exception("Don't Use This Method.");
    public override void BeginJump(GroundState.Values newState) => throw new System.Exception("Don't Use This Method.");
}
