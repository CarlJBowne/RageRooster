using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using SLS.StateMachineH.Timelines;
using TMPro;
using UltEvents;
using UnityEngine;

public class PlayerGrabAction : PlayerStateBehavior
{
    [SerializeReference] public AnimationAction initialAnimation;
    public AnimationCurve forwardSpeedCurve;
    public AnimationCurve forwardSpeedInfluenceCurve;
    public AnimationCurve turningSpeedCurve;
    public AnimationCurve verticalShiftCurve;
    public float horizontalThreshold;
    public float verticalThreshold;
    public float directionalThreshold;
    public float maxAttemptTime;
    public TimedMovementAffector failMissedReturn;
    public TimedMovementAffector failBlockedReturn;
    //public TimedMovementAffector passthroughReturn;                      NOTE. GRABBABLE SWITCHES DOES NOT WORK CORRECTLY YET.
    [SerializeReference] public AnimationAction grabAnimation;
    public float grabAnimationTime;
    public bool moveToTargetPosition;
    public bool considerDropLaunch = false;
    public UltEvent finalSuccessReturn;

    float elapsedTime = 0;
    bool secondPhase = false;
    Target selectedTarget;
    Grabbable selectedGrabbable;
    Vector3 storedTargetPosition;
    Vector3 playerPhaseTwoStatePosition;

    protected override void OnAwake() => grabAnimationTime = 1 / grabAnimationTime;

    public void BeginGrabAttempt()
    {
        elapsedTime = 0f;
        secondPhase = false;
        selectedTarget = TargetingManager.MeleeChannel.CurrentTarget;
        if (selectedTarget == null)
        {
            EndCleanup();
            failMissedReturn.State.Enter();
            return;
        }
        Grabbable.Attempt(selectedTarget.gameObject, out Grabbable.GrabResult grabResult, out Grabbable grabbable);
        if (grabbable == null || grabResult is Grabbable.GrabResult.Missed)
        {
            EndCleanup();
            failMissedReturn.State.Enter();
            return;
        }

        selectedGrabbable = grabbable;

        State.Enter();
        initialAnimation.Do(Player.Animator);
    }

    protected override void OnFixedUpdate()
    {
        if (!secondPhase)
        {
            elapsedTime += Time.fixedDeltaTime;

            if (selectedTarget != null) storedTargetPosition = selectedTarget.position;

            float horizontalDistance = (Player.Center - storedTargetPosition).XZ().magnitude;
            float verticalDistance = (Player.Center.y - storedTargetPosition.y).Abs();
            float angleDifference = Vector3.Angle(Player.Forward, (storedTargetPosition - Player.Center).XZ());

            SampleCurve(forwardSpeedCurve, out float forwardSpeed);
            SampleCurve(forwardSpeedInfluenceCurve, out float forwardSpeedInfluence);
            SampleCurve(turningSpeedCurve, out float turningSpeed);
            SampleCurve(verticalShiftCurve, out float verticalShift);

            if (turningSpeed > 0) Player.MovementBody.DirectionSet((storedTargetPosition - Player.Center).XZ(), turningSpeed);

            Vector3 targetVelocity = Player.MovementBody.velocity;
            float targetForwardSpeed = Player.MovementBody.CurrentSpeed;


            if (forwardSpeedInfluence > 0f) targetForwardSpeed = horizontalDistance > horizontalThreshold
                    ? Mathf.Lerp(targetForwardSpeed, forwardSpeed, forwardSpeedInfluence)
                    : 0;

            Player.MovementBody.CurrentSpeed = targetForwardSpeed;
            targetVelocity.x = Player.Forward.x * targetForwardSpeed;
            targetVelocity.z = Player.Forward.z * targetForwardSpeed;

            targetVelocity.y = verticalShift > 0f && verticalDistance > 0 ? verticalShift : Player.MovementBody.velocity.y;

            Player.MovementBody.VelocitySet(targetVelocity.x, targetVelocity.y, targetVelocity.z);

            if ((horizontalDistance <= horizontalThreshold && verticalDistance <= verticalThreshold && angleDifference <= directionalThreshold) || elapsedTime > maxAttemptTime) //CHANGE PHASE
            {
                if (selectedTarget == null || selectedGrabbable == null)
                {
                    failMissedReturn.State.Enter();
                    EndCleanup();
                    return;
                }
                Grabbable.GrabResult grabResult = selectedGrabbable.GetGrabbable();
                if (grabResult is Grabbable.GrabResult.Missed)
                {
                    failMissedReturn.State.Enter();
                    EndCleanup();
                    return;
                }

                storedTargetPosition = selectedTarget.position + (Player.Position - Player.Center);
                playerPhaseTwoStatePosition = Player.Position;
                elapsedTime = 0;
                Player.Grabber.Grab(selectedGrabbable);
                secondPhase = true;

            }
        }
        else
        {
            elapsedTime += Time.fixedDeltaTime * grabAnimationTime;

            Vector3 newPosition = Vector3.Lerp(playerPhaseTwoStatePosition, storedTargetPosition, elapsedTime);
            Player.MovementBody.Position = newPosition;

            if (elapsedTime >= 1)
            {
                if (Player.StateMachine.Airborne && Upgrades.Active.dropLaunch && Input.Grab.IsPressed())
                    Player.StateMachine.DropLaunch.Enter();
                else finalSuccessReturn.Invoke();
                EndCleanup();
            }
        }

    }

    public void EndCleanup()
    {
        selectedTarget = null;
        selectedGrabbable = null;
        secondPhase = false;
    }
    private void SampleCurve(AnimationCurve C, out float res) => res = C.Evaluate(elapsedTime);
}
