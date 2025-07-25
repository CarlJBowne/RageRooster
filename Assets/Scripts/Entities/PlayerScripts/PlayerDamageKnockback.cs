using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Obsolete]
public class PlayerDamageKnockback : PlayerMovementEffector
{
    public AnimationCurve backwards;
    public AnimationCurve upwards;
    public float duration;

    private float currentTime;
    private Vector3 backwardsVector;

    public override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        currentTime = 0;
        playerMovementBody.grounded = false;
        playerMovementBody.VelocitySet(y: upwards.Evaluate(0));
    }
    public override void OnExit(State next)
    {
        base.OnExit(next);
    }

    public override void OnFixedUpdate()
    {
        currentTime += Time.deltaTime;
        if (currentTime > duration) currentTime = duration;
        base.OnFixedUpdate();
    }

    public override void HorizontalMovement(out float? resultX, out float? resultZ)
    {
        resultX = backwardsVector.x * backwards.Evaluate(currentTime / duration);
        resultZ = backwardsVector.z * backwards.Evaluate(currentTime / duration);
    }

    public override void VerticalMovement(out float? result)
    {
        result = upwards.Evaluate(currentTime / duration);
    }
}
