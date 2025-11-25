using DG.Tweening;
using SLS.StateMachineH;
using SLS.StateMachineH.Timelines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerTargetPursuer : StateTimeline
{
    public float length;
    public AnimationCurve forwardSpeedCurve;
    public AnimationCurve forwardSpeedInfluenceCurve;
    public AnimationCurve turningSpeedCurve;
    public AnimationCurve verticalShiftCurve;
    public float closeDistance = .5f;

    private MeleeTarget target;
    private Vector3 targetPosition;
    [SerializeField, HideInInspector] private SLS.StateMachineH.Timelines.TimedMovementAffector failedBackup;

    protected override void OnSetup()
    {
        base.OnSetup();
        failedBackup = GetComponent<SLS.StateMachineH.Timelines.TimedMovementAffector>();
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        if(TargetingManager.GetMeleeTarget() != null)
        {
            target = TargetingManager.GetMeleeTarget();
            if(failedBackup != null) failedBackup.overrideOff = true;
            Begin();
        }
        else
        {
            if (failedBackup != null) failedBackup.overrideOff = false;
        }
    }

    protected override void OnTick(float delta)
    {
        if (target != null) targetPosition = target.position - (target.position - Player.Position).normalized * closeDistance;

        SampleCurve(forwardSpeedCurve, out float forwardSpeed);
        SampleCurve(forwardSpeedInfluenceCurve, out float forwardSpeedInfluence);
        SampleCurve(turningSpeedCurve, out float turningSpeed);
        SampleCurve(verticalShiftCurve, out float verticalShift);

        if (turningSpeed > 0) Player.MovementBody.DirectionSet((target.position - Player.Transform.position).XZ(), turningSpeed);

        Vector3 targetVelocity = Player.MovementBody.velocity;
        float targetForwardSpeed = Player.MovementBody.CurrentSpeed;


        if(forwardSpeedInfluence > 0f)
        {
            targetForwardSpeed = Mathf.Lerp(targetForwardSpeed, forwardSpeed, forwardSpeedInfluence * delta 
                * (Vector3.Distance(Player.Position, targetPosition) > closeDistance).Int());
        }

        Player.MovementBody.CurrentSpeed = targetForwardSpeed;
        targetVelocity.x = (targetPosition - Player.Position).x * targetForwardSpeed;
        targetVelocity.z = (targetPosition - Player.Position).z * targetForwardSpeed;

        targetVelocity.y = verticalShift > 0f 
            ? verticalShift * (targetPosition.y - Player.Position.y).Sign() 
            : Player.MovementBody.velocity.y;

        Player.MovementBody.VelocitySet(targetVelocity.x, targetVelocity.y, targetVelocity.z);

        if (elapsedTime >= length) End();
    }

    private void SampleCurve(AnimationCurve C, out float res) => res = C.Evaluate(elapsedTime);


    protected override void OnEnd() => target = null;
}
