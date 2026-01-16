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
    public PlayerAiming aimingMovement;
    public State shootingState;
    private UIHUDSystem UI;
    private bool justShot;
    private CoroutinePlus justShotCO;
    #endregion

    private void Awake()
    {
        TryGetComponent(out audio);
        UI = UIHUDSystem.Instance;

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

        if (Player.Animator.enabled) Player.Animator.Update(0f);

        if (!aimingMovement.State && TargetingManager.RangedChannel.CurrentTarget != null)
            targetPos.position = TargetingManager.RangedChannel.CurrentTarget.position;
    }

    private void LateUpdate()
    {
        if (!enabled) return;
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

    public Grabbable currentGrabbed => Player.Grabber != null ? Player.Grabber.currentGrabbed : null;


    #region Ranged / Aiming (unchanged)

    public Transform realMuzzle;
    public Transform shootMuzzle;
    public Transform targetPos;
    public State idleState;
    public ObjectPool eggPool;
    public Timer.Loop eggReplenishRate = new(1f);
    public UnityEngine.Animations.Rigging.Rig aimingRig;
    public State aimThrowState;

    [HideProperty] public int eggAmount = 10;
    [HideProperty] public int eggCapacity = 10;
    [HideProperty] public float currentTargetDistance = 10f;

    public bool hasEggsToShoot => eggAmount > 0;

    public void EnterAiming(State targetState)
    {
        if (eggCapacity == 0 && currentGrabbed != null) return;

        aimingMovement.SetXYRotation(Cameras.normalCamera.m_XAxis.Value, Cameras.normalCamera.m_YAxis.Value);
        TargetingManager.ToggleAimingDownSights(true);
        Player.Animator.CrossFade("Aim", 0.3f);
        targetState.Enter();
        aimingRig.enabled = true;
        aimingRig.weight = 1;
        UI.SetHitMarkerVisibility(true);
        Cameras.SetTargetVirtualCamera(Cameras.aimingCamera);
        aiming = true;
    }
    public void ExitAiming(State targetState)
    {
        if (!aiming) return;
        Cameras.normalCamera.m_XAxis.Value = aimingMovement.hAxis.Value;
        TargetingManager.ToggleAimingDownSights(false);
        Player.Animator.CrossFade("GroundBasic", 0.1f);
        targetState.Enter();
        aimingRig.enabled = false;
        aimingRig.weight = 0;
        UI.SetHitMarkerVisibility(false);
        Cameras.SetTargetVirtualCamera(Cameras.normalCamera);
        aiming = false;
    }

    public void ExitAimingAux()
    {
        if (!aiming) return;
        Cameras.normalCamera.m_XAxis.Value = aimingMovement.hAxis.Value;
        TargetingManager.ToggleAimingDownSights(false);
        aimingRig.enabled = false;
        aimingRig.weight = 0;
        UI.SetHitMarkerVisibility(false);
        Cameras.SetTargetVirtualCamera(Cameras.normalCamera);
        aiming = false;
        Player.StateMachine.Children[0].Enter();
    }

    public void ExitAimingInstant()
    {
        if (!aiming) return;
        Cameras.normalCamera.m_XAxis.Value = aimingMovement.hAxis.Value;
        TargetingManager.ToggleAimingDownSights(false);
        aimingRig.enabled = false;
        aimingRig.weight = 0;
        UI.SetHitMarkerVisibility(false);
        Cameras.SetTargetVirtualCamera(Cameras.normalCamera);
        aiming = false;
        Player.Animator.Play("GroundBasic");
        Player.StateMachine.Children[0].Enter();
    }

    public void Shoot()
    {
        if (!aiming) return;
        if (currentGrabbed != null) aimThrowState.Enter();
        else if (eggAmount >= 1 && !shootingState.Active)
            shootingState.Enter();
    }

    public void ShootPoint()
    {
        if (justShot) return;
        realMuzzle.position = shootMuzzle.position;
        Quaternion Q = realMuzzle.rotation;
        Q.SetLookRotation(targetPos.position - realMuzzle.position);
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

    #endregion
}