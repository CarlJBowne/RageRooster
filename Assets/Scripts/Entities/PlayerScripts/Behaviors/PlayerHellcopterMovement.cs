using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;

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
        if (Player.MovementBody.velocity.y <= fallStateThreshold) Fall(ref result);
        return true;
    }

    protected override void VerticalUpwards(ref float Y)
    {
        if (Player.MovementBody.JumpStateCurrent == PlayerMovementBody.JumpState.Decelerating)
        {
            Y = currentVent.hellcopterSpeed;
            if (transform.position.y >= targetHeight) Player.MovementBody.UnLand(PlayerMovementBody.JumpState.Falling);
        }
        else if (Player.MovementBody.JumpStateCurrent == PlayerMovementBody.JumpState.Falling && Player.MovementBody.velocity.y <= fallStateThreshold) Fall(ref Y);

    }

    protected override void Fall(ref float Y)
    {
        if (Player.MovementBody.velocity.y > fallStateThreshold) Y = fallStateThreshold;
        Player.MovementBody.UnLand(PlayerMovementBody.JumpState.Falling);
        if (fallState != null) fallState.Enter();
    }


    protected override void StartFrom_Jump()
    {
        if (!isUpward) return;

        currentVent = Player.MovementBody.CurrentVent;
        targetHeight = currentVent.transform.position.y + currentVent.hellcopterTargetHeight;

        Player.MovementBody.velocity.y = currentVent.hellcopterSpeed;
        targetHeight -= (currentVent.hellcopterSpeed.P()) / (2 * gravity * Time.deltaTime);
        if (targetHeight <= transform.position.y)
        {
            Player.MovementBody.velocity.y = Mathf.Sqrt(2 * gravity * currentVent.hellcopterTargetHeight);
        }

#if UNITY_EDITOR
        Player.MovementBody.jumpMarkers = new()
                {
                    transform.position,
                    transform.position + Vector3.up * targetHeight
                };
#endif
    }

    protected override void StartFrom_Decel()
    {

    }

    protected override void StartFrom_Falling()
    {
        Player.MovementBody.velocity.y = Player.MovementBody.velocity.y.Max(0);
    }

    public override void BeginJump() => throw new System.Exception("Don't Use This Method.");
    public override void BeginJump(float power, float height, float minHeight) => throw new System.Exception("Don't Use This Method.");
    public override void BeginJump(PlayerMovementBody.JumpState newState) => throw new System.Exception("Don't Use This Method.");
}
