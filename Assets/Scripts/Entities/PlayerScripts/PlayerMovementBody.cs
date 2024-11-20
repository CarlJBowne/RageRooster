using EditorAttributes;
using SLS.StateMachineV2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMovementBody : PlayerStateBehavior
{
    #region Config
    public int movementProjectionSteps;
    public float checkBuffer = 0.005f;
    public float maxSlopeAngle = 20f;
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

    [DisableInPlayMode] public Vector3 velocity;
    #endregion

    #region GetSet
    //public Vector3 velocity { get => rb.velocity; set => rb.velocity = value; }
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

        //collider.center = new Vector3(collider.center.x, (collider.height / 2) + skinDistance, collider.center.z);
    }

    public override void OnFixedUpdate()
    {
        M.animator.SetFloat("CurrentSpeed", currentSpeed);

        //if(rb.velocity.magnitude > 0) 
        //    rb.velocity = Vector3.MoveTowards(rb.velocity, Vector3.zero, rb.velocity.magnitude / 10f);

        rb.velocity = Vector3.zero;

        if (coyoteTimeLeft > 0) coyoteTimeLeft -= Time.deltaTime;

        if (groundedState.active && tripleJumpTimeLeft > 0)
        {
            tripleJumpTimeLeft -= Time.deltaTime;
            if (tripleJumpTimeLeft <= 0) secondJump = false;
        }

        initVelocity = velocity;


        if(velocity.y <= 0.01f)
        {
            GroundStateChange(rb.DirectionCast(Vector3.down, checkBuffer, checkBuffer, out RaycastHit groundHit));

            if (grounded)
            {
                velocity.y = 0;
                initVelocity.y = 0;

                if (Vector3.Angle(Vector3.up, groundHit.normal) < 90 - maxSlopeAngle)
                    velocity = velocity.ProjectAndScale(groundHit.normal);
            }
        }

        //initGroundNormal = groundHit.normal;
        //initPosition = position;
        Move(velocity * Time.fixedDeltaTime);

        velocity = initVelocity;

        /*
        transform.position += velocity.XZ();
        
        Vector3 horizontal = velocity.XZ() * Time.fixedDeltaTime / 2;
        Vector3 vertical = velocity.y * Time.fixedDeltaTime * Vector3.up / 2;
        horizontal = CollideAndSlide(horizontal, transform.position, 0, false, horizontal);
        vertical = CollideAndSlide(vertical, transform.position, 0, true, vertical);
        
        rb.MovePosition(rb.position + horizontal + vertical);
        */
    }

    Vector3 initVelocity;
    //Vector3 initGroundNormal;
    //Vector3 initPosition;

    private void Move(Vector3 vel, int step = 0)
    {
        float horizontalMag = initVelocity.XZ().magnitude;
        float verticalMag = initVelocity.y.Abs();

        if (rb.DirectionCast(vel.normalized, vel.magnitude, checkBuffer, out RaycastHit hit))
        {
            Vector3 snapToSurface = vel.normalized * hit.distance;
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);
            rb.MovePosition(position + snapToSurface);

            if(step == 0) GroundStateChange(rb.DirectionCast(Vector3.down, checkBuffer, checkBuffer, out RaycastHit groundHit));

            if(hit.collider.gameObject.layer == Layers.NonSolid && verticalMag < horizontalMag * 2)
            {
                rb.MovePosition(position + vel.normalized * 0.05f);
                return; 
            }
            if (verticalMag < horizontalMag * 2 && velocity.normalized.y < 0) GroundStateChange(true);

            if (step == movementProjectionSteps || Vector3.Dot(Vector3.down, hit.normal) > 0.45f) return;

            Vector3 newDir = leftover.ProjectAndScale(hit.normal) * (Vector3.Dot(leftover.normalized, hit.normal) + 1);

            Move(newDir, step + 1);
        }
        else
        {
            rb.MovePosition(position + vel);
            if (grounded && initVelocity.y <= 0 && rb.DirectionCast(Vector3.down, 0.5f, checkBuffer, out RaycastHit groundHit))
            {
                rb.MovePosition(position + Vector3.down * groundHit.distance);
            }
        }
    }

    public bool GroundStateChange(bool input)
    {
        if (input == grounded || rb.velocity.y > 0) return false;
        grounded = input;

        if (!grounded) coyoteTimeLeft = coyoteTime;
        else tripleJumpTimeLeft = tripleJumpTime;
        if ((grounded && !groundedState.active) || (!grounded && !airborneState.active))
            TransitionTo(grounded ? groundedState : fallState);
        if (grounded && controller.CheckJumpBuffer()) BeginJump();

        return true;
    }

    /*
    private void PhysicsCallbacks(PhysicsCallback type, Collision collision, Collider _)
    {
        
        if (type != PhysicsCallback.OnCollisionEnter || !state.active) return;

        if (GroundCheck())
        {
            GroundStateChange(true);
            VelocitySet(y: 0);
            PositionSet(y: position.y - castResults[0].distance + skinDistance * 1.999f);
        }
         
    }
    private List<Collision> collisions;
    */

    public void BeginJump()
    {
        TransitionTo(secondJump ? jumpState2 : jumpState1);
        secondJump.Toggle();
        grounded = false;
    }

    private void OnDrawGizmos()
    {
        Gizmos.DrawRay(rb.position + rb.centerOfMass, rb.velocity.XZ() * Time.fixedDeltaTime);
    }

}
