using SLS.StateMachineV2;
using UnityEngine;
using System.Linq;

public class PlayerMovementBody : PlayerStateBehavior
{
    #region Config
    public float acceleration;
    public float maxSpeed;
    public float decceleration;
    //public float defaultGravity = 0;
    public float maxDownwardVelocity = 100f;
    public float skinDistance = 0.05f;
    public float coyoteTime = 0.5f;
    public float jumpBuffer = 0.3f;
    public float tripleJumpTime = 0.3f;
    public State groundedState;
    public State walkFallState;
    public State fallState;
    public State glideState;
    public State jumpState1;
    public State jumpState2;
    #endregion

    #region Data
    //[HideInInspector] public float currentGravity = 0;
    [HideInInspector] public bool baseMovability = true;
    [HideInInspector] public bool canJump = true;
    public bool grounded = true;
    [HideInInspector] public bool secondJump;
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public Vector3 currentDirection;


    float jumpInput;
    float coyoteTimeLeft;
    float tripleJumpTimeLeft;
    
    [SerializeField] Vector3 D_velocity;
    #endregion

    #region GetSet
    public Vector3 velocity { get => M.rb.velocity; set => M.rb.velocity = value; }
    public Vector3 position { get => M.rb.position; set => M.rb.position = value; }
    public Quaternion rotationQ { get => M.rb.rotation; set => M.rb.rotation = value; }
    public Vector3 rotation { get => transform.eulerAngles; set => transform.eulerAngles = value; }

    public void VelocitySet(float? x = null, float? y = null, float? z = null)
    {
        velocity = new Vector3(
            x ?? velocity.x,
            y ?? velocity.y,
            z ?? velocity.z
            );
    }
    public void PositionSet(float? x = null, float? y = null, float? z = null)
    {
        position = new Vector3(
            x ?? position.x,
            y ?? position.y,
            z ?? position.z
            );
    }


    #endregion GetSet



    public override void OnAwake()
    {
        M.physicsCallbacks += OnCollisionEnter_C;
        M.collider.center = new Vector3(M.collider.center.x, (M.collider.height / 2) + skinDistance, M.collider.center.z);
    }

    public override void OnUpdate()
    {
        if (input.jump.WasPressedThisFrame()) jumpInput = jumpBuffer + Time.fixedDeltaTime;
        if (jumpInput > 0) jumpInput -= Time.deltaTime;
    }

    public override void OnFixedUpdate()
    {
        //if (baseMovability) BaseHorizontalMovement();

        if (canJump) JumpHandle();

        if (grounded)
        {
            VelocitySet(y: 0);
            GroundStateChange(GroundCheck());
        }

        D_velocity = velocity;
    }

    private void JumpHandle()
    {
        if (fallState.active && coyoteTimeLeft > 0) coyoteTimeLeft -= Time.deltaTime;

        if (groundedState.active && tripleJumpTimeLeft > 0)
        {
            tripleJumpTimeLeft -= Time.deltaTime;
            if (tripleJumpTimeLeft <= 0) secondJump = false;
        }

        if (jumpInput > 0 && (groundedState.active || (walkFallState.active && coyoteTimeLeft > 0)))
        {
            jumpInput = 0;
            TransitionTo(secondJump ? jumpState2 : jumpState1);
            secondJump.Toggle();
            grounded = false;
        }

        if (fallState.active && input.jump.IsPressed()) TransitionTo(glideState);
        if (glideState.active && !input.jump.IsPressed()) TransitionTo(fallState);

    }

    public bool DirectionCast(Vector3 direction)
    {
        castResults = M.rb.SweepTestAll(direction);
        return castResults.Length > 0;
    }
    
    //RE ADD SKIN DISTANCE TO FIX AAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAAA
    public bool GroundCheck()
    {
        PositionSet(y: position.y + skinDistance);
        castResults = M.rb.SweepTestAll(Vector3.down, skinDistance*2f);
        PositionSet(y: position.y - skinDistance);
        return castResults.Length > 0;
    }
    public RaycastHit[] castResults;
    public bool GroundStateChange(bool input)
    {
        if(input == grounded) return false;
        grounded = input;

        if (!grounded) coyoteTimeLeft = coyoteTime;
        else tripleJumpTimeLeft = tripleJumpTime;
        if ((grounded && !groundedState.active) || (!grounded && !fallState.active))
            TransitionTo(grounded ? groundedState : walkFallState);

        return true;
    }

    private void OnCollisionEnter_C(PhysicsCallback type, Collision collision, Collider _)
    {
        if (type != PhysicsCallback.OnCollisionEnter || !state.active) return;

        if (GroundCheck())
        {
            GroundStateChange(true);
            VelocitySet(y: 0);
            PositionSet(y: position.y - castResults[0].distance + skinDistance * 1.999f);
        }
    }

    public State FallOrGlide() => input.jump.IsPressed() ? glideState : fallState;

}
