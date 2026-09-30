using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using SLS.Physics3D;
using static RageRooster.Player.Services;
public class PlayerAirborneMovement : PlayerMovementEffector
{

    public JumpPhase defaultPhase = JumpPhase.Jumping;
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
    public bool isUpward => defaultPhase == JumpPhase.Jumping;

    private void Update()
    {
        
    }

    public override bool VerticalMovement(out float result)
    {
        result = ApplyGravity(gravity, terminalVelocity, flatGravity);
        if (isUpward) VerticalUpwards(ref result);
        else if (Player.MovementBody.Velocity.y <= fallStateThreshold && fallState != this) Fall(ref result);
        return true;
    }

    protected virtual void VerticalUpwards(ref float Y)
    {
        if (Player.MovementBody.JumpPhase == JumpPhase.Jumping && transform.position.y >= targetMinHeight) 
            Player.MovementBody.UnLand(JumpPhase.Decelerating);
        if (Player.MovementBody.JumpPhase == JumpPhase.Decelerating && transform.position.y >= targetHeight) 
            Player.MovementBody.UnLand(JumpPhase.Falling);

        if (Player.MovementBody.JumpPhase < JumpPhase.Decelerating) 
            Y = jumpPower;
        if (Player.MovementBody.JumpPhase > JumpPhase.Jumping &&
           (Player.MovementBody.Velocity.y <= fallStateThreshold || (allowMidFall && !Input.Jump.IsPressed())))
            Fall(ref Y);

    }

    protected virtual void Fall(ref float Y)
    {
        if (Player.MovementBody.Velocity.y > fallStateThreshold) Y = fallStateThreshold;
        Player.MovementBody.UnLand(JumpPhase.Falling);
        if (fallState != null) fallState.Enter();
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        if (!isFinal) return;

        PrepPhase(out JumpPhase nextJumpPhase);

        Player.MovementBody.UnLand(nextJumpPhase);
        switch (nextJumpPhase)
        {
            case JumpPhase.Jumping: StartFrom_Jump(); break;
            case JumpPhase.Decelerating: StartFrom_Decel(); break;
            case JumpPhase.Falling: StartFrom_Falling(); break;
        }
    }

    protected virtual void PrepPhase(out JumpPhase nextJumpPhase)
    {
        nextJumpPhase = defaultPhase;
        if (nextJumpPhase < JumpPhase.Jumping)
        {
            nextJumpPhase = Player.MovementBody.JumpPhase;
            if (nextJumpPhase < JumpPhase.Jumping) nextJumpPhase = JumpPhase.Jumping;
        }
    }

    protected virtual void StartFrom_Jump()
    {
        Player.MovementBody.Velocity.y = jumpPower;
        targetMinHeight = transform.position.y + jumpMinHeight;
        targetHeight = (transform.position.y + jumpHeight) - (jumpPower.P()) / (2 * gravity);
        if (targetHeight <= transform.position.y)
        {
            Player.MovementBody.Velocity.y = Mathf.Sqrt(2 * gravity * jumpHeight);
            targetMinHeight = transform.position.y;
        }

#if UNITY_EDITOR
        Player.MovementBody.Debug.PlaceJumpMarker(targetHeight, jumpHeight);
#endif
    }
    protected virtual void StartFrom_Decel()
    {

    }
    protected virtual void StartFrom_Falling()
    {
        Player.MovementBody.Velocity.y = Player.MovementBody.Velocity.y.Max(0);
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
    public virtual void BeginJump(JumpPhase newState)
    {
        JumpPhase skippedDefault = defaultPhase;
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

