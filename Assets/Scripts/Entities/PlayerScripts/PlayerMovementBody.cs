using SLS.StateMachineV2;
using UnityEngine;

public class PlayerMovementBody : StateBehavior
{
    new PlayerStateMachine M => base.M as PlayerStateMachine;

    #region Config
    public float acceleration;
    public float maxSpeed;
    public float decceleration;
    public float defaultGravity = 0;
    public float maxDownwardVelocity = 100f;
    public float skinDistance = 0.05f;
    public float coyoteTime = 0.5f;
    public float jumpBuffer = 0.3f;
    public float tripleJumpTime = 0.3f;
    public State groundedState;
    public State fallState;
    public State glideState;
    public State[] jumpingStates;
    #endregion

    #region Data
    [HideInInspector] public float currentGravity = 0;
    [HideInInspector] public bool baseMovability = true;
    [HideInInspector] public bool canJump = true;
    [HideInInspector] public bool grounded = true;
    [HideInInspector] public int currentJump = 1;

    float jumpInput;
    float coyoteTimeLeft;
    float tripleJumpTimeLeft;
    
    public Vector3 velocity { get => M.rb.velocity; set => M.rb.velocity = value; }
    public Vector3 position { get => M.rb.position; set => M.rb.position = value; }
    #endregion

    public override void Awake_S()
    {
        currentJump = 1;
        currentGravity = defaultGravity;
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

        velocity -= Vector3.up * currentGravity * Time.deltaTime;

        if (canJump) JumpHandle();

        if (velocity.y < -maxDownwardVelocity) SetVelocity(y: -maxDownwardVelocity);

        GroundCheck(velocity.y * -Time.deltaTime);

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
            if (tripleJumpTimeLeft <= 0) currentJump = 1;
        }

        if (jumpInput > 0 && (groundedState.active || (fallState.active && coyoteTimeLeft > 0)))
        {
            jumpInput = 0;
            TransitionTo(jumpingStates[currentJump-1]);
            currentJump = currentJump == 3 ? 1 : currentJump + 1;
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
            TransitionTo(result ? groundedState : FallOrGlide());

        if (position.y <= -0.1944985f)
        {
            Debug.LogError("");
        }
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
