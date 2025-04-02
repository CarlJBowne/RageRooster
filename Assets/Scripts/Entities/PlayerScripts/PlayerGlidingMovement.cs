using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;

public class PlayerGlidingMovement : PlayerMovementEffector
{
    [Header("Horizontal")]
    public float acceleration;
    public float decceleration;
    public float maxSpeed;
    public float stopping = 0.75f;
    [Tooltip("1 = full second turn, 50 = 1 FixedUpdate turn")]
    public float maxTurnSpeed = 25;
    
    [Header("Vertical")]
    public float gravity = 9.81f;
    public float terminalVelocity = 100f;
    public bool flatGravity = false;
    public PlayerAirborneMovement fallState;
    public bool isVentGlide;
    public float raiseRate;

    protected float targetHeight;
    VolcanicVent currentVent = null;


    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = playerMovementBody.CurrentSpeed;
        Vector3 currentDirection = playerMovementBody.currentDirection;

        HorizontalMain(ref currentSpeed, ref currentDirection, playerController.camAdjustedMovement, Time.fixedDeltaTime * 50);

        playerMovementBody.CurrentSpeed = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        resultX = literalDirection.x;
        resultZ = literalDirection.z;
    }
    public override void VerticalMovement(out float? result)
    {
        if(!isVentGlide || transform.position.y > targetHeight) 
            result = ApplyGravity(gravity, terminalVelocity, flatGravity);
        else if (transform.position.y < targetHeight) 
            result = raiseRate/* * currentVent.transform.up.y*/;
        else result = 0;

        if(!input.jump.IsPressed()) Fall(ref result);

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


    private void Fall(ref float? Y)
    {
        Y = Y.Value.Max(0);
        playerMovementBody.jumpPhase = 3;
        if (fallState != null) fallState.Enter();
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        if (!isFinal) return;

        playerMovementBody.GroundStateChange(false);

        playerMovementBody.VelocitySet(y: playerMovementBody.velocity.y.Max(0));

        if (isVentGlide)
        {
            currentVent = playerMovementBody.currentVent;
            targetHeight = currentVent.transform.position.y + (currentVent.glideHeight/* * currentVent.transform.up.y*/);
        }
    }

    public void Enter() => state.TransitionTo();

}
