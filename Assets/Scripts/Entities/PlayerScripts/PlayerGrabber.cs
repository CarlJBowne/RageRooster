using SLS.StateMachineV2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerGrabber : Grabber
{
    #region Config
    public float checkSphereRadius;
    public Vector3 checkSphereOffset;
    public float launchVelocity;
    public LayerMask layerMask;
    public Transform oneHandedHand;
    public Transform twoHandedHand;
    public State groundState;
    public State airBorneState;

    #endregion
    #region Data
    private Vector3 realOffset => transform.forward * checkSphereOffset.z + transform.up * checkSphereOffset.y + transform.right * checkSphereOffset.x;
    private bool twoHanded;
    Vector3 upcomingLaunchVelocity;
    PlayerMovementBody move;
    #endregion

    private void Awake()
    {
        move = GetComponentInChildren<PlayerMovementBody>();
    }

    public void GrabButtonPress()
    {
        if (!grabbing) InitGrab();
        else Throw();
    }

    public void InitGrab() 
    {
        Collider[] results = Physics.OverlapSphere(transform.position + realOffset, checkSphereRadius, layerMask);
        foreach (Collider r in results) 
            if (AttemptGrab(r.gameObject)) 
                break;
        
    }

    private void LateUpdate()
    {
        if (!grabbing) return;
        currentGrabbed.transform.position = twoHanded ? twoHandedHand.position : oneHandedHand.position;
        currentGrabbed.transform.rotation = twoHanded ? twoHandedHand.rotation : oneHandedHand.rotation;
    }

    private void Throw()
    {
        if (groundState.enabled)
        {
            upcomingLaunchVelocity = transform.forward * launchVelocity;
        }
        else if (airBorneState.enabled)
        {
            upcomingLaunchVelocity = Vector3.down * launchVelocity;
            move.velocity += 10 * launchVelocity * Vector3.up;
        }
        Release();
    }

    protected override void OnGrab()
    {
        twoHanded = currentGrabbed.twoHanded;
    }
    protected override void OnRelease()
    {
        currentGrabbed.rb.velocity = upcomingLaunchVelocity;
    }
}