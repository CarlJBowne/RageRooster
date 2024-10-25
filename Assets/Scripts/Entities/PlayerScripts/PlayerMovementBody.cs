using SLS.StateMachineV2;
using UnityEngine;
using System.Linq;
using EditorAttributes;

public class PlayerMovementBody : PlayerStateBehavior
{
    #region Config
    public float skinDistance = 0.05f;
    public float coyoteTime = 0.5f;
    public float tripleJumpTime = 0.3f;
    public State groundedState;
    public State airborneState;
    public State fallState;
    public State jumpState1;
    public State jumpState2;
    #endregion

    #region Data

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public new CapsuleCollider collider;

    [HideInInspector] public bool baseMovability = true;
    [HideInInspector] public bool canJump = true;
    public bool grounded = true;
    [HideInInspector] public bool secondJump;
    [HideInInspector] public float currentSpeed;
    [HideInInspector] public Vector3 currentDirection;

    [HideInInspector] public float coyoteTimeLeft;
    float tripleJumpTimeLeft;
    
    [SerializeField, ReadOnly, Rename("Position")] Vector3 D_position;
    [SerializeField, ReadOnly, Rename("Velocity")] Vector3 D_velocity;
    #endregion

    #region GetSet
    public Vector3 velocity { get => rb.velocity; set => rb.velocity = value; }
    public Vector3 position { get => rb.position; set => rb.position = value; }
    public Quaternion rotationQ { get => rb.rotation; set => rb.rotation = value; }
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
        rb = GetComponentFromMachine<Rigidbody>();
        collider = GetComponentFromMachine<CapsuleCollider>();


        M.physicsCallbacks += OnCollisionEnter_C;
        collider.center = new Vector3(collider.center.x, (collider.height / 2) + skinDistance, collider.center.z);
    }
    public override void OnFixedUpdate()
    {
        if (canJump) JumpHandle();

        if (grounded)
        {
            VelocitySet(y: 0);
            GroundStateChange(GroundCheck());
        }

        D_position = position;
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
    }

    public bool DirectionCast(Vector3 direction)
    {
        castResults = rb.SweepTestAll(direction);
        return castResults.Length > 0;
    }
    
    public bool GroundCheck()
    {
        PositionSet(y: position.y + skinDistance);
        castResults = rb.SweepTestAll(Vector3.down, skinDistance*2f);
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
        if ((grounded && !groundedState.active) || (!grounded && !airborneState.active))
            TransitionTo(grounded ? groundedState : fallState);
        if (grounded && controller.CheckJumpBuffer()) BeginJump();

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

    public void BeginJump()
    {
        TransitionTo(secondJump ? jumpState2 : jumpState1);
        secondJump.Toggle();
        grounded = false;
    }

}
