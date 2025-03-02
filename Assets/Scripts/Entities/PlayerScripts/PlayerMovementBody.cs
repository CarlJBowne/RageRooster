using EditorAttributes;
using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

public class PlayerMovementBody : PlayerStateBehavior
{
    #region Config
    public int movementProjectionSteps;
    public float checkBuffer = 0.005f;
    public float maxSlopeNormalAngle = 20f;
    public PlayerAirborneMovement jumpState1;
    public PlayerWallJump wallJumpState;
    public PlayerAirborneMovement airChargeState;

    #endregion

    #region Data

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public new CapsuleCollider collider;

    [HideInInspector] public bool baseMovability = true;
    [HideInInspector] public bool canJump = true;
    public bool grounded = true;
    public float movementModifier = 1;
    [DisableInPlayMode, DisableInEditMode] public float currentSpeed;
    [DisableInPlayMode, DisableInEditMode] public int jumpPhase;
    //-1 = Inactive
    //0 = PreMinHeight
    //1 = PreMaxHeight
    //2 = SlowingDown
    //3 = Falling
    [HideInInspector] public Vector3 _currentDirection = Vector3.forward; 

    Transform anchorTransform;
    Vector3 prevAnchorPosition;

    [DisableInPlayMode, DisableInEditMode] public Vector3 velocity;
    #endregion

    #region GetSet
    //public Vector3 velocity { get => rb.velocity; set => rb.velocity = value; }
    public Vector3 position { 
        get => rb.position;
        set => rb.MovePosition(value); }
    public Quaternion rotationQ 
    { get => rb.rotation; set => rb.rotation = value; }
    public Vector3 rotation 
    { get => transform.eulerAngles; set => transform.eulerAngles = value; }

    public Vector3 center => position + collider.center;

    [HideInInspector] public Vector3 currentDirection
    {
        get => _currentDirection;
        set
        {
            if (_currentDirection == value) return;
            _currentDirection = value;
            if(body) body.rotation = _currentDirection.DirToRot();
        }
    }

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
        currentDirection = Vector3.forward;
        //M.physicsCallbacks += Collision;
    }

    public override void OnFixedUpdate()
    {
        Vector3 prevPosition = rb.position;
        {
            M.animator.SetFloat("CurrentSpeed", currentSpeed);
            rb.velocity = Vector3.zero;

            //Anchor System is busted. Fix later.
            //if (anchorTransform)
            //{
            //    Vector3 anchorOffset = prevAnchorPosition - anchorTransform.localPosition;
            //    prevAnchorPosition = anchorTransform.localPosition;
            //    rb.MovePosition(rb.position - anchorOffset);
            //}


        } // NonMovement

        initVelocity = new Vector3(velocity.x * movementModifier, velocity.y, velocity.z * movementModifier);
        initNormal = Vector3.up;

        if (PlayerStateMachine.DEBUG_MODE_ACTIVE && Input.Jump.IsPressed()) VelocitySet(y: 10f);

        if (velocity.y < 0.01f ||(grounded && velocity.y >= 0.1f)) 
        {
            if(rb.DirectionCast(Vector3.down, checkBuffer, checkBuffer, out groundHit))
            {
#if UNITY_EDITOR
                AddToQueuedHits(new(groundHit));
#endif
                initNormal = groundHit.normal;
                if (WithinSlopeAngle(groundHit.normal))
                {
                    GroundStateChange(true, groundHit.transform);
                    velocity.y = 0;
                    initVelocity.y = 0;
                    initVelocity = initVelocity.ProjectAndScale(groundHit.normal);
                }
            }
            else GroundStateChange(false);
        }

        Move(initVelocity * Time.fixedDeltaTime, initNormal);

        M.freeLookCamera.transform.position += transform.position - prevPosition;
    }

    Vector3 initVelocity;
    Vector3 initNormal;
    RaycastHit groundHit;

    /// <summary>
    /// The Collide and Slide Algorithm.
    /// </summary>
    /// <param name="vel">Input Velocity.</param>
    /// <param name="prevNormal">The Normal of the previous Step.</param>
    /// <param name="step">The current step. Starts at 0.</param>
    private void Move(Vector3 vel, Vector3 prevNormal, int step = 0)
    {
        if (rb.DirectionCast(vel.normalized, vel.magnitude, checkBuffer, out RaycastHit hit))
        {
#if UNITY_EDITOR
            AddToQueuedHits(new(hit));
#endif
            Vector3 snapToSurface = vel.normalized * hit.distance;
            Vector3 leftover = vel - snapToSurface;
            Vector3 nextNormal = hit.normal;
            bool stopped = false;
            rb.MovePosition(position + snapToSurface);

            if (step == movementProjectionSteps) return;

            if (grounded)
            {
                //Runs into wall/to high incline.
                if (Mathf.Approximately(hit.normal.y, 0) || (hit.normal.y > 0 && !WithinSlopeAngle(hit.normal))) 
                    Stop(prevNormal);

                if (grounded && prevNormal.y > 0 && hit.normal.y < 0) //Floor to Cieling
                    FloorCeilingLock(prevNormal, hit.normal);
                else if (grounded && prevNormal.y < 0 && hit.normal.y > 0) //Ceiling to Floor
                    FloorCeilingLock(hit.normal, prevNormal);
            }
            else
            {
                if(vel.y < .1f && WithinSlopeAngle(hit.normal))
                {
                    GroundStateChange(true, hit.transform);
                    leftover.y = 0;
                }
                else
                {
                    leftover = leftover.ProjectAndScale(hit.normal);
                    stopped = true;
                }
            }

                void FloorCeilingLock(Vector3 floorNormal, Vector3 ceilingNormal) => 
                    Stop(floorNormal.y != floorNormal.magnitude ? floorNormal : ceilingNormal);

                void Stop(Vector3 newNormal)
                {
                    nextNormal = newNormal.XZ().normalized;
                    //if (Vector3.Dot(newNormal, vel.XZ()) <= -.75f)
                        stopped = true;
                }

            if (stopped && M.SendSignal("Bonk", overrideReady: true, addToQueue: false)) return;

            Vector3 newDir = leftover.ProjectAndScale(nextNormal) * (Vector3.Dot(leftover.normalized, nextNormal) + 1); 
            Move(newDir, nextNormal, step + 1);
        }
        else
        {
            rb.MovePosition(position + vel);
            //Snap to ground when walking on a downward slope.
            if (grounded && initVelocity.y <= 0)
            {
                if (rb.DirectionCast(Vector3.down, 0.5f, checkBuffer, out RaycastHit groundHit))
                    rb.MovePosition(position + Vector3.down * groundHit.distance);
                else
                {
                    GroundStateChange(false);
                    M.SendSignal("WalkOff", overrideReady: true);
                }
            }
        }
    }

    /// <summary>
    /// Call to Change the Ground State and do all the logic related to that.
    /// </summary>
    /// <param name="input">New Grounded Value (Will early return if the same as the current value.)</param>
    /// <returns>Whether the Change was Successful.</returns>
    public bool GroundStateChange(bool input, Transform anchor)
    {
        if (input == grounded || rb.velocity.y > 0.01f) return false;
        grounded = input;

        //Anchor System is busted. Fix later.
        //anchorTransform = anchor && !anchor.gameObject.isStatic ? anchor : null;
        if (grounded)
        {
            jumpPhase = -1;

            if(anchor) prevAnchorPosition = anchor.position;
            M.SendSignal("Land", overrideReady: true);

            if (controller.CheckJumpBuffer()) M.SendSignal("Jump");
        }

        return true;
    }
    public bool GroundStateChange(bool input) => GroundStateChange(input, null);

    private bool WithinSlopeAngle(Vector3 inNormal) => Vector3.Angle(Vector3.up, inNormal) < maxSlopeNormalAngle;

    private void OnCollisionEnter(Collision collision)
    {
        Vector3 contactPoint = collision.GetContact(0).normal;
        if (!grounded && velocity.y > .1f && Vector3.Dot(contactPoint, Vector3.up) < -0.75f)
        {
            sFall.TransitionTo();
            VelocitySet(y: 0);
        }
        else if (!grounded && WithinSlopeAngle(contactPoint))
            GroundStateChange(true, collision.transform);
    }

    public void InstantSnapToFloor()
    {
        rb.DirectionCast(Vector3.down, 1000, .5f, out RaycastHit hit);
        rb.MovePosition(position + Vector3.down * hit.distance);
    }
    
    
    //[System.Obsolete]
    //public void TryBeginJump(PlayerAirborneMovement target)
    //{
    //    if ((grounded && sGrounded) || (sAirborne && body.coyoteTimeLeft > 0))
    //    {
    //        target.state.TransitionTo();
    //        grounded = false;
    //        anchorTransform = null;
    //    }
    //
    //    else controller.CheckJumpBuffer();
    //}

    //[System.Obsolete]
    //public void LatchAnchor(Transform newAnchor)
    //{
    //    if(newAnchor == null)
    //    {
    //        anchorTransform = null;
    //        return;
    //    }
    //    if (anchorTransform == newAnchor || newAnchor.gameObject.isStatic) return;
    //    anchorTransform = newAnchor;
    //    prevAnchorPosition = newAnchor.localPosition;
    //}

#if UNITY_EDITOR

    private List<HitNormalDisplay> queuedHits = new();
    private void AddToQueuedHits(HitNormalDisplay hit)
    {
        queuedHits.Add(hit);
        if(queuedHits.Count > 100) queuedHits.RemoveAt(0);
    }
    private void OnDrawGizmos()
    {
        foreach (HitNormalDisplay item in queuedHits) Debug.DrawRay(item.position, item.normal / 10);
        foreach (Vector3 item in jumpMarkers) Handles.DrawWireDisc(item, Vector3.up, 0.5f);
    }

    public List<Vector3> jumpMarkers = new List<Vector3>();

#endif

}

public struct HitNormalDisplay
{
    public Vector3 position;
    public Vector3 normal;
    public HitNormalDisplay(Vector3 position, Vector3 normal)
    {
        this.position = position;
        this.normal = normal;
    }
    public HitNormalDisplay(RaycastHit fromHit)
    {
        this.position = fromHit.point;
        this.normal = fromHit.normal;
    }
}