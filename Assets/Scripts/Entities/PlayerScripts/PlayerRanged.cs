using EditorAttributes;
using SLS.StateMachineV3;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.InputSystem;

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
    private bool active;
    Vector3 upcomingLaunchVelocity;
    PlayerStateMachine machine;
    PlayerMovementBody body;
    Animator animator;
    [HideProperty] public PlayerAiming aimingState;
    public Grabbable currentGrabbed => grabber.currentGrabbed;

    private Quaternion baseShootMuzzleRotation;
    #endregion

    private void Awake()
    {
        baseShootMuzzleRotation = shootMuzzle.localRotation;
        grabber.ranged = this;
        machine = GetComponent<PlayerStateMachine>();
        body = GetComponent<PlayerMovementBody>();
        animator = GetComponent<Animator>();
        grabber.animator = animator;
        if(aimingState == null) aimingState = FindObjectOfType<PlayerAiming>(true);
        eggPool.Initialize();
        Input.Shoot.performed += Shoot;
    }

    private void FixedUpdate()
    {
        if (eggAmount < eggCapacity)
        {
            eggAmount += Time.deltaTime;
            if (eggAmount > eggCapacity) eggAmount = eggCapacity;
        }

        pointerStartH.position = body.position + Vector3.up;

        if (aimingState.state) AimingFixedUpdate();
        else NonAimingFixedUpdate();
    }
    private void LateUpdate()
    {
        if (aimingState.state) AimingPostUpdate();
    }


    //private void OnAnimatorMove()
    //{
    //    if (aimingState.state) AimingPostUpdate();
    //    grabber.LateUpdate();
    //}

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
        [HideInInspector] public PlayerRanged ranged;
        [HideInInspector] public Animator animator;
        [HideInInspector] public bool twoHanded;

        public void LateUpdate()
        {
            if (currentGrabbed != null)
            {
                //animator.Update(0);
                currentGrabbed.transform.SetPositionAndRotation(twoHanded ? twoHandedHand : oneHandedHand);
            }
        }


        protected override void OnGrab() => twoHanded = currentGrabbed.twoHanded;
        protected override void OnRelease() => currentGrabbed.rb.velocity = ranged.upcomingLaunchVelocity;

        public Grabbable CheckForGrabbable()
        {
            Collider[] results = Physics.OverlapSphere(ranged.transform.position + GetRealOffset(ranged.transform), checkSphereRadius, layerMask);
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
    public Transform rightUpperArm;
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
    [HideProperty] public float currentTargetDistance = 10f;

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


    public void AimingFixedUpdate()
    {
        if (machine.signalReady && !Input.ShootMode.IsPressed()) ExitAiming(idleState);

        body.currentDirection = Vector3.RotateTowards(
            body.currentDirection, pointerStartH.forward, 
            playerRotationSpeed * Mathf.PI * Time.fixedTime, 0);

        if (Physics.Raycast(pointerStartV.position, pointerStartV.forward, out RaycastHit hit, pointerDistance, pointerLayerMask))
        {
            pointerTarget.position = hit.point;
            currentTargetDistance = hit.distance;
        }
        else
        {
            pointerTarget.position = pointerStartV.position + pointerStartV.forward * pointerDistance;
            currentTargetDistance = pointerDistance;
        }
    }

    public void AimingPostUpdate()
    {
        animator.Update(0);

        spine1.localEulerAngles -= Vector3.forward * pointerV;
        //
        ////Vector3 initialAimPosition = shootMuzzle.position + shootMuzzle.forward * (currentTargetDistance -(shootMuzzle.position - pointerStartH.position).magnitude);
        //
        //shootMuzzle.rotation.SetLookRotation(pointerTarget.position - shootMuzzle.position);
        ////rightWrist.eulerAngles += shootMuzzle.localEulerAngles - baseShootMuzzleRotation.eulerAngles;
        ////shootMuzzle.localRotation = baseShootMuzzleRotation;
    }

    public void NonAimingFixedUpdate()
    {
        pointerH = machine.freeLookCamera.State.FinalOrientation.eulerAngles.y;
        pointerTarget.localPosition = Vector3.MoveTowards(pointerTarget.localPosition, Vector3.forward * pointerDistance, .5f);
        aimingState.pointerVRot = Mathf.MoveTowardsAngle(aimingState.pointerVRot, 0, 1);
        pointerV = Mathf.MoveTowardsAngle(pointerV, 0, 1);
    }



    public void EnterAiming()
    {
        animator.CrossFade("GrabAim.GunAim", 0.1f);
        aimingState.state.TransitionTo();
        shootingVCam.Priority = 11;
        shootingVCam.gameObject.SetActive(true);
        active = true;
    }
    public void ExitAiming(State nextState)
    {
        machine.freeLookCamera.m_XAxis.Value = pointerH;
        animator.CrossFade("GrabAim.Null", 0.1f);
        nextState.TransitionTo();
        shootingVCam.Priority = 9;
        shootingVCam.gameObject.SetActive(false);
        active = false;
        //aimingState.ResetPointerStartRotation();
        //pointerTarget.position = pointerStartV.position + pointerStartV.forward * pointerDistance;
    }

    public void Shoot(InputAction.CallbackContext ctx)
    {
        AimingPostUpdate();

        if (!active) return;
        if (grabber.currentGrabbed != null)
        {

        }
        else if (eggAmount >= 1)
            eggPool.Pump();

    }
}