using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class PlayerLassoProjectile : PlayerProjectile
{
    //config
    public float pullSpeed;
    public float reachplayerDistance = 1f;
    public float gravity;

    //data
    private bool pullingPhase;
    private Grabbable grabbable;
    private float currentYVelocity;
    private Vector3 currentHDirection;

    public override void Send(Target target, Transform initPosition, Transform fallBackTargetPosition)
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



        // Determine horizontal direction and distance (XZ plane)
        Vector3 targetDelta = trueTargetPos - transform.position;
        currentHDirection = targetDelta.XZ();
        float horizontalDistance = currentHDirection.magnitude;
        currentHDirection.Normalize();

        float yDelta = targetDelta.y + 1; //Make lasso aim slightly high to look more like its looping around the target

        // Compute time to reach target based on horizontal speed
        float expectedTravelTime = (speed > 0f && horizontalDistance > 0f) ? horizontalDistance / speed : 0f;

        // Compute required initial vertical velocity: v_y = dy / t + 0.5 * g * t
        float initYvelocity;
        if (expectedTravelTime > 0f && !float.IsInfinity(expectedTravelTime) && !float.IsNaN(expectedTravelTime))
        {
            initYvelocity = yDelta / expectedTravelTime + 0.5f * gravity * expectedTravelTime;
        }
        else
        {
            // Fallback: if no horizontal travel (or speed is zero), try a simple ballistic estimate:
            // If target is above, give sufficient upward velocity to reach dy under gravity: v = sqrt(2*g*dy)
            // If target is at or below, no initial upward velocity required.
            initYvelocity = yDelta > 0f ? Mathf.Sqrt(2f * gravity * yDelta) : 0f;
        }

        // Store computed values for use in physics updates
        currentYVelocity = initYvelocity;

        if (rotateActualBody) transform.rotation = Quaternion.LookRotation(currentHDirection);

        gameObject.SetActive(true);
    }


    protected override void FixedUpdate()
    {
        if (!pullingPhase) ProjectileUpdate();
        else PullUpdate();
    }

    protected override void ProjectileUpdate()
    {
        float targetSpeed = speed;
        if (activeTarget != null && !lostTarget && (homingPerSecond > 0 || initialHomingPerSecond > 0))
        {
            Vector3 toTarget = (activeTarget.position - transform.position).XZ();
            float distanceLeft = toTarget.magnitude;
            toTarget.Normalize();
            float angleToTarget = Vector3.Angle(currentHDirection, toTarget);
            if (angleToTarget > loseTargetAngle) lostTarget = true;
            else if (angleToTarget > .1f)
            {
                float currentHomingPerSecond = (timeFlying < initialHomingDuration) ? initialHomingPerSecond : homingPerSecond;
                currentHDirection = Vector3.RotateTowards(currentHDirection, toTarget, currentHomingPerSecond * Mathf.Deg2Rad * Time.fixedDeltaTime, 0f).normalized;
                if (rotateActualBody) transform.rotation = Quaternion.LookRotation(currentHDirection);
            }

            if (distanceLeft <= 1.5f) targetSpeed *= distanceLeft * 1.5f;
        }

        //Note, Look into possible homing adjustments to vertical velocity as well for better target tracking.

        // Horizontal movement
        transform.position += targetSpeed * Time.fixedDeltaTime * currentHDirection;
        transform.position += currentYVelocity * Time.fixedDeltaTime * Vector3.up;
        currentYVelocity -= gravity * Time.fixedDeltaTime;

        // Update time flying
        timeFlying += Time.fixedDeltaTime;
    }
    private void PullUpdate()
    {
        transform.position += pullSpeed * Time.fixedDeltaTime * (Player.Position - transform.position).normalized;

        if (grabbable != null) grabbable.transform.position = this.transform.position;

        if (Vector3.Distance(transform.position, Player.Position) <= reachplayerDistance) ReachPlayer();
    }


    protected override void OnTriggerEnter(Collider other) => Contact(other.gameObject);
    public override void Contact(GameObject target)
    {
        if (target == Player.GameObject || pullingPhase) return;
        pullingPhase = true;

        Grabbable.Attempt(target, success => { grabbable = success; grabbable.Grab(); }, null, null);

        Player.SignalManager.FireSignalBasic("LassoPull");
    }

    private void ReachPlayer()
    {
        pullingPhase = false;
        gameObject.SetActive(false);
        Player.SignalManager.FireSignalBasic(grabbable != null ? "LassoReach" : "LassoReachMiss");
        if (grabbable != null) Player.Grabber.Grab(grabbable);
        grabbable = null;
    }
}