using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using THIS = B2AC_Peck;

public class B2AC_Peck : RigConstraint<THIS.Job, THIS.Data, THIS.Binder>
{
    public float followPlayerRate;
    public float yPosition = 8.19f;

    public struct Job : IWeightedAnimationJob
    {
        public ReadWriteTransformHandle constrained;
        public ReadOnlyTransformHandle source;
        public FloatProperty jobWeight { get; set; }

        public FloatProperty followPlayerRate;
        public FloatProperty yPosition;


        public void ProcessRootMotion(UnityEngine.Animations.AnimationStream stream) { }
        public void ProcessAnimation(UnityEngine.Animations.AnimationStream stream)
        {
            float weight = jobWeight.Get(stream);
            if (!stream.isValid || weight <= 0.0001f) return; // Ensure valid data before proceeding

            float followPlayerRate = this.followPlayerRate.Get(stream);
            float yPosition = this.yPosition.Get(stream);

            if(followPlayerRate < .001f)
            {
                Vector3 target = constrained.GetPosition(stream);
                target.y = yPosition;
                constrained.SetPosition(stream, Vector3.Lerp(constrained.GetPosition(stream), target, weight));
                return;
            }

            Vector3 newPosition = constrained.GetPosition(stream);
            Vector3 sourcePosition = source.GetPosition(stream);

            if (followPlayerRate > 0)
                try
                {
                    newPosition = Vector3.MoveTowards(newPosition.XZ(), sourcePosition.XZ(), followPlayerRate * Time.deltaTime);
                }
                catch
                {
                    newPosition = Vector3.MoveTowards(newPosition.XZ(), sourcePosition.XZ(), followPlayerRate);
                }

            newPosition.y = yPosition;

            constrained.SetPosition(stream, Vector3.Lerp(constrained.GetPosition(stream), newPosition, weight));
        }
    }

    [System.Serializable]
    public struct Data : IAnimationJobData
    {
        public Transform constrainedObject;
        [SyncSceneToStream] public Transform sourceObject;

        public bool IsValid() => !(constrainedObject == null || sourceObject == null);
        public void SetDefaultValues()
        {
            constrainedObject = null;
            sourceObject = null;
        }
    }

    public class Binder : AnimationJobBinder<Job, Data>
    {
        public override Job Create(Animator animator, ref Data data, Component component)
            => new()
            {
                constrained = ReadWriteTransformHandle.Bind(animator, data.constrainedObject),
                source = ReadOnlyTransformHandle.Bind(animator, data.sourceObject),
                followPlayerRate = FloatProperty.Bind(animator, component, nameof(B2AC_Peck.followPlayerRate)),
                yPosition = FloatProperty.Bind(animator, component, nameof(B2AC_Peck.yPosition))
            };

        public override void Destroy(Job job) { }
    }
}

