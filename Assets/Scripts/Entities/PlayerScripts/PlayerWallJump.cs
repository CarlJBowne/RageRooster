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

        body.currentSpeed = outwardVelocity;
        body.currentDirection = fixedDirection;

        float distance = (transform.position - startPoint).XZ().magnitude;
        if (distance >= minDistance) sFall.TransitionTo();

    }
    public override void VerticalMovement(out float? result) => result = ApplyGravity(gravity, terminalVelocity, flatGravity);

    public void WallJump(Vector3 direction)
    {
        if(upgrade && body.rb.DirectionCast(body.currentDirection, 0.5f, body.checkBuffer, out RaycastHit hit))
        {
            if (Vector3.Dot(Vector3.down, direction).Abs() > maxAngleDifference) return;
            direction = direction.XZ().Rotate(180, Vector3.up);

            if (!state.active) state.TransitionTo();
            body.VelocitySet(y: jumpPower);

            startPoint = transform.position;
            fixedDirection = direction;

            body.currentDirection = fixedDirection;

            state.TransitionTo();
        }
    }
}
