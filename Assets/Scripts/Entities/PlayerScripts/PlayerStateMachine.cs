using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;
using System;
using Cinemachine;
using System.Linq;

public class PlayerStateMachine : StateMachine
{
    #region Config

    #endregion

    #region Data
    [HideInInspector] public Animator animator;
    [HideInInspector] public Input input;
    [HideInInspector] public PlayerMovementBody body;
    [HideInInspector] public PlayerController controller;
    public Transform cameraTransform;
    public CinemachineFreeLook freeLookCamera;
    public State pauseState;

    #endregion

    protected override void Initialize()
    {
        animator = GetComponent<Animator>();
        input = Input.Get();
        body = GetComponent<PlayerMovementBody>();
        controller = GetComponent<PlayerController>();
        whenInitializedEvent?.Invoke(this);

        // Initialize the Cinemachine FreeLook camera
        freeLookCamera = FindObjectOfType<CinemachineFreeLook>();
        if (freeLookCamera != null)
        {
            freeLookCamera.Follow = transform;
            freeLookCamera.LookAt = transform;
        }

        #if UNITY_EDITOR
        input.asset.Gameplay.DebugActivate.performed += (_) => { DEBUG_MODE_ACTIVE = !DEBUG_MODE_ACTIVE; };
        #endif
    }

    public static bool DEBUG_MODE_ACTIVE;



    public static Action<PlayerStateMachine> whenInitializedEvent;

    public bool IsStableForOriginShift() => states["Grounded"].enabled || currentState == states["Fall"] || states["Glide"];

    public void InstantMove(Vector3 newPosition, float? yRot = null)
    {
        Vector3 camDelta = newPosition - transform.position;
        body.jiggles.PrepareTeleport();
        body.position = newPosition;
        body.jiggles.FinishTeleport();
        if (yRot != null) body.rotation = new(0, yRot.Value, 0);
        ResetState();
        freeLookCamera.PreviousStateIsValid = false;
        freeLookCamera.OnTargetObjectWarped(transform, camDelta);
        body.velocity = Vector3.zero;
    }
    public void InstantMove(SavePoint savePoint)
    {
        Vector3 camDelta = savePoint.SpawnPoint.position - transform.position;
        body.jiggles.PrepareTeleport();
        body.position = savePoint.SpawnPoint.position;
        body.rotation = new(0, savePoint.SpawnPoint.eulerAngles.y, 0);
        body.jiggles.FinishTeleport();
        ResetState();
        freeLookCamera.PreviousStateIsValid = false;
        freeLookCamera.OnTargetObjectWarped(transform, camDelta);
        body.velocity = Vector3.zero;
        body.InstantSnapToFloor();
        savePoint.onSpawnEvent?.Invoke();
    }

    public void ResetState()
    {
        children[0].TransitionTo();
        signalReady = true;
    }

    private State prevState;
    public void PauseState()
    {
        prevState = currentState;
        pauseState.TransitionTo();
    }
    public void UnPauseState()
    {
        prevState.TransitionTo();
    }

#if UNITY_EDITOR
    protected override void Update()
    {
        base.Update();
        queuedSignals = signalQueue.ToList();
    }
    public List<string> queuedSignals;
#endif
}
