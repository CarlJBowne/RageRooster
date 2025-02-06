using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;

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
    [ToggleGroup("Upwards", nameof(jumpHeight), nameof(jumpPower), nameof(jumpMinHeight), nameof(allowMidFall))]
    public bool upwards;
    [HideInInspector] public float jumpHeight;
    [HideInInspector] public float jumpPower;
    [HideInInspector] public float jumpMinHeight;
    public bool allowMidFall = true;

    protected float targetMinHeight;
    protected float targetHeight;



    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = body.currentSpeed;
        Vector3 currentDirection = body.currentDirection;

        if (!isDash) HorizontalMain (Time.fixedDeltaTime / 0.02f, ref currentSpeed, ref currentDirection, controller.camAdjustedMovement);
        else HorizontalCharge       (Time.fixedDeltaTime / 0.02f, ref currentSpeed, ref currentDirection, controller.camAdjustedMovement);

        body.currentDirection = currentDirection;
        body.currentSpeed = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        resultX = literalDirection.x;
        resultZ = literalDirection.z;
    }
    public override void VerticalMovement(out float? result)
    {
        result = ApplyGravity(gravity, terminalVelocity, flatGravity);
        if (upwards) VerticalUpwards(ref result, ref body.jumpPhase);
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
        body.currentDirection = currentDirection;
        body.currentSpeed = currentSpeed;


    }


    private void VerticalUpwards(ref float? Y, ref int phase)
    {
        if (phase == 0 && transform.position.y >= targetMinHeight) phase = 1;
        if (phase == 1 && transform.position.y >= targetHeight) phase = 2;
        if (phase == 2 && body.velocity.y < 0) phase = 3;

        if (phase < 2) Y = jumpPower;
        if (body.velocity.y <= 0 || (allowMidFall && phase > 0 && !input.jump.IsPressed()))
        {
            if (body.velocity.y > 0) Y = 0;
            phase = 3;
            if (fallState != null) fallState.Begin();
        }

    }

    public override void OnEnter(State prev)
    {
        if (!upwards || body.jumpPhase != -1) return;
        body.jumpPhase = 0;
        body.VelocitySet(y: jumpPower);
        if (jumpPower <= 0) return;
        targetMinHeight = transform.position.y + jumpMinHeight;
        targetHeight = (transform.position.y + jumpHeight) - (jumpPower.P()) / (2 * gravity);
        if (targetHeight <= transform.position.y)
        {
            body.VelocitySet(y: Mathf.Sqrt(2 * gravity * jumpHeight));
            targetMinHeight = transform.position.y;
        }

#if UNITY_EDITOR
        body.jumpMarkers = new()
        {
            transform.position,
            transform.position + Vector3.up * targetHeight,
            transform.position + Vector3.up * jumpHeight
        };
#endif
    }
    public override void OnExit(State next) => body.jumpPhase = -1;

    public void Begin() => state.TransitionTo();
    public void BeginJump()
    {
        body.jumpPhase = -1;
        if (isDash) body.currentSpeed = maxSpeed;
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
