using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using System;
using Cinemachine;
using System.Linq;
using SLS.Singletons;
using RageRooster.Core.Save;
using RageRooster.World;
using SLS.StateMachineH.Signals;
using RageRooster.Core;
using Services = RageRooster.Services;
using static RageRooster.Player.Services;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerStateMachine : StateMachine, IPlayerStateMachine
{
    #region Config

    [field: SerializeField] public State Grounded { get; private set; }
    [field: SerializeField] public PlayerGroundMovement IdleWalk { get; private set; }
    [field: SerializeField] public State Airborne { get; private set; }
    [field: SerializeField] public PlayerAirborneMovement Jump { get; private set; }
    [field: SerializeField] public PlayerAirborneMovement Falling { get; private set; }
    [field: SerializeField] public PlayerAirborneMovement Gliding { get; private set; }
    [field: SerializeField] public PlayerAirborneMovement VentGliding { get; private set; }
    [field: SerializeField] public PlayerWallJump WallJump { get; private set; }
    [field: SerializeField] public State DropLaunch { get; private set; }
    [field: SerializeField] public State GroundParry { get; private set; }
    [field: SerializeField] public State AirParry { get; private set; }
    [field: SerializeField] public State GrabbedMovement { get; private set; }
    [field: SerializeField] public State Aiming { get; private set; }
    [field: SerializeField] public State Paused { get; private set; }
    [field: SerializeField] public State Ragdoll { get; private set; }


    #endregion

    #region Data
    [HideInInspector] public new AudioCaller audio;
    public Transform cameraTransform;
    #endregion


    static PlayerStateMachine instance;
    public static PlayerStateMachine Get => Singleton.Get(ref instance);
    public static bool TryGet(out PlayerStateMachine res) => Singleton.TryGet(Get, out res);


    public void HaveDestroyed() { }

    protected override void PreSetup()
    {
        audio = GetComponent<AudioCaller>();
    }

    protected override void OnAwake()
    {
        if (!Services.Gameplay.Active || RoomManager.currentRoom == null)
        {
            enabled = false;
            Services.Gameplay.onFinalAwake += OnAwake;
            return;
        }
        Services.Gameplay.onFinalAwake -= OnAwake;
        enabled = true;

        Singleton.Register(ref instance, this);

        whenInitializedEvent?.Invoke(this);

        Services.UI.OnPause += OnPause;
    }

    private void OnDestroy()
    {
        Services.UI.OnPause -= OnPause;

        Singleton.Deregister(ref instance, this);
    }


    public static Action<PlayerStateMachine> whenInitializedEvent;

    public bool IsStableForOriginShift() => Grounded.enabled || CurrentState == Falling.enabled || Gliding.enabled;

    public void ResetState()
    {
        Children[0].Enter();
        //signalReady = true;
        Player.RagdollHandler.State = RagdollHandler.States.Off;
        Player.Animator.enabled = true;
        Player.Animator.Play("GroundBasic");
    }

    public void OnPause(bool value)
    {
        this.enabled = !value;
        Player.MovementBody.enabled = !value;
    }

    private State prevState;
    public void CutsceneState()
    {
        prevState = CurrentState;
        Paused.Enter();
        Player.MovementBody.Velocity.ZeroOut();
        Player.Animator.CrossFade("GroundBasic", .2f);
    }
    public void UnCutsceneState()
    {
        prevState.Enter();
    }

    public void DeathIfAtZero() { if (Player.Health == 0) Player.Death(); }
}
