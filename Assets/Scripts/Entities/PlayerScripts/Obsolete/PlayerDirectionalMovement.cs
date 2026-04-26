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

    protected override void OnAwake()
    {
        base.OnAwake();
        //playerMovementBody.direction = transform.forward;
    }

    protected override void OnFixedUpdate()
    {
        float deltaTime = Time.fixedDeltaTime / 0.02f;
        float currentSpeed = Player.MovementBody.velocity.f;
        Vector3 currentDirection = Player.MovementBody.DirectionGet;

        Vector3 controlDirection = Player.Controller.camAdjustedMovement.normalized;
        float controlMag = Player.Controller.camAdjustedMovement.sqrMagnitude;

        if (!forceMaxVelocity)
        {
            if (controlMag > 0)
            {
                float Dot = Vector3.Dot(controlDirection, currentDirection);

                if (maxTurnSpeed > 0) Player.MovementBody.DirectionSet(maxTurnSpeed * Time.fixedDeltaTime);

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
            if (maxTurnSpeed > 0) Player.MovementBody.DirectionSet(maxTurnSpeed * Time.fixedDeltaTime);
            MaxSpeedChange(true);
        }

        Player.MovementBody.velocity.f = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        //Player.MovementBody.VelocitySet(x: literalDirection.x, z: literalDirection.z);

        
        

    }

    private void MaxSpeedChange(bool value)
    {
        if (value == atTopSpeed) return;
        atTopSpeed = value;

        if (hitBox) hitBox.enabled = atTopSpeed;
    }

}
