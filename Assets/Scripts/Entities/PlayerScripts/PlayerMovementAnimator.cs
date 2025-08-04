using EditorAttributes;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementAnimator : PlayerMovementEffector
{
    [Tooltip("Generally recommended to keep at 0 and have set to 1 in animation so that the CrossFade can automatically smoothly blend the effect."), Range(0,1)]
    public float influence;
    public bool fullStop;

    public float maxSpeed = 0;
    public float minSpeed = 0;
    public float speedChangeRate = 15;
    public float turnability = 10;
    public float verticalAddSpeed;
    public float terminalVelocity = 98.1f;

    [Tooltip("Sets/Lerps the velocity to a specific point rather than adding it.")]
    [Range(0, 1)] public float setVerticalInfluence;
    public float setVerticalVelocity;
    [Tooltip("Only active if locked.")]
    public float defaultGravity;

    [Range(0, 1)] public float worldspaceInfluence;
    public Vector3 worldspaceVelocity;

    [Tooltip("Makes this Movement Effector inoperable no matter the parameters. Must be set by some kind of alternative source, or by an inheriting class.")]
    public bool locked;

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        if(locked)
        {
            base.HorizontalMovement(out resultX, out resultZ);
            return;
        }

        resultX = playerMovementBody.velocity.x;
        resultZ = playerMovementBody.velocity.z;

        if (influence > 0)
        {
            Vector3 controlVector = playerController.camAdjustedMovement;

            Vector3 targetDirection = playerMovementBody.currentDirection;
            float targetSpeed = playerMovementBody.CurrentSpeed;

            if (turnability > 0) targetDirection = Vector3.RotateTowards(targetDirection, controlVector.normalized, turnability * Mathf.PI * Time.fixedDeltaTime, 0);

            targetSpeed = controlVector.sqrMagnitude > 0
                ? targetSpeed.MoveTowards(controlVector.magnitude * speedChangeRate * (Time.deltaTime * 50), maxSpeed)
                : targetSpeed.MoveTowards(speedChangeRate * (Time.deltaTime * 50), minSpeed);

            if (influence == 1)
            {
                playerMovementBody.CurrentSpeed = targetSpeed;
                playerMovementBody.currentDirection = targetDirection;
                resultX = targetDirection.x * targetSpeed;
                resultZ = targetDirection.z * targetSpeed;
            }
            else
            {
                playerMovementBody.CurrentSpeed = Mathf.Lerp(playerMovementBody.CurrentSpeed, targetSpeed, influence);
                playerMovementBody.currentDirection = Vector3.Lerp(playerMovementBody.currentDirection, targetDirection, influence); ;
                resultX = Mathf.Lerp(resultX.Value, targetDirection.x * targetSpeed, influence);
                resultZ = Mathf.Lerp(resultZ.Value, targetDirection.z * targetSpeed, influence);
            }

        }
        if (worldspaceInfluence > 0)
        {
            Vector3 relative = transform.TransformDirection(worldspaceVelocity);
            resultX = worldspaceInfluence == 1
                ? relative.x
                : Mathf.Lerp(resultX.Value, relative.x, worldspaceInfluence);
            resultZ = worldspaceInfluence == 1
                ? relative.z
                : Mathf.Lerp(resultZ.Value, relative.z, worldspaceInfluence);
        }
        if (fullStop)
        {
            resultX = 0;
            resultZ = 0;
        }

    }
    public override void VerticalMovement(out float? result)
    {
        if (locked)
        {
            result = playerMovementBody.velocity.y - defaultGravity * .02f;
            return;
        }

        result = playerMovementBody.velocity.y;

        if (!Mathf.Approximately(verticalAddSpeed, 0)) result = (result.Value + verticalAddSpeed * Time.fixedDeltaTime).Min(-terminalVelocity);
        if(setVerticalInfluence > 0) 
            result = setVerticalInfluence == 1 
                ? setVerticalVelocity 
                : Mathf.Lerp(result.Value, setVerticalVelocity, setVerticalInfluence);
        if (worldspaceInfluence > 0)
        {
            result = worldspaceInfluence == 1 
                ? worldspaceVelocity.y 
                : Mathf.Lerp(result.Value, worldspaceVelocity.y, worldspaceInfluence);
        }
        if (fullStop)
        {
            result = 0;
        }
    }

    protected override void OnExit(State next)
    {
        locked = false;
    }







}
