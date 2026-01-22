using System;
using System.Collections;
using UnityEngine;


public class PlayerProjectile : AttackSourceSingle
{
    //Config
    public float speed;
    [Range(0,1)] public float velocityPredictionFactor;
    [Range(0,90)] public float homingPerSecond;
    [Range(0, 180)] public float loseTargetAngle;
    [Range(0, 5)] public float initialHomingDuration;
    [Range(9, 90)] public float initialHomingPerSecond;
    public bool applyActualAttack;
    public bool rotateActualBody;

    //Components

    //Data
    RangedTarget activeTarget;
    Vector3 activeVelocity;
    float timeFlying;
    bool lostTarget;
    public bool active => isActiveAndEnabled;


    public void Send(RangedTarget target, Action<Vector3> rotateAction, Transform initPosition, Transform fallBackTargetPosition)
    {
        activeTarget = target;
        lostTarget = false;
        timeFlying = 0;
        transform.position = initPosition.position;
        if(velocityPredictionFactor > 0 && target != null)
        {
            Vector3 predictedPosition = target.position + target.PredictFuturePosition(initPosition.position, speed); //(Placeholder)
            activeVelocity = (Vector3.Lerp(target.position, predictedPosition, velocityPredictionFactor) - transform.position).normalized * speed;
            rotateAction(predictedPosition);
        }
        else if (target != null)
        {
            activeVelocity = (target.position - transform.position).normalized * speed;
            rotateAction(target.position);
        }
        else
        {
            activeVelocity = (fallBackTargetPosition.position - transform.position).normalized * speed;
            rotateAction(fallBackTargetPosition.position);
        }
    }

    private void FixedUpdate()
    {
        if(activeTarget != null && !lostTarget && (homingPerSecond > 0 || initialHomingPerSecond > 0))
        {
            Vector3 toTarget = (activeTarget.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(activeVelocity, toTarget);
            if(angleToTarget > loseTargetAngle) lostTarget = true;
            else
            {
                float currentHomingPerSecond = (timeFlying < initialHomingDuration) ? initialHomingPerSecond : homingPerSecond;
                activeVelocity = Vector3.RotateTowards(activeVelocity, toTarget, currentHomingPerSecond * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f).normalized;
                if (rotateActualBody) transform.rotation = Quaternion.LookRotation(activeVelocity);
            }
        }
        transform.position += speed * Time.fixedDeltaTime * activeVelocity;
    }


}