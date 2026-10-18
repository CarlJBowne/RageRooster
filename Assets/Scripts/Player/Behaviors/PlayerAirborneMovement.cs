using EditorAttributes;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;
using SLS.Physics3D;
using static RageRooster.Player.LocalContext;
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


    public PlayerAirborneMovement fallState;
    public float fallStateThreshold = 0;

    public JumpPhase Phase 
    { 
        get; 
        protected set; 
    } = JumpPhase.Grounded;
    protected float targetMinHeight;
    protected float targetHeight;
    public bool isUpward => defaultPhase == JumpPhase.Jumping;

    public override bool VerticalMovement(out float result)
    {
        result = Player.MovementBody.Velocity.y;
        if (Phase is JumpPhase.Jumping or JumpPhase.Decelerating)
        {
            VerticalUpwards(ref result);
        }
        else if (Phase is JumpPhase.Falling)
        {
            result = ApplyGravity(gravity, terminalVelocity, flatGravity);
            if (result <= -terminalVelocity) Phase = JumpPhase.TerminalVelocity;
        }
        return true;
    }

    protected virtual void VerticalUpwards(ref float Y)
    {
        Y = Phase is JumpPhase.Jumping ? jumpPower : ApplyGravity(gravity, terminalVelocity, flatGravity);

        if (Phase is JumpPhase.Jumping && transform.position.y >= targetMinHeight) Phase = JumpPhase.Decelerating;

        if ((Phase == JumpPhase.Jumping && allowMidFall && !Input.Jump.IsPressed()) ||
        (Phase == JumpPhase.Decelerating && Y <= fallStateThreshold)) Fall(ref Y);
    }

    protected virtual void Fall(ref float Y)
    {
        Phase = JumpPhase.Falling;
        if (Y > fallStateThreshold) Y = fallStateThreshold;
        if (fallState != null) fallState.Enter();
    }

    protected override void OnEnter(State prev, bool isFinal)
    {
        base.OnEnter(prev, isFinal);
        if (!isFinal) return;

        Player.MovementBody.Anchor.DeAnchor();

        if (Phase == JumpPhase.Grounded) Phase = defaultPhase; 

        switch (Phase)
        {
            case JumpPhase.Jumping: StartFrom_Jump(); break;
            case JumpPhase.Decelerating: StartFrom_Decel(); break;
            case JumpPhase.Falling: StartFrom_Falling(); break;
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
    public void Enter(JumpPhase jumpPhase)
    {
        this.Phase = jumpPhase;
        State.Enter();
    }

    public virtual void BeginJump()
    {
        if (!State) State.Enter();
    }
    public virtual void BeginJump(float power, float height, float minHeight)
    {
        if (!isUpward) throw new System.Exception("This isn't an Upward Item.");

        float prevJumpPower = jumpPower, prevJumpHeight = jumpHeight, prevJumpMinHeight = jumpMinHeight;

        jumpPower = power;
        jumpHeight = height;
        jumpMinHeight = minHeight;

        State.Enter();

        jumpPower = prevJumpPower;
        jumpHeight = prevJumpHeight;
        jumpMinHeight = prevJumpMinHeight;
    }
    public virtual void BeginJump(JumpPhase newState)
    {
        this.Phase = newState;
        State.Enter();
    }

    [System.Obsolete("Find some better way to handle Double Jumping ability")]
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

    protected override void OnExit(State next) => Phase = JumpPhase.Grounded;
}

