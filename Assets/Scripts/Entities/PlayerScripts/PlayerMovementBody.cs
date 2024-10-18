using SLS.StateMachineV2;
using UnityEngine;

public class PlayerMovementBody : StateBehavior
{
    new PlayerStateMachine M => base.M as PlayerStateMachine;

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
    [HideInInspector] public bool grounded = true;
    [HideInInspector] public bool secondJump;

    float jumpInput;
    float coyoteTimeLeft;
    float tripleJumpTimeLeft;
    
    public Vector3 velocity { get => M.rb.velocity; set => M.rb.velocity = value; }
    public Vector3 position { get => M.rb.position; set => M.rb.position = value; }
    [SerializeField] Vector3 D_velocity;
    #endregion

    public override void Awake_S()
    {
        //currentGravity = defaultGravity;
        M.collider.center = new Vector3(M.collider.center.x, (M.collider.height / 2) + skinDistance, M.collider.center.z);
    }

    public override void Update_S()
    {
        if (Input.Jump.WasPressedThisFrame()) jumpInput = jumpBuffer + Time.fixedDeltaTime;
        if (jumpInput > 0) jumpInput -= Time.deltaTime;
    }

    public override void FixedUpdate_S()
    {
        if (baseMovability) BaseHorizontalMovement();

        if (canJump) JumpHandle();

        GroundCheck(velocity.y * -Time.deltaTime);

        D_velocity = velocity;
    }


    private void BaseHorizontalMovement()
    {
        Vector3 adjustedDirection = Input.Movement.ToXZ().Rotate(M.cameraTransform.eulerAngles.y, Vector3.up);
        Vector3 maxGoal = adjustedDirection * maxSpeed;

        Vector3 workVelocity = velocity.XZ();

        workVelocity = Vector3.MoveTowards(workVelocity, maxGoal, 
            adjustedDirection.magnitude > 0 
            ? (-Vector3.Dot(maxGoal, workVelocity) + 1).Min(1f) 
            : decceleration);

        SetVelocity(x: workVelocity.x, z: workVelocity.z);
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

        if (fallState.active && Input.Jump.IsPressed()) TransitionTo(glideState);
        if (glideState.active && !Input.Jump.IsPressed()) TransitionTo(fallState);

    }

    public void GroundCheck(float downwardMomentum)
    {
        if(downwardMomentum < 0)
        {
            if(grounded) grounded = false;
            if (groundedState.active) TransitionTo(FallOrGlide());
            return;
        }

        RaycastHit[] results = M.rb.SweepTestAll(Vector3.down, downwardMomentum + skinDistance);
        bool result = results.Length > 0;
        if (downwardMomentum < 0) result = false;

        if (grounded == result) return;
        grounded = result;

        if (result)
        { 
            SetVelocity(y: 0);
            SetPosition(y: position.y - results[0].distance + skinDistance);
        }

        if (!result) coyoteTimeLeft = coyoteTime;
        else tripleJumpTimeLeft = tripleJumpTime;
        if ((result && !groundedState.active) || (!result && !fallState.active))
            TransitionTo(result ? groundedState : walkFallState);
    }

    public State FallOrGlide() => Input.Jump.IsPressed() ? glideState : fallState;


    public void SetVelocity(float? x = null, float? y = null, float? z = null)
    {
        velocity = new Vector3(
            x ?? velocity.x,
            y ?? velocity.y,
            z ?? velocity.z
            );
    }
    public void SetPosition(float? x = null, float? y = null, float? z = null)
    {
        position = new Vector3(
            x ?? position.x,
            y ?? position.y,
            z ?? position.z
            );
    }









}
