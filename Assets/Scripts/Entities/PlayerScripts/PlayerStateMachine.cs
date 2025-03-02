using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;
using System;
using Cinemachine;

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
        body.position = newPosition;
        if (yRot != null) body.rotation = new(0, yRot.Value, 0);
        freeLookCamera.PreviousStateIsValid = false;
        freeLookCamera.OnTargetObjectWarped(transform, camDelta);
        body.velocity = Vector3.zero;
    }
    public void InstantMove(SavePoint savePoint)
    {
        Vector3 camDelta = savePoint.SpawnPoint.position - transform.position;
        body.position = savePoint.SpawnPoint.position;
        body.rotation = new(0, savePoint.SpawnPoint.eulerAngles.y, 0);
        freeLookCamera.PreviousStateIsValid = false;
        freeLookCamera.OnTargetObjectWarped(transform, camDelta);
        body.velocity = Vector3.zero;
    }
    public void Spawn(SavePoint spawn)
    {
        Vector3 camDelta = spawn.SpawnPoint.position - transform.position;
        body.position = spawn.SpawnPoint.position;
        body.rotation = new(0, spawn.SpawnPoint.eulerAngles.y, 0);
        freeLookCamera.PreviousStateIsValid = false;
        freeLookCamera.OnTargetObjectWarped(transform, camDelta);
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

}
public abstract class PlayerStateBehavior : StateBehavior
{
    [HideInInspector] public new PlayerStateMachine M;
    [HideInInspector] public Input input;
    [HideInInspector] public PlayerMovementBody body;
    [HideInInspector] public PlayerController controller;
    

    protected override void Initialize()
    {
        M = base.M as PlayerStateMachine;
        input = M.input;
        body = M.body;
        controller = M.controller;
    }
        

    #region States

    public State sGrounded => M.states["Grounded"];
    public State sCharge => M.states["Charge"];
    public State sAirborne => M.states["Airborne"];
    public State sFall => M.states["Fall"];
    public State sGlide => M.states["Glide"];
    public State sGroundSlam => M.states["GroundSlam"];
    public State sAirChargeFall => M.states["AirChargeFall"];

    #endregion

}
