using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerWallJump : PlayerMovementEffector
{
    public float gravity;
    public float terminalVelocity;
    public bool flatGravity;

    public float jumpPower;
    public float outwardVelocity;
    public float minDistance;
    public float maxAngleDifference;

    public Upgrade upgrade;

    protected Vector3 startPoint;
    protected Vector3 fixedDirection;


    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        resultX = fixedDirection.x * outwardVelocity;
        resultZ = fixedDirection.z * outwardVelocity;

        playerMovementBody.CurrentSpeed = outwardVelocity;
        playerMovementBody.currentDirection = fixedDirection;

        float distance = (transform.position - startPoint).XZ().magnitude;
        if (distance >= minDistance) sFall.TransitionTo();

    }
    public override void VerticalMovement(out float? result) => result = ApplyGravity(gravity, terminalVelocity, flatGravity);

    public bool WallJump(Vector3 direction)
    {
        if(upgrade && playerMovementBody.rb.DirectionCast(playerMovementBody.currentDirection, 0.5f, playerMovementBody.checkBuffer, out RaycastHit hit))
        {
            if (Vector3.Dot(Vector3.down, direction).Abs() > maxAngleDifference) return false;

            if (!state.active) state.TransitionTo();
            playerMovementBody.VelocitySet(y: jumpPower);

            startPoint = transform.position;
            fixedDirection = hit.normal.XZ();

            playerMovementBody.currentDirection = fixedDirection;

            state.TransitionTo();
            return true;
        }
        return false;
    }
}
