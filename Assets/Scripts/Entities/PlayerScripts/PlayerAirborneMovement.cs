using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;
using static TrackerEB;

public class PlayerAirborneMovement : PlayerMovementEffector
{

    [Header("Horizontal")]
    public float acceleration;
    public float decceleration;
    public float maxSpeed;
    public float stopping = 0.75f;
    [Tooltip("1 = full second turn, 50 = 1 FixedUpdate turn")]
    public float maxTurnSpeed = 25;
    public bool isDash;
    public float minSpeed;
    [Header("Vertical")]
    public float gravity = 9.81f;
    public float terminalVelocity = 100f;
    public bool flatGravity = false;
    public PlayerAirborneMovement fallState;
    public float fallStateThreshold = 0;
    [ToggleGroup("Upwards", nameof(jumpHeight), nameof(jumpPower), nameof(jumpMinHeight), nameof(allowMidFall))]
    public bool upwards;
    [HideProperty] public float jumpHeight;
    [HideProperty] public float jumpPower;
    [HideProperty] public float jumpMinHeight;
    [HideField(nameof(upwards))] public bool forceDownwards;
    [HideProperty, ShowField(nameof(upwards))] public bool allowMidFall = true;

    protected float targetMinHeight;
    protected float targetHeight;



    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = playerMovementBody.currentSpeed;
        Vector3 currentDirection = playerMovementBody.currentDirection;

        if (!isDash) HorizontalMain (Time.fixedDeltaTime / 0.02f, ref currentSpeed, ref currentDirection, playerController.camAdjustedMovement);
        else HorizontalCharge       (Time.fixedDeltaTime / 0.02f, ref currentSpeed, ref currentDirection, playerController.camAdjustedMovement);

        playerMovementBody.currentDirection = currentDirection;
        playerMovementBody.currentSpeed = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        resultX = literalDirection.x;
        resultZ = literalDirection.z;
    }
    public override void VerticalMovement(out float? result)
    {
        result = ApplyGravity(gravity, terminalVelocity, flatGravity);
        if (upwards) VerticalUpwards(ref result);
        else if (playerMovementBody.velocity.y <= fallStateThreshold) Fall(ref result);
    }

    private void HorizontalMain(float deltaTime, ref float currentSpeed, ref Vector3 currentDirection, Vector3 control)
    {
        Vector3 controlDirection = control.normalized;
        float controlMag = control.magnitude;

        if (controlMag > 0)
        {
            float Dot = Vector3.Dot(controlDirection, currentDirection);

            if (maxTurnSpeed > 0)
                currentDirection = Vector3.RotateTowards(currentDirection, controlDirection, maxTurnSpeed * Mathf.PI * Time.fixedDeltaTime, 0);

            currentSpeed *= Dot;
            if (currentSpeed < maxSpeed)
                currentSpeed = (currentSpeed + (controlMag * acceleration)).Max(maxSpeed) * deltaTime;
            else if (currentSpeed > maxSpeed)
                currentSpeed = (currentSpeed - (controlMag * decceleration)).Min(maxSpeed) * deltaTime;

        }
        else currentSpeed -= currentSpeed * stopping * deltaTime;
    }
    private void HorizontalCharge(float deltaTime, ref float currentSpeed, ref Vector3 currentDirection, Vector3 control)
    {
        Vector3 controlDirection = control.normalized;
        float controlMag = control.magnitude;


        if (controlMag > 0.1f)
        {
            if (currentSpeed < maxSpeed)
                currentSpeed = (currentSpeed + (controlMag * acceleration)).Max(maxSpeed) * deltaTime;
        }
        else
        {
            if (currentSpeed < minSpeed)
                currentSpeed = (currentSpeed + (controlMag * acceleration)).Max(maxSpeed) * deltaTime;
            if (currentSpeed > minSpeed)
                currentSpeed = (currentSpeed - (controlMag * decceleration)).Min(maxSpeed) * deltaTime;
        }

        if (maxTurnSpeed > 0)
            currentDirection = Vector3.RotateTowards(currentDirection, controlDirection, maxTurnSpeed * Mathf.PI * Time.fixedDeltaTime, 0);
        playerMovementBody.currentDirection = currentDirection;
        playerMovementBody.currentSpeed = currentSpeed;


    }


    private void VerticalUpwards(ref float? Y)
    {
        if (playerMovementBody.jumpPhase == 0 && transform.position.y >= targetMinHeight) playerMovementBody.jumpPhase = 1;
        if (playerMovementBody.jumpPhase == 1 && transform.position.y >= targetHeight) playerMovementBody.jumpPhase = 2;

        if (playerMovementBody.jumpPhase < 2) Y = jumpPower;
        if ((playerMovementBody.velocity.y <= fallStateThreshold) || 
            (allowMidFall && !input.jump.IsPressed()))
            Fall(ref Y);

    }

    private void Fall(ref float? Y)
    {
        if (playerMovementBody.velocity.y > fallStateThreshold) Y = fallStateThreshold;
        playerMovementBody.jumpPhase = 3;
        if (fallState != null) fallState.Enter();
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        if (!isFinal) return;

        if (!upwards && forceDownwards) playerMovementBody.VelocitySet(y: playerMovementBody.velocity.y.Max(0));
        if (!upwards || (playerMovementBody.jumpPhase > 0 && !isDash)) return;

        playerMovementBody.jumpPhase = 0;
        if (isDash) playerMovementBody.currentSpeed = maxSpeed;

        if (!upwards) return;

        playerMovementBody.VelocitySet(y: jumpPower);
        targetMinHeight = transform.position.y + jumpMinHeight;
        targetHeight = (transform.position.y + jumpHeight) - (jumpPower.P()) / (2 * gravity);
        if (targetHeight <= transform.position.y)
        {
            playerMovementBody.VelocitySet(y: Mathf.Sqrt(2 * gravity * jumpHeight));
            targetMinHeight = transform.position.y;
        }

#if UNITY_EDITOR
        playerMovementBody.jumpMarkers = new()
        {
            transform.position,
            transform.position + Vector3.up * targetHeight,
            transform.position + Vector3.up * jumpHeight
        };
#endif
    }
    //public override void OnExit(State next) => body.jumpPhase = -1;

    public void Enter() => state.TransitionTo();
    public void BeginJump()
    {
        playerMovementBody.GroundStateChange(false);
        state.TransitionTo();
    }
    public void BeginJump(float power, float height, float minHeight)
    {
        if (!upwards) throw new System.Exception("This isn't an Upward Item.");
        jumpPower = power;
        jumpHeight = height;
        jumpMinHeight = minHeight;

        state.TransitionTo();
    }
}
