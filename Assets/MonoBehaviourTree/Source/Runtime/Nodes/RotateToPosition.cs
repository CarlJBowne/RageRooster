using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

namespace MBT 
{
    [AddComponentMenu("")]
    [MBTNode("Services/Rotate To Position")]
    public class RotateToPosition : Service
    {
        public TransformReference sourceTransform;
        public TransformReference targetToFace = new TransformReference(VarRefMode.DisableConstant);

        public override void Task()
        {
            Transform t1 = sourceTransform.Value;
            if (t1 == null)
            {
                return;
            }

            Transform t2 = targetToFace.Value;
            if (t2 == null) 
            {
                return;
            }

            t1.LookAt(t2);
        }

    }
}

/* public QuaternionReference sourceQuat;
public QuaternionReference rotation = new QuaternionReference(VarRefMode.DisableConstant);

public override void Task()
{
    Quaternion q = sourceQuat.Value;
    if (q == null)
    {
        return;
    }
    rotation.Value = q;
} */