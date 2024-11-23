using SLS.StateMachineV2;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerGrabber : Grabber, IAttacker
{
    #region Config
    public float checkSphereRadius;
    public Vector3 checkSphereOffset;
    public float launchVelocity;
    public float launchJumpMult;
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
    PlayerStateMachine machine;
    PlayerMovementBody move;
    #endregion

    private void Awake()
    {
        machine = GetComponent<PlayerStateMachine>();
        machine.waitforMachineInit += () => { move = machine.GetGlobalBehavior<PlayerMovementBody>(); };
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
        currentGrabbed.transform.SetPositionAndRotation(
            twoHanded ? twoHandedHand.position : oneHandedHand.position, 
            twoHanded ? twoHandedHand.rotation : oneHandedHand.rotation);
    }

    private void Throw()
    {
        if (move.grounded || !machine.DropLaunch)
        {
            upcomingLaunchVelocity = transform.forward * launchVelocity;
        }
        else
        {
            currentGrabbed.transform.position = transform.position + Vector3.down; //REMOVE WHEN PROPER THROWING ANIMATION IS IMPLEMENTED

            upcomingLaunchVelocity = Vector3.down * launchVelocity;
            move.VelocitySet(y: launchJumpMult * launchVelocity);
        }
        if(currentGrabbed.TryGetComponent(out EnemyHealth health))
        {
            health.Ragdoll(new Attack(0, "Throw", false, this, upcomingLaunchVelocity));
            health.projectile = true;
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

    [Obsolete]
    public void Contact(GameObject target) => throw new NotImplementedException();



    /* Questions:
     Do we want to fully reset the player's velocity on drop launch or launch them relative to their downward velocity.
     Do we want the drop launch to work similar to a jump where the player keeps going up if they hold the button to a point?
     */
}