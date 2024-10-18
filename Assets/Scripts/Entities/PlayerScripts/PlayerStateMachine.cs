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
    public Transform cameraTransform;

    #endregion



    public override void Initialize()
    {
        animator = GetComponent<Animator>();
        rb = GetComponent<Rigidbody>();
        collider = GetComponent<CapsuleCollider>();
        input = Input.Get(); 
        base.Initialize();
    }

}
