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

    #endregion

    #region Data
    [HideInInspector] public Animator animator;
    [HideInInspector] public PlayerMovementBody body;
    [HideInInspector] public PlayerController controller;
    [HideInInspector] public PlayerHealth health;
    [HideInInspector] public PlayerRanged ranged;
    [HideInInspector] public new AudioCaller audio;
    public Transform cameraTransform;
    //public CinemachineFreeLook freeLookCamera;
    public State pauseState;
    public State ragDollState;

    public SerializedDictionary<string, State> states = new SerializedDictionary<string, State>();

    #endregion


    

    public void HaveDestroyed() { }

    protected override void PreSetup()
    {
        animator = GetComponent<Animator>();
        body = GetComponent<PlayerMovementBody>();
        controller = GetComponent<PlayerController>();
        health = GetComponent<PlayerHealth>();
        audio = GetComponent<AudioCaller>();
        ranged = GetComponent<PlayerRanged>();
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

#if UNITY_EDITOR
        Input.Get().Asset.FindAction("DebugActivate").performed += (_) => 
        {
            SaveFile.Current.playerStats.upgrades = Upgrades.Debug();
        };
#endif

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

    public bool IsStableForOriginShift() => states["Grounded"].enabled || CurrentState == states["Fall"] || states["Glide"];

    public void ResetState()
    {
        Children[0].Enter();
        //signalReady = true;
        Player.RagdollHandler.SetState(EntityState.Default);
        animator.enabled = true;
        animator.Play("GroundBasic");
    }

    public void Pause()
    {
        this.enabled = false;
        body.enabled = false;
    }
    public void UnPause()
    {
        this.enabled = true;
        body.enabled = true;
    }

    private State prevState;
    public void CutsceneState()
    {
        prevState = CurrentState;
        pauseState.Enter();
        body.velocity = Vector3.zero;
        body.CurrentSpeed = 0;
        animator.CrossFade("GroundBasic", .2f);
    }
    public void UnCutsceneState()
    {
        prevState.Enter();
    }

    public void DeathIfAtZero() { if (health.GetCurrentHealth() == 0) Player.Death(); }


#if UNITY_EDITOR
    protected override void Update()
    {
        base.Update();
        //queuedSignals = signalQueue.ToList();
    }
    public List<string> queuedSignals;
#endif
}
