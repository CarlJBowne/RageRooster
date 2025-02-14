using SLS.StateMachineV3;
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
    public float launchJumpMult;
    public LayerMask layerMask;
    public Transform oneHandedHand;
    public Transform twoHandedHand;
    public State groundState;
    public State airBorneState;
    public Upgrade dropLaunchUpgrade;
    public PlayerAirborneMovement jumpState;

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
        move = GetComponent<PlayerMovementBody>();
        //if(!move) machine.waitforMachineInit += () => { move = machine.GetGlobalBehavior<PlayerMovementBody>(); };
    }

    public void TryGrabThrow(PlayerGrabAction state, bool held)
    {
        if (currentGrabbed != null) Throw();
        else state.AttemptGrab(CheckForGrabbable(), held);
    }

    public void GrabPoint()
    {
        if (machine.currentState.TryGetComponent(out PlayerGrabAction grab)) grab.GrabPoint();
    }

    public Grabbable CheckForGrabbable()
    {
        Collider[] results = Physics.OverlapSphere(transform.position + realOffset, checkSphereRadius, layerMask);
        foreach (Collider r in results)
            if (AttemptGrab(r.gameObject, out Grabbable result, false))
                return result; 
        return null;
    }

    private void LateUpdate()
    {
        if (!grabbing) return;
        currentGrabbed.transform.SetPositionAndRotation(twoHanded ? twoHandedHand : oneHandedHand);
    }

    public void Throw()
    {
        if (move.grounded || !dropLaunchUpgrade)
        {
            currentGrabbed.transform.position = transform.position + transform.forward;
            upcomingLaunchVelocity = transform.forward * launchVelocity;
        }
        else
        {
            currentGrabbed.transform.position = transform.position + Vector3.down; //REMOVE WHEN PROPER THROWING ANIMATION IS IMPLEMENTED

            upcomingLaunchVelocity = Vector3.down * launchVelocity;
            move.VelocitySet(y: launchJumpMult * launchVelocity);
            jumpState.Enter();
        }
        if(currentGrabbed.TryGetComponent(out EnemyHealth health))
        {
            health.Ragdoll(new(0, upcomingLaunchVelocity, "Throwing"));
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

}