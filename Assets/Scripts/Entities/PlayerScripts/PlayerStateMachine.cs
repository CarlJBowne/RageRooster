using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using System;
using Cinemachine;
using System.Linq;
using AYellowpaper.SerializedCollections;

public class PlayerStateMachine : StateMachine
{
    #region Config

    [SerializeField] Upgrade[] upgrades;

    #endregion

    #region Data
    [HideInInspector] public Animator animator;
    [HideInInspector] public PlayerMovementBody body;
    [HideInInspector] public PlayerController controller;
    [HideInInspector] public PlayerHealth health;
    [HideInInspector] public PlayerRanged ranged;
    [HideInInspector] public new AudioCaller audio;
    public Transform cameraTransform;
    public CinemachineFreeLook freeLookCamera;
    public State pauseState;
    public State ragDollState;
    public RagdollHandler ragDollHandler;
    public float fallDownPitTime;
    public float deathTime;
    CoroutinePlus deathCoroutine;

    public SerializedDictionary<string, State> states = new SerializedDictionary<string, State>();

    #endregion

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
        Gameplay.Get().playerStateMachine = this;

        // Initialize the Cinemachine FreeLook camera
        freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
        if (freeLookCamera != null)
        {
            freeLookCamera.Follow = transform;
            freeLookCamera.LookAt = transform;
        }

#if UNITY_EDITOR
        Input.Get().Asset.FindAction("DebugActivate").performed += (_) => 
        {
            DEBUG_MODE_ACTIVE = !DEBUG_MODE_ACTIVE;
            for (int i = 0; i < upgrades.Length; i++) upgrades[i].EnableUpgrade();
        };
#endif

        whenInitializedEvent?.Invoke(this);
    }

    public static bool DEBUG_MODE_ACTIVE;



    public static Action<PlayerStateMachine> whenInitializedEvent;

    public bool IsStableForOriginShift() => states["Grounded"].enabled || CurrentState == states["Fall"] || states["Glide"];

    public void InstantMove(Vector3 newPosition, float? yRot = null)
    {
        Vector3 camDelta = newPosition - transform.position;
        //body.jiggles.PrepareTeleport();
        body.ForceSetPosition(newPosition);
        //body.jiggles.FinishTeleport(); 
        if (yRot != null) body.rotation = new(0, yRot.Value, 0);
        ResetState(); 
        freeLookCamera.PreviousStateIsValid = false;
        freeLookCamera.OnTargetObjectWarped(transform, camDelta);
        body.velocity = Vector3.zero;
    }
    public void InstantMove(SavePoint savePoint)
    {
        Vector3 camDelta = savePoint.SpawnPoint.position - transform.position;
        //body.jiggles.PrepareTeleport();
        body.ForceSetPosition(savePoint.SpawnPoint.position);
        body.rotation = new(0, savePoint.SpawnPoint.eulerAngles.y, 0);
        //body.jiggles.FinishTeleport();
        ResetState();
        ranged.Release(Vector3.zero, false);
        freeLookCamera.PreviousStateIsValid = false;
        freeLookCamera.OnTargetObjectWarped(transform, camDelta);
        body.velocity = Vector3.zero;
        body.InstantSnapToFloor();
        savePoint.onSpawnEvent?.Invoke();
    }

    public void ResetState()
    {
        Children[0].Enter();
        //signalReady = true;
        ragDollHandler.SetState(EntityState.Default);
        animator.enabled = true;
        animator.Play("GroundBasic");
    }

    private State prevState;
    public void PauseState()
    {
        prevState = CurrentState;
        pauseState.Enter();
        body.velocity = Vector3.zero;
        body.CurrentSpeed = 0;
        animator.CrossFade("GroundBasic", .2f);
    }
    public void UnPauseState()
    {
        prevState.Enter();
    }

    public void Death(bool justPit = false)
    {
        CoroutinePlus.Begin(ref deathCoroutine, Enum(justPit), Gameplay.Get());
        IEnumerator Enum(bool justPit)
        {
            Vector3 targetVelocity = body.velocity;
            audio.PlayOneShot("Death");
            ragDollState.Enter();
            body.velocity = Vector3.zero;
            ragDollHandler.SetState(EntityState.RagDoll);
            ragDollHandler.SetVelocity(targetVelocity*0.75f);
            animator.enabled = false;

            yield return WaitFor.SecondsRealtime(justPit ? fallDownPitTime : fallDownPitTime + 1);

            if (justPit)
            {
                yield return Overlay.OverGameplay.BasicFadeOutWait(.5f);
                yield return Gameplay.SpawnPlayer();
                Overlay.OverGameplay.BasicFadeIn(.5f);
            }
            else
            {
                yield return Overlay.OverGameplay.GameOverAnim();
                yield return WaitFor.SecondsRealtime(deathTime);
                yield return Overlay.OverMenus.BasicFadeOutWait(1f);
                PlayerHealth.Global.Update(PlayerHealth.Global.maxHealth);
                yield return Gameplay.DoReloadSave();
                Overlay.OverGameplay.Reset();
                yield return Gameplay.SpawnPlayer();
                Overlay.OverMenus.BasicFadeIn(1f);
            }
        }
    }

    public void DeathIfAtZero() { if (health.GetCurrentHealth() == 0) Death(); }

    public void FinishSignal() => SignalManager.FireSignal(new("Finish", ignoreLock: true));


#if UNITY_EDITOR
    protected override void Update()
    {
        base.Update();
        //queuedSignals = signalQueue.ToList();
    }
    public List<string> queuedSignals;
#endif
}
