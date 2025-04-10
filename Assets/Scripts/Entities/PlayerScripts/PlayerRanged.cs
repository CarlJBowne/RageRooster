using DG.Tweening;
using EditorAttributes;
using SLS.StateMachineV3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Animations.Rigging;
using UnityEngine.Events;
using UnityEngine.InputSystem;
using UnityEngine.SocialPlatforms;

public class PlayerRanged : MonoBehaviour, IGrabber
{
    #region Config
    public PlayerAirborneMovement jumpState;
    public State airThrowState;
    public State dropLaunchState;
    public Upgrade dropLaunchUpgrade;
    public Transform heldItemAnchor;

    #endregion
    #region Data
    private bool active;
    PlayerStateMachine machine;
    PlayerMovementBody body;
    Animator animator;
    [HideProperty] public PlayerAiming aimingState;
    public IGrabbable currentGrabbed { get; private set; }

    private UIHUDSystem UI;
    private new Collider collider;
    private CoroutinePlus layerFadeCoroutine;
    #endregion 

    private void Awake()
    {
        TryGetComponent(out machine);
        TryGetComponent(out body);
        TryGetComponent(out animator);
        TryGetComponent(out collider);
        UIHUDSystem.TryGet(out UI);
        if (aimingState == null) aimingState = FindObjectOfType<PlayerAiming>(true);

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

        animator.Update(0f);

        if (aimingState.state) AimingFixedUpdate();
        else NonAimingFixedUpdate();

    }
    private void LateUpdate()
    {
        if (aimingState.state) AimingPostUpdate();
        
        currentGrabbed?.transform.SetPositionAndRotation(heldItemAnchor);
    }
    private void OnDestroy()
    {
        GlobalState.maxAmmoUpdateCallback -= UpdateMaxAmmo;
    }

    #region Grabbing Throwing

    public float launchVelocity;
    public float checkSphereRadius;
    public Vector3 checkSphereOffset;
    public LayerMask layerMask;
    public Transform oneHandedHand;
    public Transform twoHandedHand;
    public UltEvents.UltEvent<bool> GrabStateEvent;
    public Collider ownerCollider => collider;


    public void TryGrabThrow(PlayerGrabAction state, State throwState)
    {
        if (machine.signalReady && currentGrabbed != null)
        {
            throwState.TransitionTo();
            new CoroutinePlus(QuickTurn(), this);
            IEnumerator QuickTurn()
            {
                float time = 0f;
                float rate = Vector3.Angle(pointer.startH.eulerAngles, body.currentDirection) / .1f;

                while(time < 0.1f)
                {
                    time += Time.deltaTime;
                    body.rotationQ = Quaternion.RotateTowards(body.rotationQ, pointer.startH.rotation, rate * Time.deltaTime);
                    yield return null;
                }
            }
        }
        else state.BeginGrabAttempt(CheckForGrabbable());
    }
    public void TryGrabThrowAir(PlayerGrabAction state)
    {
        if (machine.signalReady && currentGrabbed != null)
        {
            body.transform.DOBlendableRotateBy(new(0, pointer.startH.eulerAngles.y - body.transform.eulerAngles.y, 0), 0.1f);
            (!dropLaunchUpgrade ? airThrowState : dropLaunchState).TransitionTo();
        }
        else state.BeginGrabAttempt(CheckForGrabbable());
    }

    public void GrabPoint(IGrabbable grabbed)
    {
        if (!grabbed.Grab(this)) return; 
        currentGrabbed = grabbed;
        //OnGrab

        CoroutinePlus.Begin(ref layerFadeCoroutine, TurnOnLayers(1f), this);
        IEnumerator TurnOnLayers(float rate)
        {
            float V = 0;
            while (V < 1)
            {
                V += Time.deltaTime * rate;
                animator.SetLayerWeight(1, V);
                animator.SetLayerWeight(2, V);
                yield return null;
            }
        }

        //var S = new ConstraintSource { sourceTransform = heldItemAnchor };
        //grabbed.ParentConstraint.AddSource(S);
        //grabbed.ParentConstraint.constraintActive = true;
        heldItemAnchor.localPosition = grabbed.HeldOffset;
        grabbed.transform.position = heldItemAnchor.position;
        grabbed.transform.rotation = heldItemAnchor.rotation; 


        GrabStateEvent?.Invoke(true);

        //Change Later when officially removing 1-handed grabbing
        //animator.CrossFade(true ? "GrabAim.Hold2" : "GrabAim.Hold1", 0.2f);
    }

    public void GrabPointSignal() => machine.SendSignal("FinishGrab", overrideReady: true);

    public void ThrowPoint()
    {

        currentGrabbed.transform.position = muzzle.position;

        if (!body.grounded && dropLaunchUpgrade)
        {
            //body.VelocitySet(y: grabber.launchJumpMult * grabber.launchVelocity);
            jumpState.BeginJump();
        }
        Release(muzzle.forward * launchVelocity, true);
    }

    public void Release(Vector3 velocity, bool thrown = false)
    {
        if (thrown) currentGrabbed.Throw(velocity);
        else currentGrabbed.Release();
        //OnRelease

        CoroutinePlus.Begin(ref layerFadeCoroutine, TurnOffLayers(1f), this);
        IEnumerator TurnOffLayers(float rate)
        {
            float V = 1;
            while (V > 0)
            {
                V -= Time.deltaTime * rate;
                animator.SetLayerWeight(1, V);
                animator.SetLayerWeight(2, V);
                yield return null;
            }
        }
        //currentGrabbed.ParentConstraint.constraintActive = false;
        //currentGrabbed.ParentConstraint.RemoveSource(0);


        currentGrabbed = null;
        GrabStateEvent?.Invoke(false);
        //animator.CrossFade("GrabAim.Null", 0.2f);
    }

    public IGrabbable CheckForGrabbable()
    {
        IGrabbable.Test(Physics.OverlapSphere(transform.position + GetRealOffset(transform), checkSphereRadius, layerMask), out IGrabbable result);
        return result;
    }
    private Vector3 GetRealOffset(Transform transform) =>
    transform.forward * checkSphereOffset.z + transform.up * checkSphereOffset.y + transform.right * checkSphereOffset.x;







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
            muzzle.position = pointer.startH.position - (pointer.startH.up * (1 + (currentGrabbed == null ? 0 : currentGrabbed.AdditionalThrowDistance)));
            muzzle.eulerAngles = Vector3.right * 90;
        }
        else
        {
            muzzle.position = pointer.startH.position + (pointer.startH.forward * (1 + (currentGrabbed == null ? 0 : currentGrabbed.AdditionalThrowDistance)));
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
        if (currentGrabbed != null) AimThrow();
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