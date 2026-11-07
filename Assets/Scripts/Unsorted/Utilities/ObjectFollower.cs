using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectFollower : MonoBehaviour
{
    public Transform target;
    Transform T;
    public bool followPosition;
    public bool followRotation;


    private void Awake() => T = transform;
    private void FixedUpdate()
    {
        if(followPosition) T.position = target.position;
        if(followRotation) T.rotation = target.rotation;

    }

    public void InstantMove(bool position = true, bool rotation = true)
    {
        if (position) T.position = target.position;
        if (rotation) T.rotation = target.rotation;
    }

}
