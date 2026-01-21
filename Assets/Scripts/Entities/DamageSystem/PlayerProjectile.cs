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


    public void Send(Vector3 initPosition, RangedTarget target, Action<Vector3> initialRotateAction)
    {
        activeTarget = target;
        lostTarget = false;
        transform.position = initPosition;
        timeFlying = 0;
        if(velocityPredictionFactor > 0)
        {
            Vector3 predictedPosition = target.position + Vector3.up; //(Placeholder)
            activeVelocity = (Vector3.Lerp(target.position, predictedPosition, velocityPredictionFactor) - transform.position).normalized * speed;
        }
        else 
        {
            activeVelocity = (target.position - transform.position).normalized * speed;
        }

    }

    private void FixedUpdate()
    {
        if((homingPerSecond > 0 || initialHomingPerSecond > 0) && !lostTarget)
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
        transform.position += activeVelocity * speed * Time.fixedDeltaTime;
    }


}