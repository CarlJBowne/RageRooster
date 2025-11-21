using DG.Tweening;
using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using System;
using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerRanged : MonoBehaviour
{
    #region Config
    public PlayerAirborneMovement jumpState;
    public State airThrowState;
    public State dropLaunchState;
    #endregion

    #region Data
    private bool aiming;
    new AudioCaller audio;
    public PlayerAiming aimingState;
    public State shootingState;
    private UIHUDSystem UI;
    private bool justShot;
    private CoroutinePlus justShotCO;
    #endregion

    private void Awake()
    {
        TryGetComponent(out audio);
        UI = UIHUDSystem.Instance;

        pointer.target.position = pointer.startV.position + pointer.startV.forward * pointer.distance;

        eggPool.Initialize();
        Player.Ammo.updateAmmo += UI.ammo.UpdateAmmo;
        Player.Ammo.updateMaxAmmo += UI.ammo.UpdateMax;
    }

    private void OnEnable() => PauseMenu.onPause += ExitAimingInstant;
    private void OnDisable() => PauseMenu.onPause -= ExitAimingInstant;

    private void FixedUpdate()
    {
        if (!enabled) return;
        if (eggAmount < eggCapacity) eggReplenishRate.Tick(() => Player.Ammo.Current++);

        pointer.startH.position = Player.MovementBody.Position + Vector3.up;

        if (Player.Animator.enabled) Player.Animator.Update(0f);

        if (aimingState.State) AimingFixedUpdate();
        else NonAimingFixedUpdate();
    }

    private void LateUpdate()
    {
        if (!enabled) return;
        // delegate grabbed-object follow to Player.Grabber; keep safety checks
        if (Player.Grabber != null && Player.Grabber.currentGrabbed != null && Player.Grabber.heldItemAnchor != null)
        {
            var t = Player.Grabber.currentGrabbed.transform;
            t.SetPositionAndRotation(Player.Grabber.heldItemAnchor.position, Player.Grabber.heldItemAnchor.rotation);
        }
    }

    private void OnDestroy()
    {
        Player.Ammo.updateAmmo -= UI.ammo.UpdateAmmo;
        Player.Ammo.updateMaxAmmo -= UI.ammo.UpdateMax;
    }

    // Expose currentGrabbed (forward to Player.Grabber) so external code using PlayerRanged.currentGrabbed continues to work.
    public Grabbable currentGrabbed => Player.Grabber != null ? Player.Grabber.currentGrabbed : null;

    #region Grabbing API Forwarders
    // Thin wrappers that forward to the new PlayerGrabber. This preserves the existing public API.
    public float launchVelocity
    {
        get => Player.Grabber.launchVelocity;
        set { Player.Grabber.launchVelocity = value; }
    }

    public void TryGrabThrow(PlayerGrabAction state, State throwState) { }
    public void TryGrabThrowAir(PlayerGrabAction state) { }
    public void GrabPoint(IGrabbable_Obsolete grabbed) { }
    public void GrabPointSignal() {}
    public void ThrowPoint() { }
    public void Release(Vector3 velocity, bool thrown = false) { }
    public IGrabbable_Obsolete CheckForGrabbable() => null;
    #endregion

    #region Ranged / Aiming (unchanged)
    public Pointer pointer;
    [Serializable] public class Pointer
    {
        public Transform startH;
        public Transform startV;
        public Transform target;
        public float distance;
        public Transform shootMuzzlePos;
        public LayerMask layerMask;
        public Transform hitMarker;
    }

    public Transform realMuzzle;
    public Transform spine1;
    public Cinemachine.CinemachineVirtualCameraBase shootingVCam;
    public State idleState;
    public ObjectPool eggPool;
    public float playerRotationSpeed = 10;
    public Timer.Loop eggReplenishRate = new(1f);
    public UnityEngine.Animations.Rigging.Rig aimingRig;
    public State aimThrowState;

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
        Player.MovementBody.InstantDirectionChange(
            Vector3.RotateTowards(
                Player.MovementBody.direction, pointer.startH.forward,
                playerRotationSpeed * Mathf.PI * Time.fixedTime, 0)
            );

        currentTargetDistance = pointer.distance;

        if (Physics.Raycast(pointer.startV.position + pointer.startV.forward, pointer.startV.forward, out RaycastHit hit, pointer.distance, pointer.layerMask))
        {
            UI.UpdateHitMarker(hit.point, hit.distance, hit.collider.TryGetComponent(out IDamagable _));
            pointer.hitMarker.transform.position = hit.point;
            currentTargetDistance = hit.distance;
        }
        else
        {
            UI.UpdateHitMarker(pointer.target.position, pointer.distance, false);
            pointer.hitMarker.transform.position = pointer.target.position;
        }
    }

    public void NonAimingFixedUpdate()
    {
        pointerH = Cameras.normalCamera.State.FinalOrientation.eulerAngles.y;
        pointer.target.position = Vector3.MoveTowards(pointer.target.position, pointer.startV.position + pointer.startV.forward * pointer.distance, .5f);
        aimingState.hAxis.Value = pointerH;
        aimingState.vAxis.Value = Mathf.MoveTowardsAngle(aimingState.vAxis.Value, 0, 1);
        pointerV = Mathf.MoveTowardsAngle(pointerV, 0, 1);
        if (!Player.MovementBody.Grounded && Upgrades.Active.dropLaunch)
        {
            realMuzzle.position = pointer.startH.position - (pointer.startH.up * (1 + (currentGrabbed == null ? 0 : currentGrabbed.AdditionalThrowDistance)));
            realMuzzle.eulerAngles = Vector3.right * 90;
        }
        else
        {
            realMuzzle.position = pointer.startH.position + (pointer.startH.forward * (1 + (currentGrabbed == null ? 0 : currentGrabbed.AdditionalThrowDistance)));
            realMuzzle.rotation = pointer.startH.rotation;
        }
    }

    public void EnterAiming()
    {
        if (eggCapacity == 0 && currentGrabbed != null) return;

        Player.Animator.CrossFade("Aim", 0.3f);
        aimingState.State.Enter();
        aimingRig.enabled = true;
        aimingRig.weight = 1;
        UI.SetHitMarkerVisibility(true);
        shootingVCam.Priority = 11;
        shootingVCam.gameObject.SetActive(true);
        aiming = true;
    }
    public void ExitAiming(State normalState, State grabbingState)
    {
        if (!aiming) return;
        Cameras.normalCamera.m_XAxis.Value = pointerH;
        Player.Animator.CrossFade("GroundBasic", 0.1f);
        (currentGrabbed == null ? normalState : grabbingState).Enter();
        aimingRig.enabled = false;
        aimingRig.weight = 0;
        UI.SetHitMarkerVisibility(false);
        shootingVCam.Priority = 9;
        shootingVCam.gameObject.SetActive(false);
        aiming = false;
    }

    public void ExitAimingAux()
    {
        if (!aiming) return;
        Cameras.normalCamera.m_XAxis.Value = pointerH;
        aimingRig.enabled = false;
        aimingRig.weight = 0;
        UI.SetHitMarkerVisibility(false);
        shootingVCam.Priority = 9;
        shootingVCam.gameObject.SetActive(false);
        aiming = false;
        Player.StateMachine.Children[0].Enter();
    }

    public void ExitAimingInstant()
    {
        if (!aiming) return;
        Cameras.normalCamera.m_XAxis.Value = pointerH;
        aimingRig.enabled = false;
        aimingRig.weight = 0;
        UI.SetHitMarkerVisibility(false);
        shootingVCam.Priority = 9;
        shootingVCam.gameObject.SetActive(false);
        aiming = false;
        Player.Animator.Play("GroundBasic");
        Player.StateMachine.Children[0].Enter();
    }

    public void Shoot()
    {
        if (!aiming) return;
        if (currentGrabbed != null) AimThrow();
        else if (eggAmount >= 1 && !shootingState.Active)
            shootingState.Enter();
    }

    public void ShootPoint()
    {
        if (justShot) return;
        realMuzzle.position = pointer.shootMuzzlePos.position;
        Quaternion Q = realMuzzle.rotation;
        Q.SetLookRotation(pointer.hitMarker.position - realMuzzle.position);
        realMuzzle.rotation = Q;

        audio.PlayOneShot("EggShoot");
        eggPool.Pump().GetComponent<ProjectileMovement>().Send();
        Player.Ammo.Current--;
        justShot = true;
        CoroutinePlus.Begin(ref justShotCO, Enum(), this);
        IEnumerator Enum()
        {
            yield return null;
            justShot = false;
        }
    }

    public void AimThrow()
    {
        aimThrowState.Enter();
    }
    #endregion
}