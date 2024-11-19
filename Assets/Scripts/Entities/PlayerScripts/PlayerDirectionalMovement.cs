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
    public bool outwardTurn;
    public float minSpeedForRotate;
    public PlayerFullbodyHitbox hitBox;

    #endregion
    #region Data
    [HideInInspector] public bool atTopSpeed;

    #endregion 

    public override void OnAwake()
    {
        base.OnAwake();
        body.currentDirection = transform.forward;
        hitBox = GetComponent<PlayerFullbodyHitbox>();
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
            
            if(maxTurnSpeed > 0)
                currentDirection = Vector3.RotateTowards(currentDirection, controlDirection, maxTurnSpeed * Mathf.PI * Time.fixedDeltaTime, 0);

            if(!outwardTurn) currentSpeed *= Dot;
            if(currentSpeed < maxSpeed)
                currentSpeed = (currentSpeed + (controlMag * acceleration)).Max(maxSpeed) * deltaTime;
            else if(currentSpeed > maxSpeed)
                currentSpeed = (currentSpeed - (controlMag * decceleration)).Min(maxSpeed) * deltaTime;

            if (currentSpeed == maxSpeed) MaxSpeedChange(true);
            else if (currentSpeed < maxSpeed) MaxSpeedChange(false);
        }
        else
        {
            currentSpeed -= currentSpeed * stopping * deltaTime;
            MaxSpeedChange(false);
        }

        body.rotation = currentDirection.DirToRot();

        Vector3 literalDirection = transform.forward * currentSpeed;

        body.VelocitySet(x: literalDirection.x, z: literalDirection.z);

        body.currentSpeed = currentSpeed;
        body.currentDirection = currentDirection;

    }

    private void MaxSpeedChange(bool value)
    {
        if (value == atTopSpeed) return;
        atTopSpeed = value;

        if (hitBox) hitBox.SetBoxState(value);
    }

}
