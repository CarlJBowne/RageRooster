using System;
using System.Collections;
using UnityEngine;


public class PlayerProjectile : AttackSourceSingle
{
    //Config
    public float speed;
    [Range(0, 1)] public float velocityPredictionFactor;
    [Range(0, 90)] public float homingPerSecond;
    [Range(0, 180)] public float loseTargetAngle;
    [Range(0, 5)] public float initialHomingDuration;
    [Range(9, 90)] public float initialHomingPerSecond;
    public bool applyActualAttack;
    public bool rotateActualBody;

    //Components

    //Data
    protected Target activeTarget;
    protected Vector3 activeVelocity;
    protected float timeFlying;
    protected bool lostTarget;
    public bool active => isActiveAndEnabled;


    public virtual void Send(Target target, Transform initPosition, Transform fallBackTargetPosition)
    {
        activeTarget = target;
        lostTarget = false;
        timeFlying = 0;
        transform.position = initPosition.position;
        Vector3 trueTargetPos = velocityPredictionFactor > 0 && target != null
            ? Vector3.Lerp(target.position, target.PredictFuturePosition(initPosition.position, speed), velocityPredictionFactor)
            : target != null
                ? target.position
                : fallBackTargetPosition.position;

        activeVelocity = (trueTargetPos - transform.position).normalized;

        if (rotateActualBody) transform.rotation = Quaternion.LookRotation(activeVelocity);

        gameObject.SetActive(true);
    }

    protected virtual void FixedUpdate() => ProjectileUpdate();

    protected virtual void ProjectileUpdate()
    {
        if (activeTarget != null && !lostTarget && (homingPerSecond > 0 || initialHomingPerSecond > 0))
        {
            Vector3 toTarget = (activeTarget.position - transform.position).normalized;
            float angleToTarget = Vector3.Angle(activeVelocity, toTarget);
            if (angleToTarget > loseTargetAngle) lostTarget = true;
            else if (angleToTarget > .1f)
            {
                float currentHomingPerSecond = (timeFlying < initialHomingDuration) ? initialHomingPerSecond : homingPerSecond;
                activeVelocity = Vector3.RotateTowards(activeVelocity, toTarget, currentHomingPerSecond * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f).normalized;
                if (rotateActualBody) transform.rotation = Quaternion.LookRotation(activeVelocity);
            }
        }
        transform.position += speed * Time.fixedDeltaTime * activeVelocity;
    }

    public override void Contact(GameObject target)
    {
        if (target == Player.GameObject) return;
        if(target.TryGetComponent(out IDamagable targetDamagable)) targetDamagable.Damage(GetAttack());
        gameObject.SetActive(false);
    }
}