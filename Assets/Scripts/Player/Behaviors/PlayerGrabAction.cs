using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using RageRooster.Core.Save;
using SLS.StateMachineH;
using SLS.StateMachineH.Timelines;
using TMPro;
using UltEvents;
using UnityEngine;


namespace RageRooster.Player
{
    public class PlayerGrabAction : StateBehavior
    {
        //[SerializeReference] public AnimationAction initialAnimation;
        public AnimationCurve forwardSpeedCurve;
        public AnimationCurve forwardSpeedInfluenceCurve;
        public AnimationCurve turningSpeedCurve;
        public float instantDirectionSnap = 2;
        public AnimationCurve verticalShiftCurve;
        public float horizontalThresholdAdjustment = 0.05f;
        public float verticalThreshold = 3;
        public float directionalThreshold = 3;
        public float directionalMissThreshold = 12f;
        public float maxAttemptTime = 3;
        public TimedMovementAffector failMissedReturn;
        public TimedMovementAffector failBlockedReturn;
        public State successState;
        public float grabAnimationTime;
        public bool moveToTargetPosition;
        public bool considerDropLaunch = false;
        public UltEvent finalSuccessReturn;

        float elapsedTime = 0;
        int phase = 0;
        //0 = Inactive
        //1 = Pursuing Phase
        //2 = Grabbing Animation
        float horizontalThreshold;
        Target selectedTarget;
        Grabbable selectedGrabbable;
        Vector3 storedTargetPosition;
        float horizontalDistance;
        float verticalDistance;
        float angleDifference;
        Vector3 playerPhaseTwoStartPosition;

        protected override void OnAwake() => grabAnimationTime = 1 / grabAnimationTime;

        public void BeginGrabAttempt()
        {
            elapsedTime = 0f;
            phase = 0;
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
            phase = 1;
            horizontalThreshold = selectedGrabbable.grabRadius + Player.Collider.radius + horizontalThresholdAdjustment;
            Player.MovementBody.Direction.Set((selectedTarget.position - Player.Position).XZ(), instantDirectionSnap);
            //initialAnimation.Do(Player.Animator);
        }

        protected override void OnFixedUpdate()
        {
            if (phase == 1)
            {
                elapsedTime += Time.fixedDeltaTime;

                if (selectedTarget != null) storedTargetPosition = selectedTarget.position;

                horizontalDistance = (Player.Center - storedTargetPosition).XZ().magnitude;
                verticalDistance = (Player.Center.y - storedTargetPosition.y).Abs();
                angleDifference = Vector3.Angle(Player.Forward, (storedTargetPosition - Player.Center).XZ());

                SampleCurve(forwardSpeedCurve, out float forwardSpeed);
                SampleCurve(forwardSpeedInfluenceCurve, out float forwardSpeedInfluence);
                SampleCurve(turningSpeedCurve, out float turningSpeed);
                SampleCurve(verticalShiftCurve, out float verticalShift);

                if (turningSpeed > 0) Player.MovementBody.Direction.Set
                        ((storedTargetPosition - Player.Center).XZ(), turningSpeed * Time.fixedDeltaTime);

                Vector3 targetVelocity = Player.MovementBody.Velocity.Local;


                if (forwardSpeedInfluence > 0f) targetVelocity.z = horizontalDistance > horizontalThreshold
                        ? Mathf.Lerp(targetVelocity.z, forwardSpeed, forwardSpeedInfluence)
                        : 0;

                //Simpsons Comic Book Guy voice:
                //"This is a big fat steaming Hack, but I'm strapped for time and don't want to deal with deltaTime nonsense."

                if (verticalShift > 0f && verticalDistance > 0)
                {
                    //targetVelocity.y used as holder for Position calculations
                    targetVelocity.y = Player.Transform.position.y;
                    targetVelocity.y = targetVelocity.y.MoveTowards(verticalShift * Time.fixedDeltaTime, storedTargetPosition.y - Player.Collider.center.y);
                    Player.MovementBody.Position = new(Player.Position.x, targetVelocity.y, Player.Position.z);
                    targetVelocity.y = 0;
                }

                Player.MovementBody.Velocity.f = targetVelocity.z;
                Player.MovementBody.Velocity.u = targetVelocity.y;

                if (angleDifference > directionalMissThreshold)
                {
                    failBlockedReturn.State.Enter();
                    EndCleanup();
                    return;
                }

                if (elapsedTime > maxAttemptTime || (horizontalDistance <= horizontalThreshold && angleDifference <= directionalThreshold && (verticalDistance <= verticalThreshold || verticalShift == 0))) //CHANGE PHASE
                {
                    if (selectedTarget == null || selectedGrabbable == null || angleDifference > directionalThreshold)
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

                    Player.MovementBody.Velocity.ZeroOut();
                    storedTargetPosition = selectedTarget.position + (Player.Position - Player.Center);
                    playerPhaseTwoStartPosition = Player.Position;
                    elapsedTime = 0;
                    Player.Grabber.Grab(selectedGrabbable);
                    phase = 2;
                    successState.Enter();
                    //grabAnimation.Do(Player.Animator);
                }
            }
            else if (phase == 2)
            {
                elapsedTime += Time.fixedDeltaTime * grabAnimationTime;

                if (moveToTargetPosition)
                {
                    Vector3 newPosition = Vector3.Lerp(playerPhaseTwoStartPosition, storedTargetPosition, elapsedTime);
                    Player.MovementBody.Position = newPosition;
                }

                if (elapsedTime >= 1)
                {
                    if (Player.StateMachine.Airborne && Player.Stats.dropLaunch && Input.Grab.IsPressed())
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
            phase = 0;
        }
        private void SampleCurve(AnimationCurve C, out float res) => res = C.Evaluate(elapsedTime);

#if UNITY_EDITOR
        private void OnDrawGizmos()
        {
            if (phase == 1)
            {
                Gizmos.DrawSphere(storedTargetPosition, .02f);
                UnityEditor.Handles.DrawWireDisc(storedTargetPosition + (Vector3.up * verticalThreshold), Vector3.up, horizontalThreshold);
                UnityEditor.Handles.DrawWireDisc(storedTargetPosition - (Vector3.up * verticalThreshold), Vector3.up, horizontalThreshold);
            }
        }
#endif
    }
}