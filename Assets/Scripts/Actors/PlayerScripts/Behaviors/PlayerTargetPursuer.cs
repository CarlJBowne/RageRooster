using DG.Tweening;
using SLS.StateMachineH;
using SLS.StateMachineH.Timelines;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities.Xtensions;
using Utilities.Xtensions.Unity;

public class PlayerTargetPursuer : StateTimeline
{
    public float length;
    public AnimationCurve forwardSpeedCurve;
    public AnimationCurve forwardSpeedInfluenceCurve;
    public AnimationCurve turningSpeedCurve;
    public AnimationCurve verticalShiftCurve;
    public float closeDistance = .5f;

    private Target target;
    private Vector3 targetPosition;
    [SerializeField, Hide] private SLS.StateMachineH.Timelines.TimedMovementAffector failedBackup;

    protected override void OnSetup()
    {
        base.OnSetup();
        failedBackup = GetComponent<SLS.StateMachineH.Timelines.TimedMovementAffector>();
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        if (TargetingManager.GetMeleeTarget() != null)
        {
            target = TargetingManager.GetMeleeTarget();
            if (failedBackup != null) failedBackup.overrideOff = true;
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

        if (turningSpeed > 0) Player.MovementBody.DirectionSet((target.position - Player.Transform.position).XZ(), turningSpeed * Time.fixedDeltaTime);

        float targetForwardSpeed = Player.MovementBody.velocity.sqrMagnitudeH;


        if (forwardSpeedInfluence > 0f)
        {
            targetForwardSpeed = Mathf.Lerp(targetForwardSpeed, forwardSpeed, forwardSpeedInfluence * delta
                * (Vector3.Distance(Player.Position, targetPosition) > closeDistance).Int());
        }

        Player.MovementBody.velocity.x = (targetPosition - Player.Position).x * targetForwardSpeed;
        Player.MovementBody.velocity.z = (targetPosition - Player.Position).z * targetForwardSpeed;

        Player.MovementBody.velocity.y = verticalShift > 0f
            ? verticalShift * (targetPosition.y - Player.Position.y).Sign()
            : Player.MovementBody.velocity.y;

        if (elapsedTime >= length) End();
    }

    private void SampleCurve(AnimationCurve C, out float res) => res = C.Evaluate(elapsedTime);


    protected override void OnEnd() => target = null;
}
