using EditorAttributes;
using SLS.StateMachineV3;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;

public class PlayerRanged : MonoBehaviour
{
    #region Config
    public State groundState;
    public State airBorneState;
    public PlayerAirborneMovement jumpState;
    public Transform muzzle;
    public Upgrade dropLaunchUpgrade;

    #endregion
    #region Data
    Vector3 upcomingLaunchVelocity;
    PlayerStateMachine machine;
    PlayerMovementBody body;
    Animator animator;
    [HideProperty] public PlayerAiming aimingState;
    public Grabbable currentGrabbed => grabber.currentGrabbed;


    #endregion

    private void Awake()
    {
        grabber.R = this;
        TryGetComponent(out machine);
        TryGetComponent(out body);
        TryGetComponent(out animator);
        if(aimingState == null) aimingState = FindObjectOfType<PlayerAiming>(true);
    }

    private void FixedUpdate()
    {
        if (eggAmount < eggCapacity)
        {
            eggAmount += Time.deltaTime;
            if (eggAmount > eggCapacity) eggAmount = eggCapacity;
        }

        pointerStartH.position = body.position + Vector3.up;

        if (aimingState.state) AimingUpdate();
        else NonAimingUpdate();
    }



    private void OnAnimatorMove()
    {
        //animator.Update(0f);
        if (aimingState.state) AimingPostUpdate();
        grabber.LateUpdate();
    }

    #region Grabbing Throwing

    public PlayerGrabber grabber = new();
    [System.Serializable]
    public class PlayerGrabber : Grabber
    {
        //Config
        public float launchVelocity;
        public float launchJumpMult;
        public float checkSphereRadius;
        public Vector3 checkSphereOffset;
        public LayerMask layerMask;
        public Transform oneHandedHand;
        public Transform twoHandedHand;

        //Data
        [HideInInspector] public PlayerRanged R;
        [HideInInspector] public bool twoHanded;

        public void LateUpdate()
        {
            if (currentGrabbed != null) currentGrabbed.transform.SetPositionAndRotation(twoHanded ? twoHandedHand : oneHandedHand);
        }


        protected override void OnGrab() => twoHanded = currentGrabbed.twoHanded;
        protected override void OnRelease() => currentGrabbed.rb.velocity = R.upcomingLaunchVelocity;

        public Grabbable CheckForGrabbable()
        {
            Collider[] results = Physics.OverlapSphere(R.transform.position + GetRealOffset(R.transform), checkSphereRadius, layerMask);
            foreach (Collider r in results)
                if (AttemptGrab(r.gameObject, out Grabbable result, false))
                    return result;
            return null;
        }

        private Vector3 GetRealOffset(Transform transform) => 
            transform.forward * checkSphereOffset.z + transform.up * checkSphereOffset.y + transform.right * checkSphereOffset.x;

    }
    public void BeginGrab(Grabbable grabbed) => grabber.BeginGrab(grabbed);

    public void TryGrabThrow(PlayerGrabAction state, bool held)
    {
        if (grabber.currentGrabbed != null) Throw();
        else state.AttemptGrab(grabber.CheckForGrabbable(), held);
    }

    public void GrabPoint() => machine.SendSignal("FinishGrab", overrideReady: true);

    public void Throw()
    {
        if (body.grounded || !dropLaunchUpgrade)
        {
            currentGrabbed.transform.position = muzzle.position;
            upcomingLaunchVelocity = muzzle.forward * grabber.launchVelocity;
        }
        else
        {
            currentGrabbed.transform.position = transform.position + Vector3.down; //REMOVE WHEN PROPER THROWING ANIMATION IS IMPLEMENTED

            upcomingLaunchVelocity = Vector3.down * grabber.launchVelocity;
            body.VelocitySet(y: grabber.launchJumpMult * grabber.launchVelocity);
            jumpState.Enter();
        }
        if (currentGrabbed.TryGetComponent(out EnemyHealth health))
        {
            health.Ragdoll(new(0, upcomingLaunchVelocity, "Throwing"));
            health.projectile = true;
        }
        grabber.Release();
    }
    #endregion Grabbing Throwing

    public Transform pointerStartH;
    public Transform pointerStartV;
    public Transform pointerTarget;
    public Transform spine1;
    public Transform rightWrist;
    public Transform shootMuzzle;
    public float pointerDistance;
    public LayerMask pointerLayerMask;
    public Cinemachine.CinemachineVirtualCamera shootingVCam;
    public State idleState;
    public ObjectPool eggPool;
    public float playerRotationSpeed = 10;

    [HideProperty] public float eggAmount = 10;
    [HideProperty] public int eggCapacity = 10;

    public float pointerH 
    { 
        get => pointerStartH.localEulerAngles.y; 
        set => pointerStartH.localEulerAngles = new(0, value, 0); 
    }
    public float pointerV 
    { 
        get => pointerStartV.localEulerAngles.x; 
        set => pointerStartV.localEulerAngles = new(value, 0, 0); 
    }

    public bool hasEggsToShoot => eggAmount > 0;

    private Vector3 rightWristBaseRotationForward = new(44.7700844f, 277.249939f, 5.11955166f);

    public void AimingUpdate()
    {
        if (machine.signalReady && Input.Shoot.WasPressedThisFrame()) Shoot();
        if (machine.signalReady && !Input.ShootMode.IsPressed()) ExitAiming(idleState);

        body.currentDirection = Vector3.RotateTowards(
            body.currentDirection, pointerStartH.forward, 
            playerRotationSpeed * Mathf.PI * Time.fixedTime, 0);

        pointerTarget.position = Physics.Raycast(pointerStartV.position, pointerStartV.forward, out RaycastHit hit, pointerDistance, pointerLayerMask)
            ? hit.point
            : pointerStartV.position + pointerStartV.forward * pointerDistance;
    }

    public void NonAimingUpdate()
    {
        pointerH = machine.freeLookCamera.State.FinalOrientation.eulerAngles.y;
        pointerTarget.localPosition = Vector3.MoveTowards(pointerTarget.localPosition, Vector3.forward * pointerDistance, .5f);
        aimingState.pointerVRot = Mathf.MoveTowardsAngle(aimingState.pointerVRot, 0, 1);
        pointerV = Mathf.MoveTowardsAngle(pointerV, 0, 1);
    }

    public void AimingPostUpdate()
    {
        spine1.eulerAngles -= Vector3.forward * pointerV;

        rightWrist.LookAt(pointerTarget);
        rightWrist.localEulerAngles += rightWristBaseRotationForward;
    }

    public void EnterAiming()
    {
        animator.CrossFade("GrabAim.GunAim", 0.1f);
        aimingState.state.TransitionTo();
        shootingVCam.Priority = 11;
        shootingVCam.gameObject.SetActive(true);
    }
    public void ExitAiming(State nextState)
    {
        machine.freeLookCamera.m_XAxis.Value = pointerH;
        animator.CrossFade("GrabAim.Null", 0.1f);
        nextState.TransitionTo();
        shootingVCam.Priority = 9;
        shootingVCam.gameObject.SetActive(false); 
        //aimingState.ResetPointerStartRotation();
        //pointerTarget.position = pointerStartV.position + pointerStartV.forward * pointerDistance;
    }

    public void Shoot()
    {
        if (grabber.currentGrabbed != null)
        {

        }
        else if (eggAmount >= 1)
            eggPool.Pump().PlaceAtMuzzle(shootMuzzle);

    }
}