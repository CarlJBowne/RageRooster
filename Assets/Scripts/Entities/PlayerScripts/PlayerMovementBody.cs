using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV2;

public class PlayerMovementBody : StateBehavior
{
    new PlayerStateMachine M => base.M as PlayerStateMachine;

    #region Config
    public float acceleration;
    public float maxSpeed;
    public float decceleration;
    public float defaultGravity = 0;
    public float maxDownwardVelocity = 100f;
    public float coyoteTime = 0.5f;
    public float jumpBuffer = 1.5f;
    public float tripleJumpTime = 1f;
    public State groundedState;
    public State walkFallingState;
    public State[] jumpingStates;
    #endregion

    #region Data
    [HideInInspector] public Vector3 velocity;
    [HideInInspector] public float currentGravity = 0;
    [HideInInspector] public float defaultMaxSpeed;
    [HideInInspector] public bool baseMovability = true;
    [HideInInspector] public bool canJump = true;
    [HideInInspector] public bool grounded;
    [HideInInspector] public float jumpInput;
    [HideInInspector] public float coyoteTimeLeft;
    [HideInInspector] public int currentJump;
    [HideInInspector] public float tripleJumpTimeLeft;
    #endregion

    public override void Update_S()
    {
        if (Input.Jump.WasPressedThisFrame()) jumpInput = jumpBuffer + Time.fixedDeltaTime;
        if (jumpInput > 0) jumpInput -= Time.deltaTime;
    }

    public override void FixedUpdate_S()
    {
        if(baseMovability) BaseHorizontalMovement();

        velocity.y -= currentGravity;

        if(canJump) JumpCheck();
        if (grounded != M.charController.isGrounded) GroundedChange(M.charController.isGrounded);

        if(velocity.y < -maxDownwardVelocity) velocity.y = -maxDownwardVelocity;
        M.charController.Move(velocity * Time.fixedDeltaTime); 
    }




    private void BaseHorizontalMovement()
    {
        Vector3 movement = M.MovementControlCameraAdjusted;

        Vector3 preVelocity = new(velocity.x, 0, velocity.z);

        if (movement.magnitude > 0)
        {
            preVelocity += movement * acceleration;
            if (preVelocity.magnitude > maxSpeed)
                preVelocity = preVelocity.normalized * maxSpeed;
        }
        else preVelocity -= preVelocity * decceleration;

        velocity = preVelocity + Vector3.up * velocity.y;
    }

    private void GroundedChange(bool value)
    {
        grounded = value;
        if (!value) coyoteTimeLeft = coyoteTime;
        else tripleJumpTimeLeft = tripleJumpTime;
        TransitionTo(value ? groundedState : walkFallingState);
    }

    private void JumpCheck()
    {
        if (walkFallingState.active && coyoteTimeLeft > 0) coyoteTimeLeft -= Time.deltaTime;

        if (groundedState.active && tripleJumpTimeLeft > 0) 
        { 
            tripleJumpTimeLeft -= Time.deltaTime;
            if (tripleJumpTimeLeft <= 0) currentJump = 1;
        }

        if (jumpInput > 0 && (groundedState.active || (walkFallingState.active && coyoteTimeLeft > 0)))
        {
            jumpInput = 0;
            TransitionTo(jumpingStates[currentJump-1]);
            currentJump = currentJump == 3 ? 1 : currentJump + 1;
            grounded = false;
        }
    }

}
