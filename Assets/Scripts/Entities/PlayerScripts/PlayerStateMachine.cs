using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using System;
using Cinemachine;
using System.Linq;
using Utilities.Singletons;
using AYellowpaper.SerializedCollections;
using RageRooster.Systems.SaveSystem;
using RageRooster.RoomSystem;
using SLS.StateMachineH.Signals;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerStateMachine : StateMachine
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
        //if (!Gameplay.Active || RoomManager.currentRoom == null)
        //{
        //    enabled = false;
        //    Gameplay.onFinalAwake += OnAwake;
        //    return;
        //}
        //Gameplay.onFinalAwake -= OnAwake;
        //enabled = true;

        Singleton.Register(ref instance, this);

        whenInitializedEvent?.Invoke(this);

        PauseMenu.onPause += Pause;
        PauseMenu.onUnPause += UnPause;
    }

    private void OnDestroy()
    {
        PauseMenu.onPause -= Pause;
        PauseMenu.onUnPause -= UnPause;

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

    public void Pause()
    {
        this.enabled = false;
        Player.MovementBody.enabled = false;
    }
    public void UnPause()
    {
        this.enabled = true;
        Player.MovementBody.enabled = true;
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

    public void DeathIfAtZero() { if (Player.Health.playerObject.GetCurrentHealth() == 0) Player.Death(); }

#if UNITY_EDITOR

    [ContextMenu("UpgradeSignals")]
    public void UpgradeSignals()
    {
        SignalManager_Old OldMan = gameObject.GetComponent<SignalManager_Old>(); //I'm old!

        SignalManager NewMan = gameObject.AddComponent<SignalManager>();

        SignalManager.Transfer(OldMan, NewMan);

        DestroyImmediate(OldMan);

        var oldNodes = gameObject.GetComponentsInChildren<SignalNode_Old>();
        var states = oldNodes.Select(x => x.State).ToArray();
        var newNodes = states.Select(x => x.gameObject.AddComponent<SignalNode>()).ToArray();

        for (int i = 0; i < states.Length; i++)
        {
            SignalNode.Transfer(oldNodes[i], newNodes[i]);
            DestroyImmediate(oldNodes[i]);
        }

        var oldAnims = gameObject.GetComponentsInChildren<StateAnimator_Legacy>();
        states = oldAnims.Select(x => x.State).ToArray();
        var newAnims = states.Select(x => x.gameObject.AddComponent<StateAnimator>()).ToArray();

        for (int i = 0; i < states.Length; i++)
        {
            StateAnimator.Transfer(oldAnims[i], newAnims[i]);
            DestroyImmediate(oldAnims[i]);
        }
    }
#endif
}
