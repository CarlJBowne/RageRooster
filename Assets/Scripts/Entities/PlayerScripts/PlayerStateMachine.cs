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
    //[HideInInspector] public CharacterController charController;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public new CapsuleCollider collider;
    [HideInInspector] public Input input;
    [HideInInspector] public PlayerMovementBody body;
    public Transform cameraTransform;

    #endregion



    protected override void Initialize()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        collider = GetComponent<CapsuleCollider>();
        input = Input.Get();
        body = GetGlobalBehavior<PlayerMovementBody>();
    }


}
public abstract class PlayerStateBehavior : StateBehavior
{
    [HideInInspector] public new PlayerStateMachine M;
    [HideInInspector] public Input input;
    [HideInInspector] public PlayerMovementBody body;
    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public new CapsuleCollider collider;
    [HideInInspector] public Animator animator;

    protected override void Initialize(StateMachine machine)
    {
        M = machine as PlayerStateMachine;
        input = M.input;
        body = M.body;
        rb = M.rb;
        collider = M.collider;
        animator = M.animator;
    }

}
