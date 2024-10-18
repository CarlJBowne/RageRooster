using SLS.StateMachineV2;
using UnityEngine;

public class PlayerAirborn : StateBehavior
{
    PlayerMovementBody movement;

    public float gravity = 9.81f;
    public bool isJump;
    public float jumpHeight;
    public float jumpPower;
    public float jumpMinHeight;
    public bool canGlide;
    public bool isGlide;
    [Tooltip("Gliding if Falling, Falling if anything else")]
    public State nextState;

    private float initialHeight;

    public override void Awake_S() => M.TryGetGlobalBehavior(out movement);

    public override void FixedUpdate_S()
    {
        if (isJump && transform.position.y < initialHeight + jumpHeight)
        {
            movement.velocity.y = jumpPower;
        }
        if (movement.velocity.y < jumpPower) TransitionTo(nextState);

        if (!Input.Jump.IsPressed() && transform.position.y > initialHeight + jumpMinHeight)
        {
            if (movement.velocity.y > 0) movement.velocity.y = 0;
            TransitionTo(nextState);
        }

        if ((canGlide && Input.Jump.IsPressed())
            || (isGlide && !Input.Jump.IsPressed()))
            TransitionTo(nextState);
    }

    public override void OnEnter()
    {
        movement.currentGravity = gravity;
        initialHeight = transform.position.y;
        movement.velocity.y = jumpPower;
    }
    public override void OnExit() => movement.currentGravity = movement.defaultGravity;

}
