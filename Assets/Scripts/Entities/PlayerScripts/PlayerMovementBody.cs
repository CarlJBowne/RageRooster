using EditorAttributes;
using SLS.StateMachineV2;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerMovementBody : PlayerStateBehavior
{
    #region Config
    public int CaSMaxBounces;
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

    //[DisableInPlayMode] public Vector3 velocity;
    [Rename("Velocity")] public Vector3 D_velocity;
    #endregion

    #region GetSet
    public Vector3 velocity { get => rb.velocity; set => rb.velocity = value; }
    public Vector3 position { get => rb.position; set => rb.position = value; }
    public Quaternion rotationQ { get => rb.rotation; set => rb.rotation = value; }
    public Vector3 rotation { get => transform.eulerAngles; set => transform.eulerAngles = value; }

    
    public void VelocitySet(float? x = null, float? y = null, float? z = null)
    {
        rb.velocity = new Vector3(
            x ?? velocity.x,
            y ?? velocity.y,
            z ?? velocity.z
            );
    }
    public void PositionSet(float? x = null, float? y = null, float? z = null)
    {
        rb.position = new Vector3(
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

        GroundStateChange(rb.DirectionCast(Vector3.down));
        if (grounded) VelocitySet(y: 0);

        if (canJump) JumpHandle();

        Vector3 horizontal = rb.velocity.XZ() * Time.fixedDeltaTime / 2;
        Vector3 vertical = rb.velocity.y * Vector3.up * Time.fixedDeltaTime / 2;
        horizontal = CollideAndSlide(horizontal, rb.position, 0, false, horizontal);
        vertical = CollideAndSlide(vertical, rb.position, 0, true, vertical);

        //queuedPos = rb.position + horizontal + vertical;
        rb.MovePosition(rb.position + horizontal + vertical);

        D_velocity = velocity;
        M.animator.SetFloat("CurrentSpeed", currentSpeed);
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



    public Vector3 CollideAndSlide(Vector3 vel, Vector3 pos, int depth, bool gravityPass, Vector3 velInit)
    {
        if (depth >= CaSMaxBounces) return Vector3.zero;

        if (DirectionCast(vel.normalized, vel.magnitude, out RaycastHit hit))
        {
            Vector3 snapToSurface = vel.normalized * (hit.distance - checkBuffer);
            Vector3 leftover = vel - snapToSurface;
            float angle = Vector3.Angle(Vector3.up, hit.normal);

            if (snapToSurface.magnitude <= checkBuffer) snapToSurface = Vector3.zero;

            // normal ground / slope
            if (angle <= maxSlopeAngle)
            {
                if (gravityPass) return snapToSurface;
                leftover = ProjectAndScale(leftover, hit.normal);
            }
            else // wall or steep slope
            {
                float scale = 1 - Vector3.Dot(
                    new Vector3(hit.normal.x, 0, hit.normal.z).normalized,
                    -new Vector3(velInit.x, 0, velInit.z).normalized
                    );

                leftover = grounded && !gravityPass
                    ? ProjectAndScale(
                        new Vector3(velInit.x, 0, velInit.z),
                        new Vector3(hit.normal.x, 0, hit.normal.z).normalized
                        ).normalized * scale
                    : ProjectAndScale(leftover, hit.normal) * scale;
            }
            return snapToSurface + CollideAndSlide(leftover, pos + snapToSurface, depth + 1, gravityPass, velInit);
        }

        return vel;
    }

    public Vector3 ProjectAndScale(Vector3 vec, Vector3 normal)
    {
        float mag = vec.magnitude;
        vec = Vector3.ProjectOnPlane(vec, normal).normalized;
        vec *= mag;
        return vec;
    }

    public bool DirectionCast(Vector3 direction, float distance = 0f)
    {
        rb.Move(rb.position - (checkBuffer * direction.normalized), rb.rotation);
        bool result = rb.SweepTest(direction.normalized, out _, Mathf.Max(2 * checkBuffer, distance));
        rb.Move(rb.position + (checkBuffer * direction.normalized), rb.rotation);
        return result;
    }
    public bool DirectionCast(Vector3 direction, out RaycastHit hit, float distance = 0f)
    {
        rb.Move(rb.position - (checkBuffer * direction.normalized), rb.rotation);
        bool result = rb.SweepTest(direction.normalized, out hit, Mathf.Max(2 * checkBuffer, distance));
        rb.Move(rb.position + (checkBuffer * direction.normalized), rb.rotation);
        return result;
    }
    public bool DirectionCast(Vector3 direction, float distance, out RaycastHit hit)
    {
        rb.Move(rb.position - (checkBuffer * direction.normalized), rb.rotation);
        bool result = rb.SweepTest(direction.normalized, out hit, Mathf.Max(2 * checkBuffer, distance));
        rb.Move(rb.position + (checkBuffer * direction.normalized), rb.rotation);
        return result;
    }
    public bool DirectionCast(Vector3 direction, out RaycastHit[] hit, float distance = 0f)
    {
        rb.Move(rb.position - (checkBuffer * direction.normalized), rb.rotation);
        hit = rb.SweepTestAll(direction.normalized, Mathf.Max(2 * checkBuffer, distance));
        rb.Move(rb.position + (checkBuffer * direction.normalized), rb.rotation);
        return hit.Length > 0;
    }
    public bool DirectionCast(Vector3 direction, float distance, out RaycastHit[] hit)
    {
        rb.Move(rb.position - (checkBuffer * direction.normalized), rb.rotation);
        hit = rb.SweepTestAll(direction.normalized, Mathf.Max(2 * checkBuffer, distance));
        rb.Move(rb.position + (checkBuffer * direction.normalized), rb.rotation);
        return hit.Length > 0;
    }


}
