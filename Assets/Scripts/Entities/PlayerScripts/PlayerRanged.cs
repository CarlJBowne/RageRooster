using DG.Tweening;
using EditorAttributes;
using SLS.StateMachineV3;
using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.Events;
using UnityEngine.InputSystem;

public class PlayerRanged : MonoBehaviour
{
    #region Config
    public State groundState;
    public State airBorneState;
    public PlayerAirborneMovement jumpState;
    public State throwState;
    public State airThrowState;
    public State dropLaunchState;
    //public Transform muzzle;
    public Upgrade dropLaunchUpgrade;

    #endregion
    #region Data
    private bool active;
    Vector3 launchVelocity;
    PlayerStateMachine machine;
    PlayerMovementBody body;
    Animator animator;
    [HideProperty] public PlayerAiming aimingState;
    public Grabbable currentGrabbed => grabber.currentGrabbed;

    private UIHUDSystem UI;
    #endregion

    private void Awake()
    {
        machine = GetComponent<PlayerStateMachine>();
        body = GetComponent<PlayerMovementBody>();
        animator = GetComponent<Animator>();
        UIHUDSystem.TryGet(out UI);
        if (aimingState == null) aimingState = FindObjectOfType<PlayerAiming>(true);

        grabber.ranged = this;
        grabber.animator = animator;
        TryGetComponent(out grabber.collider); 
        
        pointer.target.position = pointer.startV.position + pointer.startV.forward * pointer.distance;

        eggPool.Initialize();
        eggCapacity = GlobalState.maxAmmo;
        eggAmount = eggCapacity;
        UI.UpdateAmmo(eggAmount);
        GlobalState.maxAmmoUpdateCallback += UpdateMaxAmmo;
    }

    private void FixedUpdate()
    {
        if (eggAmount < eggCapacity) eggReplenishRate.Tick(() => ChangeAmmoAmount(1));

        pointer.startH.position = body.position + Vector3.up;

        if (aimingState.state) AimingFixedUpdate();
        else NonAimingFixedUpdate();
    }
    private void LateUpdate()
    {
        if (aimingState.state) AimingPostUpdate();
        grabber.LateUpdate();
    }

    private void OnDestroy()
    {
        GlobalState.maxAmmoUpdateCallback -= UpdateMaxAmmo;
    }

    public void GrabWhenAiming(PlayerGrabAction grabState, bool held)
    {
        if (grabber.currentGrabbed)
        {
            Shoot();
            return;
        }
        Grabbable grabCheck = grabber.CheckForGrabbable();
        if (grabCheck != null)
        {
            animator.Play("Grab");
            animator.Play("GrabAim.Grab");
            grabState.AttemptGrab(grabCheck, held);
        }
        else Shoot();
    }

    #region Grabbing Throwing

    public PlayerGrabber grabber = new();
    [System.Serializable]
    public class PlayerGrabber : Grabber
    {
        //Config
        public float launchVelocity;
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
        protected override void OnRelease() => currentGrabbed.SetVelocity(ranged.launchVelocity);

        public override void BeginGrab(Grabbable grabbed)
        {
            base.BeginGrab(grabbed);
            animator.CrossFade(grabbed.twoHanded ? "GrabAim.Hold2" : "GrabAim.Hold1", 0.2f);
        }

        public override void Release(Vector3 velocity)
        {
            base.Release(velocity);
            animator.CrossFade("GrabAim.Null", 0.2f);
        }

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
        if (machine.signalReady && grabber.currentGrabbed != null)
        {
            body.transform.DOBlendableRotateBy(new(0, pointer.startH.eulerAngles.y - body.transform.eulerAngles.y, 0), 0.1f);
            BeginThrow(!body.grounded);
        }
        else state.AttemptGrab(grabber.CheckForGrabbable(), held);
    }

    public void BeginThrow(bool air) => (!air ? throwState : !dropLaunchUpgrade ? airThrowState : dropLaunchState).TransitionTo();

    public void GrabPoint() => machine.SendSignal("FinishGrab", overrideReady: true);

    public void ThrowPoint()
    {

        currentGrabbed.transform.position = muzzle.position;
        launchVelocity = muzzle.forward * grabber.launchVelocity;

        if (!body.grounded && dropLaunchUpgrade)
        {
            //body.VelocitySet(y: grabber.launchJumpMult * grabber.launchVelocity);
            jumpState.BeginJump();
        }
        grabber.Release(launchVelocity);
    }


    #endregion Grabbing Throwing    

    public Pointer pointer;
    [Serializable] public class Pointer
    {
        public Transform startH;
        public Transform startV;
        public Transform target;
        public float distance;
        public Transform shootMuzzlePos;
        public LayerMask layerMask;
    }

    public Transform muzzle;
    public Transform spine1;
    public Cinemachine.CinemachineVirtualCamera shootingVCam;
    public State idleState;
    public ObjectPool eggPool;
    public float playerRotationSpeed = 10;
    public Timer.Loop eggReplenishRate = new(1f);
    public Rig aimingRig;

    [HideProperty] public int eggAmount = 10;
    [HideProperty] public int eggCapacity = 10;
    [HideProperty] public float currentTargetDistance = 10f;

    public float pointerH 
    { 
        get => pointer.startH.localEulerAngles.y; 
        set => pointer.startH.localEulerAngles = new(0, value, 0); 
    }
    public float pointerV 
    { 
        get => pointer.startV.localEulerAngles.x; 
        set => pointer.startV.localEulerAngles = new(value, 0, 0); 
    }

    public bool hasEggsToShoot => eggAmount > 0;


    public void AimingFixedUpdate()
    {
        if (machine.signalReady && !Input.Aim.IsPressed()) ExitAiming(idleState);

        body.currentDirection = Vector3.RotateTowards(
            body.currentDirection, pointer.startH.forward, 
            playerRotationSpeed * Mathf.PI * Time.fixedTime, 0);

        if (Physics.Raycast(pointer.startV.position, pointer.startV.forward, out RaycastHit hit, pointer.distance, pointer.layerMask))
        {
            pointer.target.position = hit.point;
            currentTargetDistance = hit.distance;
        }
        else
        {
            pointer.target.position = pointer.startV.position + pointer.startV.forward * pointer.distance;
            currentTargetDistance = pointer.distance;
        }
    }

    public void AimingPostUpdate()
    {
        animator.Update(0);

        spine1.localEulerAngles -= Vector3.forward * pointerV;

        muzzle.position = pointer.shootMuzzlePos.position;
        Quaternion Q = muzzle.rotation;
        Q.SetLookRotation(pointer.target.position - muzzle.position);
        muzzle.rotation = Q;
    }

    public void NonAimingFixedUpdate()
    {
        pointerH = machine.freeLookCamera.State.FinalOrientation.eulerAngles.y;
        pointer.target.position = Vector3.MoveTowards(pointer.target.position, pointer.startV.position + pointer.startV.forward * pointer.distance, .5f);
        aimingState.hAxis.Value = pointerH; 
        aimingState.vAxis.Value = Mathf.MoveTowardsAngle(aimingState.vAxis.Value, 0, 1);
        pointerV = Mathf.MoveTowardsAngle(pointerV, 0, 1);
        if(!body.grounded && dropLaunchUpgrade)
        {
            muzzle.position = pointer.startH.position - (pointer.startH.up * (1 + (currentGrabbed == null ? 0 : currentGrabbed.additionalThrowDistance)));
            muzzle.eulerAngles = Vector3.right * 90;
        }
        else
        {
            muzzle.position = pointer.startH.position + (pointer.startH.forward * (1 + (currentGrabbed == null ? 0 : currentGrabbed.additionalThrowDistance)));
            muzzle.rotation = pointer.startH.rotation;
        }
    }



    public void EnterAiming()
    {
        animator.CrossFade("GrabAim.GunAim", 0.1f);
        aimingState.state.TransitionTo();
        aimingRig.weight = 1;
        shootingVCam.Priority = 11;
        shootingVCam.gameObject.SetActive(true);
        active = true;
    }
    public void ExitAiming(State nextState)
    {
        machine.freeLookCamera.m_XAxis.Value = pointerH;
        animator.CrossFade("GrabAim.Null", 0.1f);
        nextState.TransitionTo();
        aimingRig.weight = 0;
        shootingVCam.Priority = 9;
        shootingVCam.gameObject.SetActive(false);
        active = false;
        //aimingState.ResetPointerStartRotation();
        //pointerTarget.position = pointerStartV.position + pointerStartV.forward * pointerDistance;
    }

    public void Shoot()
    {
        AimingPostUpdate();

        if (!active) return;
        if (grabber.currentGrabbed != null)
        {
            AimThrow();
        }
        else if (eggAmount >= 1)
        {
            eggPool.Pump();
            ChangeAmmoAmount(-1);
        }

    }

    public void AimThrow() => animator.Play("GrabAim.Throw");

    void ChangeAmmoAmount(int offset)
    {
        eggAmount += offset;
        UI.UpdateAmmo(eggAmount);
    }
    void UpdateMaxAmmo()
    {
        eggCapacity = GlobalState.maxAmmo;
        UI.UpdateAmmo(eggAmount);
    }

}