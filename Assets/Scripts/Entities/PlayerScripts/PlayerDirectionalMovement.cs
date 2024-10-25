using UnityEngine;

public class PlayerDirectionalMovement : PlayerStateBehavior
{
    #region Config
    public float acceleration;
    public float decceleration;
    public float maxSpeed;
    public float stopping = 0.75f;
    [Tooltip("1 = full second turn, 50 = 1 FixedUpdate turn")]
    public float maxTurnSpeed = 25;
    public float minSpeedForRotate;

    #endregion Config

    public override void OnAwake()
    {
        base.OnAwake();
        body.currentDirection = transform.forward;
    }

    public override void OnFixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime / 0.02f;
        float currentSpeed = body.currentSpeed;
        Vector3 currentDirection = body.currentDirection;

        Vector3 controlDirection = controller.camAdjustedMovement.normalized;
        float controlMag = controller.camAdjustedMovement.sqrMagnitude;

        if (controlMag > 0)
        {
            float Dot = Vector3.Dot(controlDirection, currentDirection);
            currentDirection = Vector3.RotateTowards(currentDirection, controlDirection, maxTurnSpeed * Mathf.PI * Time.fixedDeltaTime, 0);

            currentSpeed *= Dot;
            if(currentSpeed < maxSpeed)
                currentSpeed = (currentSpeed + (controlMag * acceleration)).Max(maxSpeed) * deltaTime;
            else if(currentSpeed > maxSpeed)
                currentSpeed = (currentSpeed - (controlMag * decceleration)).Min(maxSpeed) * deltaTime;
        }
        else
        {
            currentSpeed -= currentSpeed * stopping * deltaTime;
        }

        body.rotation = currentDirection.DirToRot();

        Vector3 literalDirection = transform.forward * currentSpeed;

        body.VelocitySet(x: literalDirection.x, z: literalDirection.z);

        body.currentSpeed = currentSpeed;
        body.currentDirection = currentDirection;

        /*
        Vector3 maxGoal = movementDirection * maxSpeed;

        Vector3 workVelocity = body.velocity.XZ();

        workVelocity = Vector3.MoveTowards(workVelocity, maxGoal,
            movementDirection.magnitude > 0
            ? (-Vector3.Dot(maxGoal, workVelocity) + 1).Min(1f)
            : decceleration);

        body.SetVelocity(x: workVelocity.x, z: workVelocity.z);
        */

    }


}
