using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using System;
using Cinemachine;
using System.Linq;
using SLS.ISingleton;
using AYellowpaper.SerializedCollections;
using RageRooster.Systems.SaveSystem;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerStateMachine : StateMachine, ISingleton<PlayerStateMachine>
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


    

    public void HaveDestroyed() { }

    protected override void PreSetup()
    {
        audio = GetComponent<AudioCaller>();
    }

    protected override void OnAwake()
    {
        // Initialize the Cinemachine FreeLook camera
        //freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
        //if (freeLookCamera != null)
        //{
        //    freeLookCamera.Follow = transform;
        //    freeLookCamera.LookAt = transform;
        //}

        whenInitializedEvent?.Invoke(this);

        PauseMenu.onPause += Pause;
        PauseMenu.onUnPause += UnPause;
    }

    private void OnDestroy()
    {
        PauseMenu.onPause -= Pause;
        PauseMenu.onUnPause -= UnPause; 
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
        Player.MovementBody.velocity = Vector3.zero;
        Player.MovementBody.CurrentSpeed = 0;
        Player.Animator.CrossFade("GroundBasic", .2f);
    }
    public void UnCutsceneState()
    {
        prevState.Enter();
    }

    public void DeathIfAtZero() { if (Player.Health.playerObject.GetCurrentHealth() == 0) Player.Death(); }

#if UNITY_EDITOR
    protected override void Update()
    {
        base.Update();
        //queuedSignals = signalQueue.ToList();
    }
    public List<string> queuedSignals;
#endif

    protected override void FixedUpdate()
    {
        //DebugRR.DebugTextOverlay.ClearText();
        base.FixedUpdate();
    }
}
