using System;
using System.Collections;
using System.Collections.Generic;
using DG.Tweening;
using EditorAttributes;
using RageRooster.Core;
using RageRooster.Core.Save;
using RageRooster.Player;
using SLS.GeneralUtilities.EventTickets;
using SLS.GeneralUtilities.StatObjects;
using SLS.ObjectUtilities;
using SLS.StateMachineH;
using UnityEngine;
using static RageRooster.Player.Services;
using Services = RageRooster.Services;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerRanged : MonoBehaviour
{
    #region Config
    public Transform realMuzzle;
    public Transform shootMuzzle;
    public Transform targetPos;
    public PlayerLassoProjectile LassoProjectile;
    public ObjectPool<PlayerProjectile> eggPool;
    public Timer.Loop eggReplenishRate = new(1f);
    public UnityEngine.Animations.Rigging.Rig aimingRig;
    public State aimThrowState;
    public Cinemachine.AxisState hAxis;
    public Cinemachine.AxisState vAxis;
    public Transform aimRotationController;
    public LayerMask hitScanLayerMask;
    public float playerRotationSpeed = 10f;


    #endregion

    #region Data
    protected List<EventTicket> events = new();
    public IntStatClamped Ammo = new();
    [HideProperty] public float currentTargetDistance = 10f;

    public bool hasEggsToShoot => Ammo > 0;

    #endregion


    private void Awake()
    {
        eggPool.Initialize();
        Ammo.Max = Player.Stats.MaxAmmo;
        Ammo.Value = Ammo.Max;
        Ammo.Min = 0;
        events = new()
        {
            Player.Stats.MaxAmmo.Subscribe(Ammo.SetMax),
            Services.UI.OnPause.Subscribe(ExitAimingInstant)
        };
    }
    private void OnDestroy()
    {
        eggPool.Cleanup();

    }

    private void OnEnable() => events.SubscribeAll();
    private void OnDisable() => events.UnSubscribeAll();

    private void FixedUpdate()
    {
        eggPool.Update(Time.deltaTime);
        if (!enabled) return;
        if (Ammo.Value < Ammo.Max) eggReplenishRate.Tick(() => Ammo += 1);

        if (Self.Animator.enabled) Self.Animator.Update(0f);

        if (Self.StateMachine.Aiming)
        {
            aimRotationController.position = Self.MovementBody.Position + (Vector3.up * 0.915f);

            hAxis.m_InputAxisValue = Input.Camera.x;
            hAxis.Update(Time.deltaTime);
            vAxis.m_InputAxisValue = Input.Camera.y;
            vAxis.Update(Time.deltaTime);
            aimRotationController.localEulerAngles = new Vector3(vAxis.Value, hAxis.Value, 0);

            targetPos.position = TargetingManager.RangedChannel.CurrentTarget != null
                ? TargetingManager.RangedChannel.CurrentTarget.position
                : Physics.Raycast(Cameras.aimingCamera.transform.position, Cameras.RealCamera.transform.forward, out RaycastHit hit, TargetingManager.RangedChannel.Range.maxDistance, hitScanLayerMask)
                    ? hit.point
                    : Cameras.aimingCamera.transform.position + (Cameras.RealCamera.transform.forward * TargetingManager.RangedChannel.Range.maxDistance);

            Self.MovementBody.Direction.Set
                ((targetPos.position - Self.Position).XZ(), playerRotationSpeed * Time.fixedDeltaTime);
        }
        else
        {
            targetPos.position = TargetingManager.RangedChannel.CurrentTarget != null
                ? targetPos.position = TargetingManager.RangedChannel.CurrentTarget.position
                : Self.Position + (Self.Transform.forward * TargetingManager.RangedChannel.Range.maxDistance);


        }
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

    public Grabbable currentGrabbed => Player.Grabber != null ? Player.Grabber.currentGrabbed : null;




    public void EnterAiming(State targetState)
    {
        if (Ammo.Max == 0 && currentGrabbed != null) return;

        SetAimDirection(Cameras.normalCamera.m_XAxis.Value, Cameras.normalCamera.m_YAxis.Value);
        Cameras.aimingCamera.CancelDamping();
        Cameras.aimingCamera.PreviousStateIsValid = false;
        TargetingManager.ToggleAimingDownSights(true);
        Self.Animator.CrossFade("Aim", 0.3f);
        targetState.Enter();
        aimingRig.enabled = true;
        aimingRig.weight = 1;
        UIHUDSystem.Instance.SetHitMarkerVisibility(true);
        Cameras.SetTargetVirtualCamera(Cameras.aimingCamera);
    }
    public void ExitAiming(State targetState)
    {
        if (!Self.StateMachine.Aiming) return;
        Cameras.normalCamera.m_XAxis.Value = hAxis.Value;
        TargetingManager.ToggleAimingDownSights(false);
        Self.Animator.CrossFade("GroundBasic", 0.1f);
        targetState.Enter();
        aimingRig.enabled = false;
        aimingRig.weight = 0;
        UIHUDSystem.Instance?.SetHitMarkerVisibility(false);
        Cameras.SetTargetVirtualCamera(Cameras.normalCamera);
    }

    public void ExitAimingAux()
    {
        if (!Self.StateMachine.Aiming) return;
        Cameras.normalCamera.m_XAxis.Value = hAxis.Value;
        TargetingManager.ToggleAimingDownSights(false);
        aimingRig.enabled = false;
        aimingRig.weight = 0;
        UIHUDSystem.Instance?.SetHitMarkerVisibility(false);
        Cameras.SetTargetVirtualCamera(Cameras.normalCamera);
        Self.StateMachine.IdleWalk.State.Enter();
    }

    public void ExitAimingInstant(bool _)
    {
        if (!Self.StateMachine.Aiming) return;
        Cameras.normalCamera.m_XAxis.Value = hAxis.Value;
        TargetingManager.ToggleAimingDownSights(false);
        aimingRig.enabled = false;
        aimingRig.weight = 0;
        UIHUDSystem.Instance?.SetHitMarkerVisibility(false);
        Cameras.SetTargetVirtualCamera(Cameras.normalCamera);
        Self.Animator.Play("GroundBasic");
        Self.StateMachine.IdleWalk.State.Enter();
    }

    public void SetAimDirection(float X, float Y)
    {
        aimRotationController.position = Self.MovementBody.Position + (Vector3.up * 0.915f);
        aimRotationController.localEulerAngles = new Vector3(Y, X, 0);
        hAxis.Value = X;
        vAxis.Value = Y;
    }


    public void TryShoot(State shootingState)
    {
        if (Ammo >= 1 && !shootingState.Active) shootingState.Enter();
    }

    public int totalEggsShot;

    public void ShootPoint()
    {
        totalEggsShot++;
        Self.Audio.PlayOneShot("EggShoot");
        realMuzzle.position = shootMuzzle.position;
        eggPool.Pump((p, proje) =>
        {
            p.gameObject.SetActive(true);
            proje.Send(TargetingManager.RangedChannel.CurrentTarget, realMuzzle, targetPos);
        }, realMuzzle);
        Ammo -= 1;
    }

    public void ThrowLassoPoint()
    {
        realMuzzle.position = shootMuzzle.position;
        LassoProjectile.Send(TargetingManager.RangedChannel.CurrentTarget, realMuzzle, targetPos);
    }

}