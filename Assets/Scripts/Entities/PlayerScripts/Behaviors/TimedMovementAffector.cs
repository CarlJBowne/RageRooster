using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;
using Utilities.Xtensions.Unity;


namespace SLS.StateMachineH.Timelines
{
    public class TimedMovementAffector : StateTimeline
    {
        // Note: `influenceFadeTime` is repurposed as the exit fade duration.
        public AnimationCurve minForwardMovementCurve = Curve(0);
        public AnimationCurve maxForwardMovementCurve = Curve(0);
        public AnimationCurve speedChangeCurve = Curve(15);
        public AnimationCurve turnabilityCurve = Curve(10);
        public AnimationCurve verticalAccelerationCurve = Curve(0);
        public float terminalVelocity = 98.1f;
        public AnimationCurve setVerticalInfluenceCurve = Curve(0);
        public AnimationCurve setVerticalVelocityCurve = Curve(0);
        public AnimationCurve sidewaysMovementCurve = Curve(0);

        public float loopTime = 0f;
        public float influenceFadeTime = .5f;
        public bool overrideOff;

        // Exit fade tracking (procedural fade-out of selected curves)
        float exitElapsed = -1f;

        protected override void OnBegin()
        {
            // Reset exit state; fade-in is handled by the curves themselves.
            exitElapsed = -1f;
        }

        protected override void OnExit(State next)
        {
            // Begin procedural fade-out. If duration <= 0, end immediately.
            if (influenceFadeTime <= 0f)
            {
                // Immediately end this timeline.
                End();
            }
            else
            {
                exitElapsed = 0f;
            }
        }

        protected override void OnTick(float delta)
        {
            if (overrideOff) return;

            // Compute exit fade factor (1 while active, approaches 0 while exiting).
            float exitFadeFactor = 1f;
            if (exitElapsed >= 0f)
            {
                exitElapsed += delta;
                exitFadeFactor = Mathf.Clamp01(1f - (exitElapsed / influenceFadeTime));
                //DebugRR.DebugTextOverlay.AppendNewLine($"TMA : ExitFadeFactor: {exitFadeFactor}");
                if (exitFadeFactor <= 0f)
                {
                    // Ensure End is invoked and do not apply any further effects.
                    End();
                    return;
                }
            }

            //DebugRR.DebugTextOverlay.AppendNewLine($"TMA : ExistingVelocity: {Player.MovementBody.velocity}");

            //Read Curves
            SampleCurve(minForwardMovementCurve, out float minForwardMovement);
            SampleCurve(maxForwardMovementCurve, out float maxForwardMovement);
            SampleCurve(speedChangeCurve, out float speedChange);
            SampleCurve(turnabilityCurve, out float turnability);
            SampleCurve(sidewaysMovementCurve, out float sidewaysMovement);
            SampleCurve(verticalAccelerationCurve, out float verticalAcceleration);
            SampleCurve(setVerticalInfluenceCurve, out float setVerticalInfluence);
            SampleCurve(setVerticalVelocityCurve, out float setVerticalVelocity);

            //Apply exit factor.
            speedChange *= exitFadeFactor;
            turnability *= exitFadeFactor;
            sidewaysMovement *= exitFadeFactor;
            verticalAcceleration *= exitFadeFactor;
            setVerticalInfluence *= exitFadeFactor;

            //Horizontal Movement
            Vector3 output = Player.MovementBody.velocity;
            Vector3 controlVector = Player.Controller.camAdjustedMovement;

            float targetSpeed = Player.MovementBody.CurrentSpeed;

            // Only set direction if we have meaningful input and turnability is non-zero
            if (turnability > 0f && controlVector.sqrMagnitude > 0.000001f)
                Player.MovementBody.DirectionSet(controlVector.normalized, turnability);

            Vector3 forwardDirection = Player.Transform.forward;
            Vector3 rightDirection = Player.Transform.right;
            targetSpeed = controlVector.sqrMagnitude > 0f
                ? targetSpeed.MoveTowards(controlVector.magnitude * speedChange * (delta * 50f), maxForwardMovement)
                : targetSpeed.MoveTowards(speedChange * (delta * 50f), minForwardMovement);

            Player.MovementBody.CurrentSpeed = targetSpeed;
            output = (forwardDirection * targetSpeed) + (rightDirection * sidewaysMovement) + (Vector3.up * output.y);

            // Vertical Movement
            float Y = Player.MovementBody.velocity.y;
            if (!Mathf.Approximately(0f, verticalAcceleration))
                Y += verticalAcceleration * delta;
            if (setVerticalInfluence > 0f)
                Y = Mathf.Lerp(Y, setVerticalVelocity, setVerticalInfluence);
            if (Player.MovementBody.isGrounded && Y < 0) Y = 0;
            if (Player.MovementBody.isGrounded && Y > 0) Player.MovementBody.UnLand();
            output.y = Y;

            //DebugRR.DebugTextOverlay.AppendNewLine($"TMA : Output: {output}");
            Player.MovementBody.VelocitySet(output.x, output.y, output.z);
        }

        private static AnimationCurve Curve(float input) => new(new Keyframe(0, input));
        private void SampleCurve(AnimationCurve C, out float res) => res = C.Evaluate(loopTime <= 0f ? elapsedTime : elapsedTime % loopTime);

        /*
        [SerializeField] AnimationClip referenceClip;
        [Button]
        private void RecastTiming()
        {
            float duration = referenceClip.length;

            //stretch animation curve to match the duration.
            void StretchCurve(ref AnimationCurve C)
            {
                Keyframe[] keys = C.keys;
                for (int i = 0; i < keys.Length; i++)
                    keys[i].time = (keys[i].time / keys[keys.Length - 1].time) * duration;
                C.keys = keys;
            }

            StretchCurve(ref minForwardMovementCurve);
            StretchCurve(ref maxForwardMovementCurve);
            StretchCurve(ref speedChangeCurve);
            StretchCurve(ref turnabilityCurve);
            StretchCurve(ref verticalAccelerationCurve);
            StretchCurve(ref setVerticalInfluenceCurve);
            StretchCurve(ref setVerticalVelocityCurve);
            StretchCurve(ref sidewaysMovementCurve);
        }
        */

    }
}