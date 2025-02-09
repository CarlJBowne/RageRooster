using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovementNegater : PlayerMovementEffector
{
    public enum NegateType { InstantNegate, SmoothNegate, Dampen, SmoothDampen, None}

    public NegateType horizontalNegateType;
    public NegateType verticalNegateType;

    public float rate;
    public float dampenPoint;
    public bool savePriorVelocity;

    private bool disabled;
    private Vector3 savedVelocity;
    private float savedHorizontalSpeed;
    private int savedJumpPhase;

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
                body.currentSpeed = 0;
                break;
            case NegateType.SmoothNegate:
                resultX = Mathf.MoveTowards(body.velocity.y, 0, rate);
                resultZ = Mathf.MoveTowards(body.velocity.y, 0, rate);
                body.currentSpeed = Mathf.MoveTowards(body.currentSpeed, 0, rate);
                break;
            case NegateType.Dampen:
                resultX = Mathf.Clamp(body.velocity.y, -dampenPoint, dampenPoint);
                resultZ = Mathf.Clamp(body.velocity.y, -dampenPoint, dampenPoint);
                body.currentSpeed = Mathf.Min(body.currentSpeed, dampenPoint);
                break;
            case NegateType.SmoothDampen:
                resultX = Mathf.MoveTowards(body.velocity.y, dampenPoint * body.velocity.y.Sign(), rate);
                resultZ = Mathf.MoveTowards(body.velocity.y, dampenPoint * body.velocity.y.Sign(), rate);
                body.currentSpeed = Mathf.MoveTowards(body.currentSpeed, dampenPoint, rate);
                break;
            default:
                break;
        }
    }
    public override void VerticalMovement(out float? result)
    {
        result = null;
        if (disabled) return;

        switch (horizontalNegateType)
        {
            case NegateType.InstantNegate:
                result = 0;
                break;
            case NegateType.SmoothNegate:
                result = Mathf.MoveTowards(body.velocity.y, 0, rate);
                break;
            case NegateType.Dampen:
                result = Mathf.Clamp(body.velocity.y, -dampenPoint, dampenPoint);
                break;
            case NegateType.SmoothDampen:
                result = Mathf.MoveTowards(body.velocity.y, (dampenPoint * body.velocity.y.Sign()), rate);
                break;
            default:
                break;
        }
    }
    public override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        disabled = false;
        if (savePriorVelocity)
        {
            savedVelocity = body.velocity;
            savedHorizontalSpeed = body.currentSpeed;
            savedJumpPhase = body.jumpPhase;
        }
    }
    public override void OnExit(State next)
    {
        if (savePriorVelocity)
        {
            body.velocity = savedVelocity;
            body.currentSpeed = savedHorizontalSpeed;
            body.jumpPhase = savedJumpPhase;
        }
    }
}
