using SLS.StateMachineV2;
using UnityEngine;

public class PlayerAirborn : PlayerStateBehavior
{

    public float gravity = 9.81f;
    public float terminalVelocity = 100f;
    public bool flatGravity = false;
    //public bool overrideGravityOnStart = true;
    public float jumpHeight;
    public float jumpPower;
    public float jumpMinHeight;

    private float targetMinHeight;
    private float targetHeight;

    public override void FixedUpdate_S()
    {
        body.SetVelocity(y: ApplyGravity());

        if (jumpPower > 0 && transform.position.y < targetHeight) body.SetVelocity(y: jumpPower);
        if (body.velocity.y < 0) TransitionTo(body.FallOrGlide());

        if (!input.jump.IsPressed() && transform.position.y > targetMinHeight)
        {
            if (body.velocity.y > 0) body.SetVelocity(y: 0);
            TransitionTo(body.fallState);
        }

    }

    public override void OnEnter()
    {
        if (jumpPower <= 0) return;
        targetMinHeight = transform.position.y + jumpMinHeight;
        targetHeight = body.position.y + jumpHeight - (jumpPower.P() / (2 * gravity));
        body.SetVelocity(y: jumpPower);
    }
    //public override void OnExit() => movement.currentGravity = movement.defaultGravity;

    private float ApplyGravity()
    {
        return  (!flatGravity 
            ? body.velocity.y - (gravity * Time.deltaTime) 
            : -gravity * Time.deltaTime
            ).Min(-terminalVelocity);
    }

}
