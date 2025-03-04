using UnityEngine;

[System.Obsolete]
public class PlayerDirectionalMovement : PlayerStateBehavior
{
    #region Config
    public float acceleration;
    public float decceleration;
    public float maxSpeed;
    public float stopping = 0.75f;
    [Tooltip("1 = full second turn, 50 = 1 FixedUpdate turn")]
    public float maxTurnSpeed = 25;
    public bool outwardTurn;
    public float minSpeedForRotate;
    public Collider hitBox;
    public bool forceMaxVelocity;

    #endregion
    #region Data
    [HideInInspector] public bool atTopSpeed;

    #endregion 

    public override void OnAwake()
    {
        base.OnAwake();
        playerMovementBody.currentDirection = transform.forward;
    }

    public override void OnFixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime / 0.02f;
        float currentSpeed = playerMovementBody.currentSpeed;
        Vector3 currentDirection = playerMovementBody.currentDirection;

        Vector3 controlDirection = playerController.camAdjustedMovement.normalized;
        float controlMag = playerController.camAdjustedMovement.sqrMagnitude;

        if (!forceMaxVelocity)
        {
            if (controlMag > 0)
            {
                float Dot = Vector3.Dot(controlDirection, currentDirection);

                if (maxTurnSpeed > 0)
                    currentDirection = Vector3.RotateTowards(currentDirection, controlDirection, maxTurnSpeed * Mathf.PI * Time.fixedDeltaTime, 0);

                if (!outwardTurn) currentSpeed *= Dot;
                if (currentSpeed < maxSpeed)
                    currentSpeed = (currentSpeed + (controlMag * acceleration)).Max(maxSpeed) * deltaTime;
                else if (currentSpeed > maxSpeed)
                    currentSpeed = (currentSpeed - (controlMag * decceleration)).Min(maxSpeed) * deltaTime;

                if (currentSpeed == maxSpeed) MaxSpeedChange(true);
                else if (currentSpeed < maxSpeed) MaxSpeedChange(false);
            }
            else
            {
                currentSpeed -= currentSpeed * stopping * deltaTime;
                MaxSpeedChange(false);
            }
        }
        else
        {
            currentSpeed = maxSpeed;
            if (maxTurnSpeed > 0)
                currentDirection = Vector3.RotateTowards(currentDirection, controlDirection, maxTurnSpeed * Mathf.PI * Time.fixedDeltaTime, 0);
            MaxSpeedChange(true);
        }

        playerMovementBody.currentDirection = currentDirection;
        playerMovementBody.currentSpeed = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        playerMovementBody.VelocitySet(x: literalDirection.x, z: literalDirection.z);

        
        

    }

    private void MaxSpeedChange(bool value)
    {
        if (value == atTopSpeed) return;
        atTopSpeed = value;

        if (hitBox) hitBox.enabled = atTopSpeed;
    }

}
