using SLS.StateMachineV3;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PlayerRanged : MonoBehaviour, IGrabber
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
    public Transform muzzle;

    #endregion
    #region Data
    private Vector3 realOffset => transform.forward * checkSphereOffset.z + transform.up * checkSphereOffset.y + transform.right * checkSphereOffset.x;
    private bool twoHanded;
    Vector3 upcomingLaunchVelocity;
    PlayerStateMachine machine;
    PlayerMovementBody move;
    public Grabbable currentGrabbed { get; set; }
    public UltEvents.UltEvent<bool> GrabStateEvent { get; set; }


    #endregion

    private void Awake()
    {
        machine = GetComponent<PlayerStateMachine>();
        move = GetComponent<PlayerMovementBody>();
    }

    public void TryGrabThrow(PlayerGrabAction state, bool held)
    {
        if (currentGrabbed != null) Throw();
        else state.AttemptGrab(CheckForGrabbable(), held);
    }

    public void GrabPoint()
    {if (machine.currentState.TryGetComponent(out PlayerGrabAction grab)) grab.GrabPoint();}

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
        if (currentGrabbed != null) currentGrabbed.transform.SetPositionAndRotation(twoHanded ? twoHandedHand : oneHandedHand);
    }

    public void Throw()
    {
        if (move.grounded || !dropLaunchUpgrade)
        {
            currentGrabbed.transform.position = muzzle.position;
            upcomingLaunchVelocity = muzzle.forward * launchVelocity;
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
        ((IGrabber)this).Release();
    }

    public void BeginGrab(Grabbable target) => ((IGrabber)this).BeginGrab(target);
    bool AttemptGrab(GameObject target, out Grabbable result, bool doGrab = true) => ((IGrabber)this).AttemptGrab(target, out result, doGrab);
    void IGrabber.OnGrab() => twoHanded = currentGrabbed.twoHanded;
    void IGrabber.OnRelease() => currentGrabbed.rb.velocity = upcomingLaunchVelocity;

}