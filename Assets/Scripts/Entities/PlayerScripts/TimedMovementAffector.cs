using System.Collections;
using System.Collections.Generic;
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
        private static AnimationCurve Curve(float input) => new(new Keyframe(0, input));

        float influence;
        [SerializeField] PlayerMovementAnimator playerMovementAnimator;
        float influenceChange = 0f;

        protected override void OnSetup()
        {
            base.OnSetup();
            if (!TryGetComponentFromMachine(out playerMovementAnimator))
                DestroyImmediate(this);
        }


        protected override void OnBegin() => influenceChange = 1f / influenceFadeTime;
        protected override void OnExit(State next) => influenceChange = -1f / influenceFadeTime;

        protected override void OnTick(float delta)
        {
            if(influenceChange > 0f) 
        }




        
    }
}