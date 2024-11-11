using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV2;

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

    #endregion



    protected override void Initialize()
    {
        animator = GetComponent<Animator>();
        input = Input.Get();
        body = GetGlobalBehavior<PlayerMovementBody>();
        controller = GetGlobalBehavior<PlayerController>();
    }

    private void OnCollisionStay() => body.Collision();
    private void OnCollisionExit() => body.Collision();

}
public abstract class PlayerStateBehavior : StateBehavior
{
    [HideInInspector] public new PlayerStateMachine M;
    [HideInInspector] public Input input;
    [HideInInspector] public PlayerMovementBody body;
    [HideInInspector] public PlayerController controller;

    protected override void Initialize(StateMachine machine)
    {
        M = machine as PlayerStateMachine;
        input = M.input;
        body = M.body;
        controller = M.controller;
    }

}
