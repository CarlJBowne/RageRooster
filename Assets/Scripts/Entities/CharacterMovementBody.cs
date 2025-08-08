using SLS.StateMachineH;
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(StateMachine))]
public class CharacterMovementBody : MonoBehaviour
{

    #region Config

    [SerializeField] protected Vector3 defaultGravity = new(0, 1, 0);
    [SerializeField] protected float maxSlopeNormalAngle = 45f;
    /// <summary>
    /// Whether this body should automatically check the grounded status before movement.
    /// </summary>
    public bool checkGround = true;
    /// <summary>
    /// The buffer used to check for ground.
    /// </summary>
    public float movementCheckBuffer = 0.1f;
    /// <summary>
    /// The buffer used to check for ground.
    /// </summary>
    public float groundCheckBuffer = 0.1f;
    /// <summary>
    /// The number of steps used in the Collide & Slide Algorithm.
    /// </summary>
    public int movementProjectionSteps = 5;

    #endregion Config
    #region Components

    public Rigidbody RB { get => _rb; private set => _rb = value; }
    [SerializeField] private Rigidbody _rb;
    [field: SerializeField, HideInInspector] public CapsuleCollider Collider { get; private set; }

    #endregion Components
    #region Data

    /// <summary>
    /// Custom velocity value.
    /// </summary>
    public Vector3 velocity = new(0, 0, 0);
    /// <summary>
    /// Custom angular velocity value.
    /// </summary>
    [NonSerialized] public Vector3 angularVelocity = new(0, 0, 0);

    /// <summary>
    /// The active direction of the character. Simpler controllers can probably avoid using this.
    /// </summary>
    public Vector3 direction = new(0, 0, 1);
    /// <summary>
    /// The active gravity value. (Inverted. y=1 is down.)
    /// </summary>
    [NonSerialized] private Vector3 gravity = new(0, 9.8f, 0);

    public enum CharacterRigidBodyState
    {
        Enabled,
        Kinematic,
        Ragdoll,
        OFF
    }
    public CharacterRigidBodyState RBState
    {
        get => _rbState;
        set
        {
            _rbState = value;
            switch (value)
            {
                case CharacterRigidBodyState.Enabled:
                    RB.isKinematic = false;
                    RB.detectCollisions = true;
                    RB.useGravity = false;
                    break;
                case CharacterRigidBodyState.Kinematic:
                    RB.isKinematic = true;
                    RB.detectCollisions = true;
                    RB.useGravity = false;
                    break;
                case CharacterRigidBodyState.Ragdoll:
                    RB.isKinematic = false;
                    RB.detectCollisions = true;
                    RB.useGravity = true;
                    break;
                case CharacterRigidBodyState.OFF:
                    RB.isKinematic = true;
                    RB.detectCollisions = false;
                    RB.useGravity = false;
                    break;
            }
        }
    }
    private CharacterRigidBodyState _rbState = CharacterRigidBodyState.Enabled;

    protected JumpState jumpState = JumpState.Grounded;

    protected AnchorPoint anchorPoint = AnchorPoint.Null;
    protected IMovablePlatform movingAnchor;

    #endregion Data

    #region GetSets

    public Vector3 Position
    {
        get => RB.isKinematic ? transform.position : RB.position;
        set
        {
            if (RB.isKinematic)
                return;
            transform.position = value;
            RB.position = value;
            RB.MovePosition(value);
        }
    }
    public Quaternion RotationQ
    { get => RB.rotation; set => RB.rotation = value; }
    public Vector3 Rotation
    {
        get => transform.eulerAngles;
        set => transform.eulerAngles = value;
    }

    #endregion GetSets
    #region Gets

    /// <summary>
    /// Returns the current velocity of the Rigidbody. (Inverted. y=1 is downwards, y=-1 is upwards.)
    /// </summary>
    public Vector3 Get3DGravity() => gravity;
    /// <summary>
    /// Returns the current velocity of the Rigidbody. (Y only.) (Inverted. 1 is downwards, -1 is upwards.)
    /// </summary>
    public float GetGravity() => gravity.y;

    public bool Grounded => jumpState == JumpState.Grounded;
    public JumpState JumpState => jumpState;

    public Vector3 center => Position + Collider.center;

    #endregion Gets
    #region Sets

    /// <summary>
    /// Sets the current velocity of the Rigidbody. (Inverted. y=1 is downwards, y=-1 is upwards.)
    /// </summary>
    /// <param name="newGravity">The new gravity value.</param>
    public void SetGravity(Vector3 newGravity) => gravity = newGravity;
    /// <summary>
    /// Sets the current velocity of the Rigidbody. (Y only.) (Inverted. 1 is downwards, -1 is upwards.)
    /// </summary>
    /// <param name="newGravity">The new gravity value.</param>
    public void SetGravity(float newGravity) => gravity = new(0, newGravity, 0);
    /// <summary>
    /// Sets the current velocity of the Rigidbody. (Inverted. y=1 is downwards, y=-1 is upwards.)
    /// </summary>
    /// <param name="newX"> The new gravity value on the x axis. (1 = left.) </param>
    /// <param name="newY"> The new gravity value on the y axis. (1 = down.) </param>
    /// <param name="newZ"> The new gravity value on the z axis. (1 = back.) </param>
    public void SetGravity(float newX, float newY, float newZ) => gravity = new(newX, newY, newZ);



    #endregion Sets



    [ContextMenu("GetComponents")]
    protected virtual void Awake()
    {
        if (RB == null) RB = GetComponent<Rigidbody>();
        if (Collider == null) Collider = GetComponent<CapsuleCollider>();
        
        if(InstantSnapToFloor(out RaycastHit hit)) Land(hit);
    }

    private void OnEnable()
    {
        if(_rbState == CharacterRigidBodyState.OFF) 
            RBState = CharacterRigidBodyState.Enabled;
    }
    private void OnDisable()
    {
        RBState = CharacterRigidBodyState.OFF;
    }

    protected virtual void FixedUpdate()
    {
        if(RBState != CharacterRigidBodyState.Enabled) return;
        RB.velocity = Vector3.zero;
        RB.angularVelocity = Vector3.zero;

        initVelocity = velocity * Time.fixedDeltaTime;
        initNormal = anchorPoint.normal;

        Move(initVelocity, initNormal);

        if (checkGround && velocity.y <= 0)
        {
            if (GroundCheck())
            {
                initNormal = anchorPoint.normal;
                if (WithinSlopeAngle(anchorPoint.normal))
                {
                    Land(anchorPoint);
                    velocity.y = 0;
                    initVelocity.y = 0;
                    initVelocity = initVelocity.ProjectAndScale(anchorPoint.normal);
                }
            }
            else if (jumpState == JumpState.Grounded) UnLand();
        }

        if (!Grounded) ApplyGravity();
    }

    Vector3 initVelocity;
    Vector3 initNormal;

    /// <summary>
    /// The Collide and Slide Algorithm.
    /// </summary>
    /// <param name="vel">Input Velocity.</param>
    /// <param name="prevNormal">The Normal of the previous Step.</param>
    /// <param name="step">The current step. Starts at 0.</param>
    protected virtual void Move(Vector3 vel, Vector3 prevNormal, int step = 0)
    {
        if (DirectionCast(vel.normalized, vel.magnitude, groundCheckBuffer, out RaycastHit hit))
        {
            Vector3 snapToSurface = vel.normalized * hit.distance;
            Vector3 leftover = vel - snapToSurface;
            Vector3 nextNormal = hit.normal;

            if (step == movementProjectionSteps) return;

            if (!MoveForward(snapToSurface)) return;

            if (Grounded)
            {
                //Runs into wall/to high incline.
                if (Mathf.Approximately(hit.normal.y, 0) || (hit.normal.y > 0 && !WithinSlopeAngle(hit.normal)))
                    if (StopForward(ref nextNormal, hit.normal)) return;

                if (Grounded && prevNormal.y > 0 && hit.normal.y < 0) //Floor to Cieling
                    if(FloorCeilingLock(prevNormal, hit.normal)) return;
                else if (Grounded && prevNormal.y < 0 && hit.normal.y > 0) //Ceiling to Floor
                    if(FloorCeilingLock(hit.normal, prevNormal)) return;
            }
            else
            {
                if (vel.y < .1f && WithinSlopeAngle(hit.normal))
                {
                    Land(hit);
                    leftover.y = 0;
                }
                else if (vel.y < -1f && DirectionCastAll(vel, vel.y.Abs(), groundCheckBuffer, out RaycastHit[] downHits) && downHits.Length > 1)
                {
                    UnLand();
                    leftover.y = 0;
                }
                else leftover = leftover.ProjectAndScale(hit.normal);
            }

            bool FloorCeilingLock(Vector3 floorNormal, Vector3 ceilingNormal) =>
                StopForward(ref nextNormal, floorNormal.y != floorNormal.magnitude ? floorNormal : ceilingNormal);

            Vector3 newDir = leftover.ProjectAndScale(nextNormal) * (Vector3.Dot(leftover.normalized, nextNormal) + 1);
            Move(newDir, nextNormal, step + 1);
        }
        else
        {

            if (step == movementProjectionSteps) return;
            if (!MoveForward(vel)) return;

            //Snap to ground when walking on a downward slope.
            if (Grounded && initVelocity.y <= 0)
            {
                if (DirectionCast(Vector3.down, 0.5f, groundCheckBuffer, out RaycastHit groundHit))
                    Position += Vector3.down * groundHit.distance;
                else WalkOff();
            }
        }
    }

    /// <summary>
    /// Stops the forward movement and updates the next normal vector.
    /// </summary>
    /// <param name="nextNormal">A reference to the vector that will be updated to the normalized XZ components of <paramref name="newNormal"/>.</param>
    /// <param name="newNormal">The vector whose XZ components are used to calculate the updated normal.</param>
    /// <returns>Whether the Collide and Slide Algorithm should truly stop..</returns>
    protected virtual bool StopForward(ref Vector3 nextNormal, Vector3 newNormal)
    {
        nextNormal = newNormal.XZ().normalized;
        return false;
    }

    protected virtual bool MoveForward(Vector3 offset)
    {
        Position += offset;
        return true;
    }

    protected virtual void WalkOff()
    {
        UnLand();
    }


    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest.
    /// </summary>
    /// <param name="rb">The Rigidbody in question.</param>
    /// <param name="direction">The direction the Rigidbody is going.</param>
    /// <param name="distance">The distance the Rigidbody is set to travel.</param>
    /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
    /// <param name="hit">The resulting Hit.</param>
    /// <returns>Whether anything was Hit.</returns>
    public virtual bool DirectionCast(Vector3 direction, float distance, float buffer, out RaycastHit hit)
    {
        if(buffer > 0) _rb.MovePosition(_rb.position - direction * buffer);
        bool result = _rb.SweepTest(direction.normalized, out hit, distance + buffer, QueryTriggerInteraction.Ignore);
        if (buffer > 0) _rb.MovePosition(_rb.position + direction * buffer);
        hit.distance -= buffer;
        return result; 
    }
    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest. (Returns Multiple.)
    /// </summary>
    /// <param name="rb">The Rigidbody in question.</param>
    /// <param name="direction">The direction the Rigidbody is going.</param>
    /// <param name="distance">The distance the Rigidbody is set to travel.</param>
    /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
    /// <param name="hit">The resulting Hits.</param>
    /// <returns>Whether anything was Hit.</returns>
    public virtual bool DirectionCastAll(Vector3 direction, float distance, float buffer, out RaycastHit[] hit)
    {
        if (buffer > 0) RB.MovePosition(RB.position - direction * buffer);
        hit = RB.SweepTestAll(direction.normalized, distance + buffer, QueryTriggerInteraction.Ignore);
        if (buffer > 0) RB.MovePosition(RB.position + direction * buffer);
        hit[0].distance -= buffer;
        return hit.Length > 0;
    }

    public virtual bool GroundCheck(out AnchorPoint groundHit)
    {
        bool result = DirectionCast(Vector3.down, groundCheckBuffer, groundCheckBuffer, out RaycastHit raycast); 
        groundHit = raycast;
        return result;
    }
    public virtual bool GroundCheck()
    {
        if(DirectionCast(Vector3.down, groundCheckBuffer, groundCheckBuffer, out RaycastHit raycast))
        {
            Land(raycast);
            return true;
        }
        return false;
    }

    public virtual void Land(AnchorPoint groundHit)
    {
        bool wasntGrounded = jumpState != JumpState.Grounded;
        bool objectChange = anchorPoint.transform != groundHit.transform;

        if (wasntGrounded && objectChange) return;

        jumpState = JumpState.Grounded;
        anchorPoint = groundHit;

        if (objectChange)
        {
            movingAnchor?.RemoveBody(this);
            movingAnchor = anchorPoint.transform.GetComponent<IMovablePlatform>();
            movingAnchor?.AddBody(this);
        }

        if (wasntGrounded)
        {
            LandEvent?.Invoke();
        }
    }
    public Action LandEvent;
    public virtual void UnLand(JumpState newState = JumpState.Falling)
    {
        if (newState < JumpState.Jumping) return;
        jumpState = newState;
        anchorPoint = AnchorPoint.Null;
        if(movingAnchor != null)
        {
            movingAnchor.RemoveBody(this);
            movingAnchor = null;
        }
    }


    public bool InstantSnapToFloor()
    {
        if(DirectionCast(Vector3.down, 1000, .5f, out RaycastHit hit))
        {
            Position += Vector3.down * hit.distance;
            return true;
        }
        return false;
    }
    public bool InstantSnapToFloor(out RaycastHit hit)
    {
        if (DirectionCast(Vector3.down, 1000, .5f, out hit))
        {
            Position += Vector3.down * hit.distance;
            return true;
        }
        return false;
    }

    protected virtual void OnCollisionEnter(Collision collision)
    {
        Vector3 contactPoint = collision.GetContact(0).normal;
        if (!Grounded && velocity.y > .1f && Vector3.Dot(contactPoint, Vector3.up) < -0.75f) velocity.y = 0;
        else if (!Grounded && WithinSlopeAngle(contactPoint))
            Land(collision.GetContact(0));

    }

    private bool WithinSlopeAngle(Vector3 inNormal) => Vector3.Angle(Vector3.up, inNormal) < maxSlopeNormalAngle;

    public virtual void ApplyGravity() => velocity -= gravity * Time.fixedDeltaTime;

}

public enum JumpState
{
    Null = -1,
    Grounded = 0,
    Jumping = 1,
    Decelerating = 2,
    Hangtime = 3,
    Falling = 4,
    TerminalVelocity = 5
}
