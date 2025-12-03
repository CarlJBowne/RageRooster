using System.Collections;
using System.Collections.Generic;
using UnityEngine;


namespace SLS.StateMachineH.Timelines
{
    public class TimedMovementAffector : StateTimeline
    {
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

        private float influence;








        protected override void OnTick(float delta)
        {

        }




        
    }
}