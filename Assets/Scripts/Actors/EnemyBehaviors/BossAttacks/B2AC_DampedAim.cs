using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using THIS = B2AC_DampedAim;

public class B2AC_DampedAim : RigConstraint<THIS.Job, THIS.Data, THIS.Binder>
{
    public float maxDelta;
    public bool initializeRotation;

    public struct Job : IWeightedAnimationJob
    {
        public ReadWriteTransformHandle constrained;
        public ReadOnlyTransformHandle source;
        public ReadOnlyTransformHandle initialRotationSource;
        public FloatProperty jobWeight { get; set; }

        public FloatProperty maxDelta;
        public BoolProperty initializeRotation;


        public void ProcessRootMotion(UnityEngine.Animations.AnimationStream stream) { }
        public void ProcessAnimation(UnityEngine.Animations.AnimationStream stream)
        {
            if (!stream.isValid) return; // Ensure valid data before proceeding

            if (!initializeRotation.Get(stream))
            {
                float maxDelta = this.maxDelta.Get(stream);
                float weight = this.jobWeight.Get(stream);

                if (maxDelta > 0 && weight > 0)
                {
                    Vector3 direction = source.GetPosition(stream) - constrained.GetPosition(stream);
                    var targetQ = Quaternion.LookRotation(direction, Vector3.up);
                    constrained.SetRotation(stream, Quaternion.RotateTowards(constrained.GetRotation(stream), targetQ, maxDelta * weight));
                }
                    
            }
            else constrained.SetRotation(stream, initialRotationSource.GetRotation(stream));
        }
    }

    [System.Serializable]
    public struct Data : IAnimationJobData
    {
        public Transform constrainedObject;
        [SyncSceneToStream] public Transform sourceObject;
        [SyncSceneToStream] public Transform initialRotationSource;

        public bool IsValid() => !(constrainedObject == null || sourceObject == null);
        public void SetDefaultValues()
        {
            constrainedObject = null;
            sourceObject = null;
            initialRotationSource = null;
        }
    }

    public class Binder : AnimationJobBinder<Job, Data>
    {
        public override Job Create(Animator animator, ref Data data, Component component)
            => new()
            {
                constrained = ReadWriteTransformHandle.Bind(animator, data.constrainedObject),
                source = ReadOnlyTransformHandle.Bind(animator, data.sourceObject),
                initialRotationSource = ReadOnlyTransformHandle.Bind(animator, data.initialRotationSource),
                maxDelta = FloatProperty.Bind(animator, component, nameof(B2AC_DampedAim.maxDelta)),
                initializeRotation = BoolProperty.Bind(animator, component, nameof(B2AC_DampedAim.initializeRotation)),
            };

        public override void Destroy(Job job) { }
    }
}