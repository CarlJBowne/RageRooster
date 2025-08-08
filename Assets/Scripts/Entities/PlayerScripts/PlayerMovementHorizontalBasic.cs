using SLS.StateMachineH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class PlayerMovementHorizontalBasic : PlayerMovementEffector
{
    public float acceleration;
    public float decceleration;
    public float maxSpeed;
    public float stopping = 0.75f;
    [Tooltip("1 = full second turn, 50 = 1 FixedUpdate turn")]
    public float maxTurnSpeed = 25;
    public bool forceOutward;
    public float minSpeed;

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        float currentSpeed = playerMovementBody.CurrentSpeed;
        Vector3 currentDirection = playerMovementBody.direction;

        if (!forceOutward) HorizontalMain(ref currentSpeed, ref currentDirection, playerController.camAdjustedMovement, Time.fixedDeltaTime * 50);
        else HorizontalCharge(ref currentSpeed, ref currentDirection, playerController.camAdjustedMovement, Time.fixedDeltaTime * 50);

        playerMovementBody.CurrentSpeed = currentSpeed;

        Vector3 literalDirection = transform.forward * currentSpeed;

        resultX = literalDirection.x;
        resultZ = literalDirection.z;
    }

    protected virtual void HorizontalMain(ref float currentSpeed, ref Vector3 currentDirection, Vector3 control, float deltaTime)
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
    protected virtual void HorizontalCharge(ref float currentSpeed, ref Vector3 currentDirection, Vector3 control, float deltaTime)
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


    protected override void OnEnter(State prev, bool isFinal)
    {
        if (!isFinal) return;
        if (forceOutward) playerMovementBody.CurrentSpeed = maxSpeed;
    }





}