using SLS.StateMachineV2;
using UnityEngine;
using System.Linq;
using EditorAttributes;
using UnityEngine.InputSystem;

public class PlayerController : PlayerStateBehavior
{
    #region Config

    public float jumpBuffer = 0.3f;

    public State groundedState;
    public State idleWalkState;
    public State chargingState;
    public State airborneState;
    public State fallingState;
    public State glidingState;
    public State groundSlamState;

    #endregion
    #region Data

    [HideInInspector] public float jumpInput;
    [HideInInspector] public Vector3 camAdjustedMovement;
    [HideInInspector] public PlayerGrabber grabber;
    //[HideInInspector] public PlayerAttackSystem attack;

    #endregion
    #region Getters

    #endregion

    public override void OnAwake()
    {
        grabber = GetComponentFromMachine<PlayerGrabber>();
        //attack = GetComponentFromMachine<PlayerAttackSystem>();

        input.jump.started += (_) => JumpPress();
        input.grab.started += (_) => grabber.GrabButtonPress();
        input.attack.started += (_) => PunchButtonPress();
        input.attack.started += (_) => ChargeButtonPress();
        //input.attack.started += (_) => attack.AttackButtonPress();
    }

    private void OnDestroy()
    {
        input.jump.started -= (_) => JumpPress();
    }

    public override void OnUpdate()
    {
        if (jumpInput > 0) jumpInput -= Time.deltaTime;
        camAdjustedMovement = input.movement.ToXZ().Rotate(M.cameraTransform.eulerAngles.y, Vector3.up);

        if ((input.jump.IsPressed() && fallingState.active && !grabber.currentGrabbed) || (!input.jump.IsPressed() && glidingState.active))
            TransitionTo(input.jump.IsPressed() ? glidingState : fallingState);
        M.animator.SetBool("Gliding", glidingState.active);

        if (input.charge.IsPressed() && groundedState.active || !input.charge.IsPressed() && chargingState.active)
            TransitionTo(input.charge.IsPressed() ? chargingState : idleWalkState);

    }

    private void JumpPress()
    {
        if (groundedState.active || (airborneState.active && body.coyoteTimeLeft > 0)) body.BeginJump();
        else jumpInput = jumpBuffer + Time.fixedDeltaTime;
    }

    public bool CheckJumpBuffer()
    {
        bool result = jumpInput > 0;
        jumpInput = 0;
        return result;
    }

    public void PunchButtonPress()
    {
        if (airborneState.active) groundSlamState.TransitionTo();
    }

    public void ChargeButtonPress()
    {
        if (airborneState.active)
        {

        }
    }

}
