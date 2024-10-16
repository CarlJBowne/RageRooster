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
    [HideInInspector] public CharacterController charController;
    [HideInInspector] public Input input;
    [HideInInspector] public Transform cameraTransform;


    public Vector3 MovementControl => Input.Movement.ToXZ();
    public Vector3 MovementControlCameraAdjusted => Input.Movement.ToXZ().Rotated(cameraTransform.rotation.y, Direction.up);
    #endregion



    public override void Initialize()
    {
        base.Initialize();
        animator = GetComponent<Animator>();
        charController = GetComponent<CharacterController>();
        input = Input.Get(); 
        cameraTransform = Camera.main.transform;
    }

}
