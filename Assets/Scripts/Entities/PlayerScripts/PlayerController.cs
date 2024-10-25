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
    public State airborneState;
    public State fallingState;
    public State glidingState;

    #endregion
    #region Data

    [HideInInspector] public float jumpInput;
    [HideInInspector] public Vector3 camAdjustedMovement;

    #endregion
    #region Getters

    #endregion

    public override void OnAwake()
    {
        input.jump.started += (_) => JumpPress();
    }

    private void OnDestroy()
    {
        input.jump.started -= (_) => JumpPress();
    }

    public override void OnUpdate()
    {
        if (jumpInput > 0) jumpInput -= Time.deltaTime;
        camAdjustedMovement = input.movement.ToXZ().Rotate(M.cameraTransform.eulerAngles.y, Vector3.up);

        if ((input.jump.IsPressed() && fallingState.active) || (!input.jump.IsPressed() && glidingState.active))
            TransitionTo(input.jump.IsPressed() ? glidingState : fallingState);

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
}
