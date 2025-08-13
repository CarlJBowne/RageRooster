using SLS.StateMachineH;
using System;
using UnityEngine;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(StateMachine))]
public class CharacterMovementBody : MonoBehaviour
{

    #region Config

    /// <summary>
    /// The default gravity vector for this <see cref="CharacterMovementBody"/>.
    /// </summary>
    [SerializeField] protected Vector3 defaultGravity = new(0, 1, 0);
    /// <summary>
    /// Whether Gravity should be automaticall applied or applied by some behavior
    /// </summary>
    public bool autoApplyGravity = false;
    /// <summary>
    /// The maximum angle (in degrees) of a slope this <see cref="CharacterMovementBody"/> can stand on.
    /// </summary>
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

    /// <summary>
    /// The Rigidbody component attached to this <see cref="CharacterMovementBody"/>.
    /// </summary>
    public Rigidbody RB { get => _rb; private set => _rb = value; }
    [SerializeField] private Rigidbody _rb;
    /// <summary>
    /// The CapsuleCollider component attached to this <see cref="CharacterMovementBody"/>.
    /// </summary>
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
    /// The active direction of this <see cref="CharacterMovementBody"/>. Simpler controllers can probably avoid using this.
    /// </summary>
    public Vector3 direction = new(0, 0, 1);
    /// <summary>
    /// The active gravity value. (Inverted. y=1 is down.)
    /// </summary>
    [NonSerialized] private Vector3 gravity = new(0, 9.8f, 0);

    /// <summary>
    /// The possible states for a <see cref="CharacterMovementBody"/>.
    /// </summary>
    public enum CharacterMovementBodyState
    {
        Enabled,
        Kinematic,
        Ragdoll,
        OFF
    }
    /// <summary>
    /// The current state of this <see cref="CharacterMovementBody"/>.
    /// </summary>
    public CharacterMovementBodyState RBState
    {
        get => _rbState;
        set
        {
            _rbState = value;
            switch (value)
            {
                case CharacterMovementBodyState.Enabled:
                    RB.isKinematic = false;
                    RB.detectCollisions = true;
                    RB.useGravity = false;
                    break;
                case CharacterMovementBodyState.Kinematic:
                    RB.isKinematic = true;
                    RB.detectCollisions = true;
                    RB.useGravity = false;
                    break;
                case CharacterMovementBodyState.Ragdoll:
                    RB.isKinematic = false;
                    RB.detectCollisions = true;
                    RB.useGravity = true;
                    break;
                case CharacterMovementBodyState.OFF:
                    RB.isKinematic = true;
                    RB.detectCollisions = false;
                    RB.useGravity = false;
                    break;
            }
        }
    }
    private CharacterMovementBodyState _rbState = CharacterMovementBodyState.Enabled;

    /// <summary>
    /// The current jump state of this body.
    /// </summary>
    protected JumpState jumpState = JumpState.Grounded;

    /// <summary>
    /// The current anchor point this body is attached to.
    /// </summary>
    protected AnchorPoint anchorPoint = AnchorPoint.Null;
    /// <summary>
    /// The current moving platform this body is anchored to, if any.
    /// </summary>
    protected IMovablePlatform movingAnchor;

    #endregion Data

    #region GetSets

    /// <summary>
    /// Gets or sets the position of the character.
    /// </summary>
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
    /// <summary>
    /// Gets or sets the rotation of the Rigidbody as a Quaternion.
    /// </summary>
    public Quaternion RotationQ
    { get => RB.rotation; set => RB.rotation = value; }
    /// <summary>
    /// Gets or sets the rotation of the character in Euler angles.
    /// </summary>
    public Vector3 Rotation
    {
        get => transform.eulerAngles;
        set => transform.eulerAngles = value;
    }

    #endregion GetSets
    #region Gets

    /// <summary>
    /// Returns the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
    /// </summary>
    public Vector3 Get3DGravity() => gravity;
    /// <summary>
    /// Returns the current gravity value on the Y axis. (Inverted. 1 is downwards, -1 is upwards.)
    /// </summary>
    public float GetGravity() => gravity.y;

    /// <summary>
    /// Whether the character is currently grounded.
    /// </summary>
    public bool Grounded => jumpState == JumpState.Grounded;
    /// <summary>
    /// The current jump state of the character.
    /// </summary>
    public JumpState JumpState => jumpState;

    /// <summary>
    /// The center position of the character's collider.
    /// </summary>
    public Vector3 center => Position + Collider.center;

    #endregion Gets
    #region Sets

    /// <summary>
    /// Sets the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
    /// </summary>
    /// <param name="newGravity">The new gravity value.</param>
    public void SetGravity(Vector3 newGravity) => gravity = newGravity;
    /// <summary>
    /// Sets the current gravity value on the Y axis. (Inverted. 1 is downwards, -1 is upwards.)
    /// </summary>
    /// <param name="newGravity">The new gravity value.</param>
    public void SetGravity(float newGravity) => gravity = new(0, newGravity, 0);
    /// <summary>
    /// Sets the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
    /// </summary>
    /// <param name="newX"> The new gravity value on the x axis. (1 = left.) </param>
    /// <param name="newY"> The new gravity value on the y axis. (1 = down.) </param>
    /// <param name="newZ"> The new gravity value on the z axis. (1 = back.) </param>
    public void SetGravity(float newX, float newY, float newZ) => gravity = new(newX, newY, newZ);

    #endregion Sets

    /// <summary>
    /// Gets required components and optionally snaps the character to the floor on Awake.
    /// </summary>
    [ContextMenu("GetComponents")]
    protected virtual void Awake()
    {
        if (RB == null) RB = GetComponent<Rigidbody>();
        if (Collider == null) Collider = GetComponent<CapsuleCollider>();

        if (InstantSnapToFloor(out RaycastHit hit)) Land(hit);
    }

    /// <summary>
    /// Called when the component is enabled.
    /// </summary>
    private void OnEnable()
    {
        if (_rbState == CharacterMovementBodyState.OFF)
            RBState = CharacterMovementBodyState.Enabled;
    }
    /// <summary>
    /// Called when the component is disabled.
    /// </summary>
    private void OnDisable()
    {
        RBState = CharacterMovementBodyState.OFF;
    }

    /// <summary>
    /// Handles physics-based movement and state updates.
    /// </summary>
    protected virtual void FixedUpdate()
    {
        if (RBState != CharacterMovementBodyState.Enabled) return;
        RB.velocity = Vector3.zero;
        RB.angularVelocity = Vector3.zero;

        if (checkGround && velocity.y <= 0)
        {
            if (GroundCheck(out AnchorPoint groundHit)) 
            {
                Land(groundHit);
                velocity.y = 0;
                initVelocity.y = 0;
                initVelocity = initVelocity.ProjectAndScale(groundHit.normal);
            }
            else UnLand();
        }

        initVelocity = velocity * Time.fixedDeltaTime;
        initNormal = anchorPoint.normal;

        //moveTestString = "";
        Move(initVelocity, initNormal);

        if (autoApplyGravity && !Grounded) ApplyGravity();
    }

    /// <summary>
    /// The initial velocity used in the current physics step.
    /// </summary>
    Vector3 initVelocity;
    /// <summary>
    /// The initial normal used in the current physics step.
    /// </summary>
    Vector3 initNormal;

    /// <summary>
    /// The Collide and Slide Algorithm.
    /// </summary>
    /// <param name="vel">Input Velocity.</param>
    /// <param name="prevNormal">The Normal of the previous Step.</param>
    /// <param name="step">The current step. Starts at 0.</param>
    protected virtual void Move(Vector3 vel, Vector3 prevNormal, int step = 0) 
    {
        //moveTestString += $"Step {step}: {vel}\n";

        if (DirectionCast(vel.normalized, vel.magnitude, groundCheckBuffer, out RaycastHit hit))
        {
            //moveTestString += $"Hit: {hit.normal} at distance {hit.distance}\n";
            Vector3 snapToSurface = vel.normalized * hit.distance;
            Vector3 leftover = vel - snapToSurface;
            Vector3 nextNormal = hit.normal;
            bool scaleByDot = false;

            if (step == movementProjectionSteps) return;

            if (!MoveForward(snapToSurface)) return;

            else if (Grounded)
            {
                //moveTestString += "Is Grounded.\n";

                if (Mathf.Approximately(hit.normal.y, 0))
                {
                    //moveTestString += "Hit a wall.\n";
                    scaleByDot = true;
                    leftover.y = 0;
                    if (StopForward(ref nextNormal, hit.normal)) return;
                }
                else if (hit.normal.y > 0 && !WithinSlopeAngle(hit.normal))
                {
                    //moveTestString += "Hit a steep slope.\n";
                    scaleByDot = true;
                    leftover.y = 0;
                    if (StopForward(ref nextNormal, hit.normal)) return;
                }
                    

                if (Grounded && prevNormal.y > 0 && hit.normal.y < 0) //Floor to Cieling
                {
                    if (FloorCeilingLock(prevNormal, hit.normal)) return;
                }
                else if (Grounded && prevNormal.y < 0 && hit.normal.y > 0) //Ceiling to Floor
                {
                    if (FloorCeilingLock(hit.normal, prevNormal)) return;
                }

                bool FloorCeilingLock(Vector3 floorNormal, Vector3 ceilingNormal)
                {
                    //moveTestString += "Encountered Vertical Squish.\n";
                    scaleByDot = true;
                    return StopForward(ref nextNormal, floorNormal.y != floorNormal.magnitude ? floorNormal : ceilingNormal);
                }
                    
            }
            else
            {
                //moveTestString += "Isnt Grounded.\n";


                if (Mathf.Approximately(hit.normal.y, 0))
                {
                    //moveTestString += "Hit a Wall.\n";
                    if (StopForward(ref nextNormal, hit.normal)) return;
                }
                else if(hit.normal.y > 0)
                {
                    if(WithinSlopeAngle(hit.normal))
                    {
                        //moveTestString += "Landed on a standable ground.\n";
                        Land(hit);
                        leftover.y = 0;
                    }
                    else
                    {
                        //moveTestString += "Hit a steep slope while falling.\n";
                    }
                }
                else
                {
                    //moveTestString += "Hit a sloped ceiling while jumping.\n";
                }
            }


            Vector3 newDir = leftover.ProjectAndScale(nextNormal);
            if (scaleByDot) newDir *= Vector3.Dot(leftover.normalized, nextNormal) + 1;
            Move(newDir, nextNormal, step + 1);
        }
        else
        {
            //moveTestString += "No Hit\n";

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
    /// Called during <see cref="Move"/> to stop the forward movement and update the next normal vector. Overridable.
    /// </summary>
    /// <param name="nextNormal">A reference to the vector that will be updated to the normalized XZ components of <paramref name="newNormal"/>.</param>
    /// <param name="newNormal">The vector whose XZ components are used to calculate the updated normal.</param>
    /// <returns>Whether the Collide and Slide Algorithm should truly stop..</returns>
    protected virtual bool StopForward(ref Vector3 nextNormal, Vector3 newNormal)
    {
        nextNormal = newNormal.XZ().normalized;
        return false;
    }

    /// <summary>
    /// Called during <see cref="Move"/> to move this body forward. Overridable.
    /// </summary>
    /// <param name="offset">The offset to move by.</param>
    /// <returns>True if the movement was successful, false otherwise.</returns>
    protected virtual bool MoveForward(Vector3 offset)
    {
        Position += offset;
        return true;
    }

    /// <summary>
    /// Called during <see cref="Move"/> when the character walks off a ledge or platform. Overridable.
    /// </summary>
    protected virtual void WalkOff()
    {
        UnLand();
    }

    //public string moveTestString = "";

    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest.
    /// </summary>
    /// <param name="direction">The direction the Rigidbody is going.</param>
    /// <param name="distance">The distance the Rigidbody is set to travel.</param>
    /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
    /// <param name="hit">The resulting Hit.</param>
    /// <returns>Whether anything was Hit.</returns>
    public virtual bool DirectionCast(Vector3 direction, float distance, float buffer, out RaycastHit hit)
    {
        if (buffer > 0) _rb.MovePosition(_rb.position - direction * buffer);
        bool result = _rb.SweepTest(direction.normalized, out hit, distance + buffer, QueryTriggerInteraction.Ignore);
        if (buffer > 0) _rb.MovePosition(_rb.position + direction * buffer);
        hit.distance -= buffer;
        return result;
    }
    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest. (Returns Multiple.)
    /// </summary>
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

    /// <summary>
    /// Checks if the character is grounded and outputs the ground hit information.
    /// </summary>
    /// <param name="groundHit">The anchor point of the ground hit.</param>
    /// <returns>True if grounded, false otherwise.</returns>
    public virtual bool GroundCheck(out AnchorPoint groundHit)
    {
        bool result = DirectionCast(Vector3.down, groundCheckBuffer, groundCheckBuffer, out RaycastHit raycast) && WithinSlopeAngle(raycast.normal);
        groundHit = raycast;
        return result;
    }

    /// <summary>
    /// Lands the body on the ground described by the AnchorPoint.
    /// </summary>
    /// <param name="groundHit">The anchor point of the ground hit.</param>
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
    /// <summary>
    /// Event invoked when the character lands.
    /// </summary>
    public Action LandEvent;
    /// <summary>
    /// Tells this body it is leaving the ground and what JumpState to enter.
    /// </summary>
    /// <param name="newState">The new jump state to set. Defaults to Falling.</param>
    public virtual void UnLand(JumpState newState = JumpState.Falling)
    {
        if (newState < JumpState.Jumping) return;
        jumpState = newState;
        anchorPoint = AnchorPoint.Null;
        if (movingAnchor != null)
        {
            movingAnchor.RemoveBody(this);
            movingAnchor = null;
        }
    }

    /// <summary>
    /// Instantly snaps the character to the floor below, if any.
    /// </summary>
    /// <returns>True if snapped to floor, false otherwise.</returns>
    public bool InstantSnapToFloor()
    {
        if (DirectionCast(Vector3.down, 1000, .5f, out RaycastHit hit))
        {
            Position += Vector3.down * hit.distance;
            return true;
        }
        return false;
    }
    /// <summary>
    /// Instantly snaps the character to the floor below, if any, and outputs the hit information.
    /// </summary>
    /// <param name="hit">The RaycastHit of the floor.</param>
    /// <returns>True if snapped to floor, false otherwise.</returns>
    public bool InstantSnapToFloor(out RaycastHit hit)
    {
        if (DirectionCast(Vector3.down, 1000, .5f, out hit))
        {
            Position += Vector3.down * hit.distance;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Handles collision events with other objects.
    /// </summary>
    /// <param name="collision">The collision information.</param>
    protected virtual void OnCollisionEnter(Collision collision)
    {
        Vector3 contactPoint = collision.GetContact(0).normal;
        if (!Grounded && velocity.y > .1f && Vector3.Dot(contactPoint, Vector3.up) < -0.75f) velocity.y = 0;
        else if (!Grounded && WithinSlopeAngle(contactPoint))
            Land(collision.GetContact(0));

    } 

    /// <summary>
    /// Determines if the given normal is within the allowed slope angle.
    /// </summary>
    /// <param name="inNormal">The normal to check.</param>
    /// <returns>True if within the slope angle, false otherwise.</returns>
    private bool WithinSlopeAngle(Vector3 inNormal) => Vector3.Angle(Vector3.up, inNormal) < maxSlopeNormalAngle;

    /// <summary>
    /// Runs the calculations to automatically apply the current gravity to this body.
    /// </summary>
    public virtual void ApplyGravity() => velocity -= gravity * Time.fixedDeltaTime;

}

/// <summary>
/// The possible states of a jump.
/// </summary>
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
