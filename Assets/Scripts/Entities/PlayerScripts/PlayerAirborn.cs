using SLS.StateMachineV2;
using UnityEngine;

public class PlayerAirborn : PlayerStateBehavior
{

    public float gravity = 9.81f;
    public float terminalVelocity = 100f;
    public bool flatGravity = false;
    public float jumpHeight;
    public float jumpPower;
    public float jumpMinHeight;
    public bool allowMidFall = true;
    public PlayerAirborn fallState;

    protected float targetMinHeight;
    protected float targetHeight;

    public override void OnFixedUpdate()
    {
        body.VelocitySet(y: ApplyGravity());

        if (jumpHeight <= 0) return;

        if (transform.position.y < targetHeight) body.VelocitySet(y: jumpPower);
        if (fallState != null && body.velocity.y < 0) TransitionTo(fallState.state);

        if (allowMidFall && !input.jump.IsPressed() && transform.position.y > targetMinHeight)
        {
            if (body.velocity.y > 0) body.VelocitySet(y: 0);
            TransitionTo(fallState.state);
        }
    }

    public override void OnEnter()
    {
        if (jumpPower == 0) return;
        body.VelocitySet(y: jumpPower);
        if (jumpPower <= 0) return;
        targetMinHeight = transform.position.y + jumpMinHeight;
        targetHeight = (transform.position.y + jumpHeight) - (jumpPower.P()) / (2 * gravity);

        body.jumpMarkers = new()
        {
            transform.position,
            transform.position + Vector3.up * targetHeight,
            transform.position + Vector3.up * jumpHeight
        };
    }

    protected float ApplyGravity()
    {
        return  (!flatGravity 
            ? body.velocity.y - (gravity * Time.deltaTime) 
            : -gravity * Time.deltaTime
            ).Min(-terminalVelocity);
    }

    public void BeginJump() => state.TransitionTo();
    public void BeginJump(float power, float height, float minHeight)
    {
        jumpPower = power;
        jumpHeight = height;
        jumpMinHeight = minHeight;

        BeginJump();
    }
}
