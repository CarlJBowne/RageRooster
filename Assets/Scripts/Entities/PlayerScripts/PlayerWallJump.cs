using SLS.StateMachineV2;
using UnityEngine;

public class PlayerWallJump : PlayerAirborn
{
    public float outwardVelocity;
    public float minDistance;
    public float maxAngleDifference;

    protected Vector3 startPoint;
    protected Vector3 fixedDirection;

    public override void OnFixedUpdate()
    {
        
        body.VelocitySet(
            x: fixedDirection.x * outwardVelocity,
            y: ApplyGravity(),
            z: fixedDirection.z * outwardVelocity
            );
        body.currentSpeed = outwardVelocity;
        body.currentDirection = fixedDirection;

        float distance = (transform.position - startPoint).XZ().magnitude;
        if (distance >= minDistance) sFall.TransitionTo();

         
    }

    public override void OnEnter()
    {
    }

    public void WallJump(Vector3 direction)
    {
        if (Vector3.Dot(Vector3.down, direction).Abs() > maxAngleDifference) return;
        direction = direction.XZ();

        if (!state.active) state.TransitionTo();
        body.VelocitySet(y: jumpPower);

        startPoint = transform.position;
        fixedDirection = direction;
        
        body.currentDirection = fixedDirection;
    }
}
