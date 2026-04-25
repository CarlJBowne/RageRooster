using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using RageRooster.Obsolete.Zones;
using Utilities.Xtensions.Unity;

public class PlayerAirborneMovement : PlayerMovementEffector
{

    public PlayerMovementBody.JumpState defaultPhase = PlayerMovementBody.JumpState.Jumping;
    public float jumpHeight;
    public float jumpPower;
    public float jumpMinHeight;
    public float gravity = 9.81f;
    public float terminalVelocity = 100f;
    public bool flatGravity = false;
    public bool allowMidFall = true;
    public bool allowDoubleJump = true;
    //public bool allowGlide = false; //Keeping this here for now in case we decide to re-implement the gliding.

    public PlayerAirborneMovement fallState;
    public float fallStateThreshold = 0;

    protected float targetMinHeight;
    protected float targetHeight;
    public bool isUpward => defaultPhase == PlayerMovementBody.JumpState.Jumping;

    private void Update()
    {
        
    }

    public override void VerticalMovement(out float? result)
    {
        result = ApplyGravity(gravity, terminalVelocity, flatGravity);
        if (isUpward) VerticalUpwards(ref result);
        else if (playerMovementBody.velocity.y <= fallStateThreshold && fallState != this) Fall(ref result);

    }

    protected virtual void VerticalUpwards(ref float? Y)
    {
        if (playerMovementBody.JumpStateCurrent == PlayerMovementBody.JumpState.Jumping && transform.position.y >= targetMinHeight) 
            playerMovementBody.UnLand(PlayerMovementBody.JumpState.Decelerating);
        if (playerMovementBody.JumpStateCurrent == PlayerMovementBody.JumpState.Decelerating && transform.position.y >= targetHeight) 
            playerMovementBody.UnLand(PlayerMovementBody.JumpState.Falling);

        if (playerMovementBody.JumpStateCurrent < PlayerMovementBody.JumpState.Decelerating) 
            Y = jumpPower;
        if (playerMovementBody.JumpStateCurrent > PlayerMovementBody.JumpState.Jumping &&
           (playerMovementBody.velocity.y <= fallStateThreshold || (allowMidFall && !Input.Jump.IsPressed())))
            Fall(ref Y);

    }

    protected virtual void Fall(ref float? Y)
    {
        if (playerMovementBody.velocity.y > fallStateThreshold) Y = fallStateThreshold;
        playerMovementBody.UnLand(PlayerMovementBody.JumpState.Falling);
        if (fallState != null) fallState.Enter();
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        if (!isFinal) return;

        PrepPhase(out PlayerMovementBody.JumpState nextJumpPhase);

        playerMovementBody.UnLand(nextJumpPhase);
        switch (nextJumpPhase)
        {
            case PlayerMovementBody.JumpState.Jumping: StartFrom_Jump(); break;
            case PlayerMovementBody.JumpState.Decelerating: StartFrom_Decel(); break;
            case PlayerMovementBody.JumpState.Falling: StartFrom_Falling(); break;
        }
    }

    protected virtual void PrepPhase(out PlayerMovementBody.JumpState nextJumpPhase)
    {
        nextJumpPhase = defaultPhase;
        if (nextJumpPhase < PlayerMovementBody.JumpState.Jumping)
        {
            nextJumpPhase = playerMovementBody.JumpStateCurrent;
            if (nextJumpPhase < PlayerMovementBody.JumpState.Jumping) nextJumpPhase = PlayerMovementBody.JumpState.Jumping;
        }
    }

    protected virtual void StartFrom_Jump()
    {
        playerMovementBody.VelocitySet(y: jumpPower);
        targetMinHeight = transform.position.y + jumpMinHeight;
        targetHeight = (transform.position.y + jumpHeight) - (jumpPower.P()) / (2 * gravity);
        if (targetHeight <= transform.position.y)
        {
            playerMovementBody.VelocitySet(y: Mathf.Sqrt(2 * gravity * jumpHeight));
            targetMinHeight = transform.position.y;
        }

#if UNITY_EDITOR
        playerMovementBody.jumpMarkers = new()
                {
                    transform.position,
                    transform.position + Vector3.up * targetHeight,
                    transform.position + Vector3.up * jumpHeight
                };
#endif
    }
    protected virtual void StartFrom_Decel()
    {

    }
    protected virtual void StartFrom_Falling()
    {
        playerMovementBody.VelocitySet(y: playerMovementBody.velocity.y.Max(0));
    }








    public void Enter() => State.Enter();
    public virtual void BeginJump()
    {
        if (!State) State.Enter();
    }
    public virtual void BeginJump(float power, float height, float minHeight)
    {
        if (!isUpward) throw new System.Exception("This isn't an Upward Item.");
        jumpPower = power;
        jumpHeight = height;
        jumpMinHeight = minHeight;

        State.Enter();
    }
    public virtual void BeginJump(PlayerMovementBody.JumpState newState)
    {
        PlayerMovementBody.JumpState skippedDefault = defaultPhase;
        defaultPhase = newState;
        State.Enter();
        defaultPhase = skippedDefault;
    }

    public virtual void BeginDoubleJump(float power, float height, float minHeight)
    {
        if (!allowDoubleJump)
            return;
        else
        {
            jumpPower = power;
            jumpHeight = height;
            jumpMinHeight = minHeight;

            State.Enter();
            allowDoubleJump = false;
        }
    }
}

