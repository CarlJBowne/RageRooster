using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementNegater : PlayerMovementEffector
{
    public enum NegateType { InstantNegate, SmoothNegate, Dampen, SmoothDampen, Gravity, None}

    public NegateType horizontalNegateType;
    public NegateType verticalNegateType;

    public float rate;
    public float dampenPoint;
    public bool savePriorVelocity;
    public float gravity;
    public float terminalVelocity;


    protected bool disabled;
    protected Vector3 savedVelocity;
    protected float savedHorizontalSpeed;
    protected int savedJumpPhase;

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        resultX = null;
        resultZ = null;
        if (disabled) return;

        switch (horizontalNegateType)
        {
            case NegateType.InstantNegate:
                resultX = 0;
                resultZ = 0;
                playerMovementBody.currentSpeed = 0;
                break;
            case NegateType.SmoothNegate:
                resultX = Mathf.MoveTowards(playerMovementBody.velocity.x, 0, rate * Time.deltaTime);
                resultZ = Mathf.MoveTowards(playerMovementBody.velocity.z, 0, rate * Time.deltaTime);
                playerMovementBody.currentSpeed = Mathf.MoveTowards(playerMovementBody.currentSpeed, 0, rate * Time.deltaTime);
                break;
            case NegateType.Dampen:
                resultX = Mathf.Clamp(playerMovementBody.velocity.x, -dampenPoint, dampenPoint);
                resultZ = Mathf.Clamp(playerMovementBody.velocity.z, -dampenPoint, dampenPoint);
                playerMovementBody.currentSpeed = Mathf.Min(playerMovementBody.currentSpeed, dampenPoint);
                break;
            case NegateType.SmoothDampen:
                resultX = Mathf.MoveTowards(playerMovementBody.velocity.x, dampenPoint * playerMovementBody.velocity.x.Sign(), rate * Time.deltaTime);
                resultZ = Mathf.MoveTowards(playerMovementBody.velocity.z, dampenPoint * playerMovementBody.velocity.z.Sign(), rate * Time.deltaTime);
                playerMovementBody.currentSpeed = Mathf.MoveTowards(playerMovementBody.currentSpeed, dampenPoint, rate * Time.deltaTime);
                break;
            default:
                break;
        }
    }
    public override void VerticalMovement(out float? resultY)
    {
        resultY = null;
        if (disabled) return;

        switch (verticalNegateType) 
        {
            case NegateType.InstantNegate:
                resultY = 0;
                break;
            case NegateType.SmoothNegate:
                resultY = Mathf.MoveTowards(playerMovementBody.velocity.y, 0, rate * Time.deltaTime);
                break;
            case NegateType.Dampen:
                resultY = Mathf.Clamp(playerMovementBody.velocity.y, -dampenPoint, dampenPoint);
                break;
            case NegateType.SmoothDampen:
                resultY = Mathf.MoveTowards(playerMovementBody.velocity.y, (dampenPoint * playerMovementBody.velocity.y.Sign()), rate * Time.deltaTime);
                break;
            case NegateType.Gravity:
                resultY = ApplyGravity(gravity, terminalVelocity);
                break;
            default:
                break;
        }
    }
    public override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        disabled = false;
        if (verticalNegateType == NegateType.Gravity) playerMovementBody.VelocitySet(y: 0);
        if (savePriorVelocity)
        {
            savedVelocity = playerMovementBody.velocity;
            savedHorizontalSpeed = playerMovementBody.currentSpeed;
            savedJumpPhase = playerMovementBody.jumpPhase;
        }
    }
    public override void OnExit(State next)
    {
        if (savePriorVelocity)
        {
            playerMovementBody.velocity = savedVelocity;
            playerMovementBody.currentSpeed = savedHorizontalSpeed;
            playerMovementBody.jumpPhase = savedJumpPhase;
        }
    }
}
