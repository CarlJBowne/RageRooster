using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerStateMachine : MonoBehaviour
{
    public Animator _animator;
    public CharacterController _characterController;
    public PlayerInput _playerInput;

    public ICharacterState CurrentState { get; private set; }
    public IdleState IdleState = new IdleState();
    public WalkingState WalkingState = new WalkingState();
    public RunningState RunningState = new RunningState();
    public JumpingState JumpingState = new JumpingState();
    public FallingState FallingState = new FallingState();

    void Awake()
    {
        _playerInput = new PlayerInput();
        _characterController = GetComponent<CharacterController>();
        _animator = GetComponent<Animator>();

        _isWalkingHash = Animator.StringToHash("isWalking");
        _isRunningHash = Animator.StringToHash("isRunning");
        _isJumpingHash = Animator.StringToHash("isJumping");
        _jumpCountHash = Animator.StringToHash("jumpCount");

        _playerInput.CharacterControls.Move.started += onMovementInput;
        _playerInput.CharacterControls.Move.canceled += onMovementInput;
        _playerInput.CharacterControls.Move.performed += onMovementInput;
        _playerInput.CharacterControls.Run.started += onRun;
        _playerInput.CharacterControls.Run.canceled += onRun;
        _playerInput.CharacterControls.Jump.started += onJump;
        _playerInput.CharacterControls.Jump.canceled += onJump;

        SetupJumpVariables();

        TransitionToState(IdleState);
    }

    void Update()
    {
        CurrentState.UpdateState(this);
    }

    public void TransitionToState(ICharacterState newState)
    {
        CurrentState?.ExitState(this);
        CurrentState = newState;
        CurrentState.EnterState(this);
    }

    // Existing methods like onMovementInput, onRun, onJump, SetupJumpVariables, etc.
}
