using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;

public class PlayerHellcopterMovement : PlayerMovementEffector
{
    [Header("Horizontal")]
    public float acceleration;
    public float decceleration;
    public float maxSpeed;
    public float stopping = 0.75f;
    [Tooltip("1 = full second turn, 50 = 1 FixedUpdate turn")]
    public float maxTurnSpeed = 25;
    public bool forceOutward;
    public float minSpeed;
    [Header("Vertical")]

    public int defaultPhase;
    public float gravity = 9.81f;
    public float terminalVelocity = 100f;
    public bool flatGravity = false;
    public PlayerHellcopterMovement fallState;
    public float fallStateThreshold = 0;


    public bool upwards;

    protected float targetHeight;
    VolcanicVent currentVent = null;



    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = playerMovementBody.CurrentSpeed;
        Vector3 currentDirection = playerMovementBody.currentDirection;

        if (!forceOutward) HorizontalMain(ref currentSpeed, ref currentDirection, playerController.camAdjustedMovement, Time.fixedDeltaTime * 50);
        else HorizontalCharge(ref currentSpeed, ref currentDirection, playerController.camAdjustedMovement, Time.fixedDeltaTime * 50);

        playerMovementBody.CurrentSpeed = currentSpeed;

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

    private void HorizontalMain(ref float currentSpeed, ref Vector3 currentDirection, Vector3 control, float deltaTime)
    {
        Vector3 controlDirection = control.normalized;
        float controlMag = control.magnitude;

        if (controlMag > 0)
        {
            float Dot = Vector3.Dot(controlDirection, currentDirection);

            if (maxTurnSpeed > 0) playerMovementBody.DirectionSet(maxTurnSpeed);

            currentSpeed *= Dot;
            if (currentSpeed < maxSpeed)
                currentSpeed = currentSpeed.MoveUp(controlMag * acceleration * deltaTime, maxSpeed);
            else if (currentSpeed > maxSpeed)
                currentSpeed = currentSpeed.MoveDown(controlMag * decceleration * deltaTime, maxSpeed);

        }
        else currentSpeed = currentSpeed > .01f ? currentSpeed.MoveTowards(currentSpeed * stopping * deltaTime, 0) : 0;
    }
    private void HorizontalCharge(ref float currentSpeed, ref Vector3 currentDirection, Vector3 control, float deltaTime)
    {
        Vector3 controlDirection = control.normalized;
        float controlMag = control.magnitude;


        if (controlMag > 0.1f)
        {
            if (currentSpeed < maxSpeed)
                currentSpeed = currentSpeed.MoveUp(controlMag * acceleration * deltaTime, maxSpeed);
        }
        else
        {
            if (currentSpeed < minSpeed)
                currentSpeed = currentSpeed.MoveUp(controlMag * acceleration * deltaTime, minSpeed);
            if (currentSpeed > minSpeed)
                currentSpeed = currentSpeed.MoveDown(controlMag * decceleration * deltaTime, maxSpeed);
        }

        if (maxTurnSpeed > 0) playerMovementBody.DirectionSet(maxTurnSpeed); 
        playerMovementBody.CurrentSpeed = currentSpeed;


    }


    private void VerticalUpwards(ref float? Y)
    {
        if (playerMovementBody.jumpPhase == 1)
        {
            Y = currentVent.hellcopterSpeed;
            if (transform.position.y >= targetHeight) playerMovementBody.jumpPhase = 2;
        } 
        else if (playerMovementBody.jumpPhase == 2 && playerMovementBody.velocity.y <= fallStateThreshold) Fall(ref Y);

    }

    private void Fall(ref float? Y)
    {
        if (playerMovementBody.velocity.y > fallStateThreshold) Y = fallStateThreshold;
        playerMovementBody.jumpPhase = 3;
        if (fallState != null) fallState.Enter();
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        if (!isFinal) return;

        playerMovementBody.GroundStateChange(false);

        int nextJumpPhase = defaultPhase;
        if (nextJumpPhase == -1)
        {
            nextJumpPhase = playerMovementBody.jumpPhase;
            if (nextJumpPhase == -1) nextJumpPhase = 0;
        }
        if (defaultPhase == 0) defaultPhase = 1;

        playerMovementBody.jumpPhase = nextJumpPhase;
        switch (nextJumpPhase)
        {
            case 0: break;
            case 1: StartFrom1(); break;
            case 2: break;
            case 3: StartFrom3(); break;
        }

        void StartFrom1()
        {
            if (forceOutward) playerMovementBody.CurrentSpeed = maxSpeed;

            if (!upwards) return;

            currentVent = playerMovementBody.currentVent;
            targetHeight = currentVent.transform.position.y + currentVent.hellcopterTargetHeight;

            playerMovementBody.VelocitySet(y: currentVent.hellcopterSpeed);
            targetHeight = (currentVent.transform.position.y + currentVent.hellcopterTargetHeight) - (currentVent.hellcopterSpeed.P()) / (2 * gravity);
            if (targetHeight <= transform.position.y)
            {
                playerMovementBody.VelocitySet(y: Mathf.Sqrt(2 * gravity * currentVent.hellcopterTargetHeight));
            }

#if UNITY_EDITOR
            playerMovementBody.jumpMarkers = new()
                {
                    transform.position,
                    transform.position + Vector3.up * targetHeight
                };
#endif
        }
        void StartFrom3()
        {
            playerMovementBody.VelocitySet(y: playerMovementBody.velocity.y.Max(0));
        }
    }

    public void Enter() => State.Enter();
}
