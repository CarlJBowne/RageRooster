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


    public override void VerticalMovement(out float? result)
    {
        result = ApplyGravity(gravity, terminalVelocity, flatGravity);
        if (isUpward) VerticalUpwards(ref result);
        else if (playerMovementBody.velocity.y <= fallStateThreshold) Fall(ref result);
    }

    protected override void VerticalUpwards(ref float? Y)
    {
        if (playerMovementBody.JumpState == JumpState.Decelerating)
        {
            Y = currentVent.hellcopterSpeed;
            if (transform.position.y >= targetHeight) playerMovementBody.UnLand(JumpState.Falling);
        } 
        else if (playerMovementBody.JumpState == JumpState.Falling && playerMovementBody.velocity.y <= fallStateThreshold) Fall(ref Y);

    }

    protected override void Fall(ref float? Y)
    {
        if (playerMovementBody.velocity.y > fallStateThreshold) Y = fallStateThreshold;
        playerMovementBody.UnLand(JumpState.Falling);
        if (fallState != null) fallState.Enter();
    }


    protected override void StartFrom_Jump() { }

    protected override void StartFrom_Decel()
    {
        if (!isUpward) return;

        currentVent = playerMovementBody.currentVent;
        targetHeight = currentVent.transform.position.y + currentVent.hellcopterTargetHeight;

        playerMovementBody.VelocitySet(y: currentVent.hellcopterSpeed);
        targetHeight = (currentVent.transform.position.y + currentVent.hellcopterTargetHeight) - (currentVent.hellcopterSpeed.P()) / (2 * gravity);
        if (targetHeight <= transform.position.y)
        {
            playerMovementBody.VelocitySet(y: Mathf.Sqrt(2 * gravity * currentVent.hellcopterTargetHeight));
        }

#if UNITY_EDITOR
        playerMovementBody.jumpMarkers = new()
                {
                    transform.position,
                    transform.position + Vector3.up * targetHeight
                };
#endif
    }

    protected override void StartFrom_Falling()
    {
        playerMovementBody.VelocitySet(y: playerMovementBody.velocity.y.Max(0));
    }

    public override void BeginJump() => throw new System.Exception("Don't Use This Method.");
    public override void BeginJump(float power, float height, float minHeight) => throw new System.Exception("Don't Use This Method.");
    public override void BeginJump(JumpState newState) => throw new System.Exception("Don't Use This Method.");
}
