using System.Collections;
using System.Collections.Generic;
using UnityEditor.Rendering;
using UnityEngine;


namespace SLS.StateMachineH.Timelines
{
    public class TimedMovementAffector : StateTimeline
    {
        public float influenceFadeTime = .5f;
        public AnimationCurve minForwardMovement = Curve(0);
        public AnimationCurve maxForwardMovement = Curve(0);
        public AnimationCurve speedChange = Curve(15);
        public AnimationCurve turnability = Curve(10);
        public AnimationCurve verticalAcceleration = Curve(0);
        public float terminalVelocity = 98.1f;
        public AnimationCurve setVerticalInfluence = Curve(0);
        public AnimationCurve setVerticalVelocity = Curve(0);
        public AnimationCurve sidewaysMovement = Curve(0);
        public float loopTime = 0f;
        private static AnimationCurve Curve(float input) => new(new Keyframe(0, input));

        PlayerMovementBody body;
        float influence;
        float influenceChange = 0f;

        protected override void OnBegin() => influenceChange = 1f / influenceFadeTime;
        protected override void OnExit(State next) => influenceChange = -1f / influenceFadeTime;

        protected override void OnTick(float delta)
        {
            if(Mathf.Approximately(influenceChange, 0))
            {
                influence += influenceChange * delta;
                if(influence is >= 1f or <= 0f)
                {
                    influence = Mathf.Clamp01(influence);
                    influenceChange = 0f;
                }   
            } 

            if(influence <= 0f) return;

            SampleCurve(minForwardMovement, out float minForwardMovementV);
            SampleCurve(maxForwardMovement, out float maxForwardMovementV);
            SampleCurve(speedChange, out float speedChangeV);
            SampleCurve(turnability, out float turnabilityV);
            SampleCurve(verticalAcceleration, out float verticalAccelerationV);
            SampleCurve(setVerticalInfluence, out float setVerticalInfluenceV);
            SampleCurve(setVerticalVelocity, out float setVerticalVelocityV);
            SampleCurve(sidewaysMovement, out float sidewaysMovementV);


            float Y = body.velocity.y;
            if(!Mathf.Approximately(0, verticalAccelerationV))
                Y += verticalAccelerationV * influence * delta;
            if(setVerticalInfluenceV > 0)
                Y = Mathf.Lerp(Y, setVerticalVelocityV, setVerticalInfluenceV * influence * delta);
            
            


        }

        private float SampleCurve(AnimationCurve C) => C.Evaluate(loopTime <= 0 ? elapsedTime : elapsedTime % loopTime);
        private void SampleCurve(AnimationCurve C, out float res) => res = C.Evaluate(loopTime <= 0 ? elapsedTime : elapsedTime % loopTime);



    }
}