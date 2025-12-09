using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;


namespace SLS.StateMachineH.Timelines
{
    public class TimedMovementAffector : StateTimeline
    {
        public float influenceFadeTime = .5f;
        public AnimationCurve minForwardMovementCurve = Curve(0);
        public AnimationCurve maxForwardMovementCurve = Curve(0);
        public AnimationCurve speedChangeCurve = Curve(15);
        public AnimationCurve turnabilityCurve = Curve(10);
        public AnimationCurve verticalAccelerationCurve = Curve(0);
        public float terminalVelocityCurve = 98.1f; // TODO: apply terminal velocity if required
        public AnimationCurve setVerticalInfluenceCurve = Curve(0);
        public AnimationCurve setVerticalVelocityCurve = Curve(0);
        public AnimationCurve sidewaysMovementCurve = Curve(0);
        public float loopTime = 0f;
        public bool overrideOff;
        private static AnimationCurve Curve(float input) => new(new Keyframe(0, input));

        float influence;
        float influenceChange = 0f;

        protected override void OnBegin()
        {
            // Guard against divide by zero (instant influence when fade time <= 0)
            if (influenceFadeTime <= 0f)
            {
                influence = 1f;
                influenceChange = 0f;
            }
            else
            {
                influenceChange = 1f / influenceFadeTime;
            }
        }

        protected override void OnExit(State next)
        {
            if (influenceFadeTime <= 0f)
            {
                influence = 0f;
                influenceChange = 0f;
                // When immediately exiting, ensure End is invoked if appropriate
                End();
            }
            else
            {
                influenceChange = -1f / influenceFadeTime;
            }
        }

        protected override void OnTick(float delta)
        {
            if (overrideOff) return;

            DebugRR.DebugTextOverlay.AppendNewLine($"TMA : Influence: {influence}");
            DebugRR.DebugTextOverlay.AppendNewLine($"TMA : ExistingVelocity: {Player.MovementBody.velocity}");

            if (!Mathf.Approximately(influenceChange, 0f))
            {
                influence += influenceChange * delta;
                if (influence is >= 1f or <= 0f)
                {
                    influence = Mathf.Clamp01(influence);
                    influenceChange = 0f;
                    if (influence == 0f) End();
                }
            }

            if (influence <= 0f) return;

            // Horizontal Movement
            Vector3 output = Player.MovementBody.velocity;

            SampleCurve(minForwardMovementCurve, out float minForwardMovement);
            SampleCurve(maxForwardMovementCurve, out float maxForwardMovement);
            SampleCurve(speedChangeCurve, out float speedChange);
            SampleCurve(turnabilityCurve, out float turnability);
            SampleCurve(sidewaysMovementCurve, out float sidewaysMovement);

            Vector3 controlVector = Player.Controller.camAdjustedMovement;

            float targetSpeed = Player.MovementBody.CurrentSpeed;

            // Only set direction if we have meaningful input
            if (turnability > 0f && controlVector.sqrMagnitude > 0.000001f)
                Player.MovementBody.DirectionSet(controlVector.normalized, turnability * influence);

            Vector3 forwardDirection = Player.Transform.forward;
            Vector3 rightDirection = Player.Transform.right;

            // Use passed-in delta consistently for time-based calculations
            float timeFactor = delta * 50f;

            // MoveTowards helper usage preserved; assume it behaves like Mathf.MoveTowards(current, target, maxDelta)
            if (controlVector.sqrMagnitude > 0f)
            {
                targetSpeed = targetSpeed.MoveTowards(controlVector.magnitude * speedChange * timeFactor, maxForwardMovement);
            }
            else
            {
                targetSpeed = targetSpeed.MoveTowards(speedChange * timeFactor, minForwardMovement);
            }

            if (influence == 1f)
            {
                Player.MovementBody.CurrentSpeed = targetSpeed;
                output = (forwardDirection * targetSpeed) + (rightDirection * sidewaysMovement) + (Vector3.up * output.y);
            }
            else
            {
                Player.MovementBody.CurrentSpeed = Mathf.Lerp(Player.MovementBody.CurrentSpeed, targetSpeed, influence);

                output = new()
                {
                    x = Mathf.Lerp(output.x, (forwardDirection.x * targetSpeed) + (rightDirection.x * sidewaysMovement), influence),
                    y = output.y, // will be replaced below with vertical computation
                    z = Mathf.Lerp(output.z, (forwardDirection.z * targetSpeed) + (rightDirection.z * sidewaysMovement), influence)
                };
            }

            // Vertical Movement
            SampleCurve(verticalAccelerationCurve, out float verticalAcceleration);
            SampleCurve(setVerticalInfluenceCurve, out float setVerticalInfluence);
            SampleCurve(setVerticalVelocityCurve, out float setVerticalVelocity);

            float Y = Player.MovementBody.velocity.y;
            if (!Mathf.Approximately(0f, verticalAcceleration))
                Y += verticalAcceleration * influence * delta;

            if (setVerticalInfluence > 0f)
                Y = Mathf.Lerp(Y, setVerticalVelocity, setVerticalInfluence * influence);

            // Assign computed vertical component to output before applying velocity
            output.y = Y;

            DebugRR.DebugTextOverlay.AppendNewLine($"TMA : Output: {output}");
            Player.MovementBody.VelocitySet(output.x, output.y, output.z);
        }

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