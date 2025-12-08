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
        public float terminalVelocityCurve = 98.1f;
        public AnimationCurve setVerticalInfluenceCurve = Curve(0);
        public AnimationCurve setVerticalVelocityCurve = Curve(0);
        public AnimationCurve sidewaysMovementCurve = Curve(0);
        public float loopTime = 0f;
        public bool overrideOff;
        private static AnimationCurve Curve(float input) => new(new Keyframe(0, input));

        float influence;
        float influenceChange = 0f;

        protected override void OnBegin() => influenceChange = 1f / influenceFadeTime;
        protected override void OnExit(State next) => influenceChange = -1f / influenceFadeTime;

        protected override void OnTick(float delta)
        {
            if(overrideOff) return;

            DebugRR.DebugTextOverlay.AppendNewLine($"TMA : Influence: {influence}");
            DebugRR.DebugTextOverlay.AppendNewLine($"TMA : ExistingVelocity: {Player.MovementBody.velocity}");

            if (!Mathf.Approximately(influenceChange, 0))
            {
                influence += influenceChange * delta;
                if(influence is >= 1f or <= 0f)
                {
                    influence = Mathf.Clamp01(influence);
                    influenceChange = 0f;
                    if (influence == 0) End();
                }   
            } 

            if(influence <= 0f) return;

            //Horizontal Movement

            Vector3 output = Player.MovementBody.velocity;

            SampleCurve(minForwardMovementCurve, out float minForwardMovement);
            SampleCurve(maxForwardMovementCurve, out float maxForwardMovement);
            SampleCurve(speedChangeCurve, out float speedChange);
            SampleCurve(turnabilityCurve, out float turnability);
            SampleCurve(sidewaysMovementCurve, out float sidewaysMovement);

            Vector3 controlVector = Player.Controller.camAdjustedMovement;

            float targetSpeed = Player.MovementBody.CurrentSpeed;

            if(turnability > 0) Player.MovementBody.DirectionSet(controlVector.normalized, turnability * influence);

            Vector3 forwardDirection = Player.Transform.forward;
            Vector3 rightDirection = Player.Transform.right;

            targetSpeed = controlVector.sqrMagnitude > 0
                ? targetSpeed.MoveTowards(controlVector.magnitude * speedChange * (Time.deltaTime * 50), maxForwardMovement)
                : targetSpeed.MoveTowards(speedChange * (Time.deltaTime * 50), minForwardMovement);

            if (influence == 1)
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
                    y = output.y,
                    z = Mathf.Lerp(output.z, (forwardDirection.z * targetSpeed) + (rightDirection.z * sidewaysMovement), influence)
                };
            }



            //Vertical Movement

            SampleCurve(verticalAccelerationCurve, out float verticalAcceleration);
            SampleCurve(setVerticalInfluenceCurve, out float setVerticalInfluence);
            SampleCurve(setVerticalVelocityCurve, out float setVerticalVelocity);

            float Y = Player.MovementBody.velocity.y;
            if (!Mathf.Approximately(0, verticalAcceleration))
                Y += verticalAcceleration * influence * delta;
            if (setVerticalInfluence > 0)
                Y = Mathf.Lerp(Y, setVerticalVelocity, setVerticalInfluence * influence);

            DebugRR.DebugTextOverlay.AppendNewLine($"TMA : Output: {output}");
            Player.MovementBody.VelocitySet(output.x, output.y, output.z);

        }

        private void SampleCurve(AnimationCurve C, out float res) => res = C.Evaluate(loopTime <= 0 ? elapsedTime : elapsedTime % loopTime);

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