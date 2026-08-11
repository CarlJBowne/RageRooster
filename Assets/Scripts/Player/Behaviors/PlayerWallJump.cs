using RageRooster.Core.Save;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using static RageRooster.Player.Services;

public class PlayerWallJump : PlayerMovementEffector
{
    public float gravity;
    public float terminalVelocity;
    public bool flatGravity;

    public float jumpPower;
    public float outwardVelocity;
    public float minDistance;
    public float maxAngleDifference;

    public string animationName = "WallJump";

    protected Vector3 startPoint;
    protected Vector3 fixedDirection;


    public override bool ForwardMovement(out float resultF)
    {
        resultF = outwardVelocity;

        float distance = (transform.position - startPoint).XZ().magnitude;
        if (distance >= minDistance) Player.StateMachine.Falling.Enter();
        return true;
    }
    public override bool VerticalMovement(out float result)
    {
        result = ApplyGravity(gravity, terminalVelocity, flatGravity);
        return true;
    }

    public bool WallJump(Vector3 direction)
    {
        if (Player.MovementBody.Sweep(Player.MovementBody.Direction.value * 0.5f, out RaycastHit hit, Player.MovementBody.Ground.groundCheckBuffer))
        {
            if (Vector3.Dot(Vector3.down, direction).Abs() > maxAngleDifference) return false;

            if (!State.Active) State.Enter();
            Player.Animator.Play(animationName, -1, 0f);
            Player.MovementBody.Velocity.y = jumpPower;

            startPoint = transform.position;
            fixedDirection = hit.normal.XZ();

            Player.MovementBody.Direction.Set(fixedDirection);

            State.Enter();
            return true;
        }
        return false;
    }
}
