using SLS.StateMachineH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;
using static RageRooster.Player.Services;

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

    public override bool ForwardMovement(out float resultF)
    {
        float result = Player.MovementBody.Velocity.f;
        Vector3 currentDirection = Player.MovementBody.Direction;

        if (!forceOutward) HorizontalMain(Time.fixedDeltaTime * 50);
        else HorizontalCharge(Time.fixedDeltaTime * 50);

        void HorizontalMain(float deltaTime)
        {
            Vector3 controlDirection = Player.Controller.camAdjustedMovement.normalized;
            float controlMag = Player.Controller.camAdjustedMovement.magnitude;

            if (controlMag > 0)
            {
                float Dot = Vector3.Dot(controlDirection, currentDirection);

                if (maxTurnSpeed > 0) Player.MovementBody.Direction.Set
                        (Player.Controller.camAdjustedMovement, maxTurnSpeed * Time.fixedDeltaTime);

                result *= Dot;
                if (result < maxSpeed)
                    result = result.MoveUp(controlMag * acceleration * deltaTime, maxSpeed);
                else if (result > maxSpeed)
                    result = result.MoveDown(controlMag * decceleration * deltaTime, maxSpeed);

            }
            else result = result > .01f ? result.MoveTowards(result * stopping * deltaTime, 0) : 0;

        }
        void HorizontalCharge(float deltaTime)
        {
            Vector3 controlDirection = Player.Controller.camAdjustedMovement.normalized;
            float controlMag = Player.Controller.camAdjustedMovement.magnitude;


            if (controlMag > 0.1f)
            {
                if (result < maxSpeed)
                    result = result.MoveUp(controlMag * acceleration * deltaTime, maxSpeed);
            }
            else
            {
                if (result < minSpeed)
                    result = result.MoveUp(controlMag * acceleration * deltaTime, minSpeed);
                if (result > minSpeed)
                    result = result.MoveDown(controlMag * decceleration * deltaTime, maxSpeed);
            }

            if (maxTurnSpeed > 0) Player.MovementBody.Direction.Set
                    (Player.Controller.camAdjustedMovement, maxTurnSpeed * Time.fixedDeltaTime);
            Player.MovementBody.Velocity.f = result;


        }

        Player.MovementBody.Velocity.f = result;

        Vector3 literalDirection = transform.forward * result;

        resultF = result;
        return true;
    }




    protected override void OnEnter(State prev, bool isFinal)
    {
        if (!isFinal) return;
        if (forceOutward) Player.MovementBody.Velocity.f = maxSpeed;
    }





}