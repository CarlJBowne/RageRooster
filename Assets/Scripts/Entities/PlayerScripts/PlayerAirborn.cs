using SLS.StateMachineV2;
using UnityEngine;

public class PlayerAirborn : StateBehavior
{
    PlayerMovementBody movement;

    public float gravity = 9.81f;
    public float terminalVelocity = 100f;
    public bool flatGravity = false;
    //public bool overrideGravityOnStart = true;
    public float jumpHeight;
    public float jumpPower;
    public float jumpMinHeight;

    private float targetMinHeight;
    private float targetHeight;

    public override void Awake_S() => M.TryGetGlobalBehavior(out movement);

    public override void FixedUpdate_S()
    {
        movement.SetVelocity(y: ApplyGravity());

        if (jumpPower > 0 && transform.position.y < targetHeight) movement.SetVelocity(y: jumpPower);
        if (movement.velocity.y < 0) TransitionTo(movement.FallOrGlide());

        if (!Input.Jump.IsPressed() && transform.position.y > targetMinHeight)
        {
            if (movement.velocity.y > 0) movement.SetVelocity(y: 0);
            TransitionTo(movement.fallState);
        }

    }

    public override void OnEnter()
    {
        if (jumpPower <= 0) return;
        targetMinHeight = transform.position.y + jumpMinHeight;
        targetHeight = movement.position.y + jumpHeight - (jumpPower.P() / (2 * gravity));
        movement.SetVelocity(y: jumpPower);
    }
    //public override void OnExit() => movement.currentGravity = movement.defaultGravity;

    private float ApplyGravity()
    {
        return  (!flatGravity 
            ? movement.velocity.y - (gravity * Time.deltaTime) 
            : -gravity * Time.deltaTime
            ).Min(-terminalVelocity);
    }

}
