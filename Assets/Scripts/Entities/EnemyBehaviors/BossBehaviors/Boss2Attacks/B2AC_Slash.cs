using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using THIS = B2AC_Slash;

public class B2AC_Slash : RigConstraint<THIS.Job, THIS.Data, THIS.Binder>
{
    public float xMin = 47.48f;
    public float xMax = 23.96f ;
    public float zMin = -28.63f ;
    public float zMax = -51.16f ;
    public float xPercentage = 0 ;
    public float zPercentage = 0 ;
    public float yPos = 2.705396f ;
    public float headMovementMaxDelta = 2;
    public float zMaxMaxDelta = 25 ;

    public struct Job : IWeightedAnimationJob
    {
        public ReadWriteTransformHandle constrained;
        public ReadOnlyTransformHandle source;
        public FloatProperty jobWeight { get; set; }

        public FloatProperty xMin;
        public FloatProperty xMax;
        public FloatProperty zMin;
        public FloatProperty zMax;
        public FloatProperty xPercentage;
        public FloatProperty zPercentage;
        public FloatProperty yPos;
        public FloatProperty headMovementMaxDelta;
        public FloatProperty zMaxMaxDelta;

        public Transform relativeSpace;

        public Matrix4x4 relativeSpaceMatrix;

        public void ProcessRootMotion(UnityEngine.Animations.AnimationStream stream) { }
        public void ProcessAnimation(UnityEngine.Animations.AnimationStream stream)
        {
            if (!stream.isValid) return; // Ensure valid data before proceeding

            zMax.Set(stream, zMax.Get(stream).MoveTowards(zMaxMaxDelta.Get(stream), relativeSpaceMatrix.inverse.MultiplyPoint3x4(source.GetPosition(stream)).z));

            Vector3 targetPos = new(Mathf.LerpUnclamped(xMin.Get(stream), xMax.Get(stream), xPercentage.Get(stream)), 
                                    yPos.Get(stream), 
                                    Mathf.LerpUnclamped(zMin.Get(stream), zMax.Get(stream), zPercentage.Get(stream)));

            try
            {
                float delta = headMovementMaxDelta.Get(stream) * Time.deltaTime;
                constrained.SetLocalPosition(stream, Vector3.Lerp(constrained.GetLocalPosition(stream),
                                                            Vector3.MoveTowards(constrained.GetLocalPosition(stream), targetPos, delta),
                                                            jobWeight.Get(stream)
                                                            ));
            }
            catch
            {
                constrained.SetLocalPosition(stream, Vector3.Lerp(constrained.GetLocalPosition(stream),
                                            targetPos,
                                            jobWeight.Get(stream)
                                            ));

            }

        }
    }

    [System.Serializable]
    public struct Data : IAnimationJobData
    {
        public Transform constrainedObject;
        [SyncSceneToStream] public Transform sourceObject;
        [SyncSceneToStream] public Transform relativeSpace;

        public bool IsValid() => !(constrainedObject == null || sourceObject == null);
        public void SetDefaultValues()
        {
            constrainedObject = null;
            sourceObject = null;
        }

        public Matrix4x4 GetRSM()
        {
            Matrix4x4 matrixTrans = Matrix4x4.identity;
            matrixTrans.SetTRS(relativeSpace.position, relativeSpace.rotation, Vector3.one);
            return matrixTrans;

            ////Matrix4x4 version of Transform.InverseTransformPoint
            //Vector3 localPosition = matrixTrans.inverse.MultiplyPoint3x4(worldPositon);

        }
    }

    public class Binder : AnimationJobBinder<Job, Data>
    {
        public override Job Create(Animator animator, ref Data data, Component component)
            => new()
            {
                constrained = ReadWriteTransformHandle.Bind(animator, data.constrainedObject),
                source = ReadOnlyTransformHandle.Bind(animator, data.sourceObject),
                xMin = FloatProperty.Bind(animator, component, nameof(B2AC_Slash.xMin)),
                xMax = FloatProperty.Bind(animator, component, nameof(B2AC_Slash.xMax)),
                zMin = FloatProperty.Bind(animator, component, nameof(B2AC_Slash.zMin)),
                zMax = FloatProperty.Bind(animator, component, nameof(B2AC_Slash.zMax)),
                xPercentage = FloatProperty.Bind(animator, component, nameof(B2AC_Slash.xPercentage)),
                zPercentage = FloatProperty.Bind(animator, component, nameof(B2AC_Slash.zPercentage)),
                yPos = FloatProperty.Bind(animator, component, nameof(B2AC_Slash.yPos)),
                headMovementMaxDelta = FloatProperty.Bind(animator, component, nameof(B2AC_Slash.headMovementMaxDelta)),
                zMaxMaxDelta = FloatProperty.Bind(animator, component, nameof(B2AC_Slash.zMaxMaxDelta)),
                relativeSpace = data.relativeSpace,
                relativeSpaceMatrix = data.GetRSM()
            };

        public override void Destroy(Job job) { }
    }
}

