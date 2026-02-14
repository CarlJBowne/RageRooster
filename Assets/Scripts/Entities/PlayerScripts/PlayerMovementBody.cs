using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using SLS.ISingleton;
using SLS.StateMachineH;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(StateMachine))]
public class PlayerMovementBody : MonoBehaviour, ISingleton<PlayerMovementBody>
{
    #region Config

    /// <summary>
    /// The default gravity vector for this body.
    /// </summary>
    [SerializeField] protected Vector3 defaultGravity = new(0, 1, 0);
    /// <summary>
    /// Whether Gravity should be automatically applied or applied by some behavior
    /// </summary>
    public bool autoApplyGravity = false;
    /// <summary>
    /// The maximum angle (in degrees) of a slope this body can stand on.
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

    public PlayerAirborneMovement jumpState1;
    public PlayerWallJump wallJumpState;
    public PlayerAirborneMovement airChargeState;
    public PlayerAirborneMovement doubleJump;
    public Vector3 frontCheckDefaultOffset;
    public float frontCheckDefaultRadius;
    public bool Mario64StyleAntiVoid;
    public LayerMask nonVoidLayerMask;
    public State idleState;
    public State airNeutralState;

    #endregion

    #region Components

    /// <summary>
    /// The Rigidbody component attached to this body.
    /// </summary>
    [field: SerializeField, HideInInspector] public Rigidbody RB { get; private set; }
    /// <summary>
    /// The CapsuleCollider component attached to this body.
    /// </summary>
    [field: SerializeField, HideInInspector] public CapsuleCollider Collider { get; private set; }

    #endregion

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
    /// The active direction of this body. Simpler controllers can probably avoid using this.
    /// </summary>
    public Vector3 direction = new(0, 0, 1);
    /// <summary>
    /// The active gravity value. (Inverted. y=1 is down.)
    /// </summary>
    [NonSerialized] private Vector3 gravity = new(0, 9.8f, 0);

    /// <summary>
    /// The possible states for the body.
    /// </summary>
    public enum BodyState
    {
        Enabled,
        Kinematic,
        Ragdoll,
        OFF
    }
    /// <summary>
    /// The current state of this body.
    /// </summary>
    public BodyState RBState
    {
        get => _rbState;
        set
        {
            _rbState = value;
            switch (value)
            {
                case BodyState.Enabled:
                    RB.isKinematic = false;
                    RB.detectCollisions = true;
                    RB.useGravity = false;
                    Collider.enabled = true;
                    break;
                case BodyState.Kinematic:
                    RB.isKinematic = true;
                    RB.detectCollisions = true;
                    RB.useGravity = false;
                    Collider.enabled = true;
                    break;
                case BodyState.Ragdoll:
                    RB.isKinematic = false;
                    RB.detectCollisions = true;
                    RB.useGravity = true;
                    Collider.enabled = false;
                    break;
                case BodyState.OFF:
                    RB.isKinematic = true;
                    RB.detectCollisions = false;
                    RB.useGravity = false;
                    Collider.enabled = false;
                    break;
            }
        }
    }
    private BodyState _rbState = BodyState.Enabled;

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

    #endregion

    #region Player Specific Fields

    [HideInInspector] public PlayerStateMachine Machine;
    [HideInInspector] public PlayerController playerController;
    [HideInInspector] public Animator animator;

    [HideInInspector] public bool baseMovability = true;
    [HideInInspector] public bool canJump = true;
    public float movementModifier = 1;
    public float CurrentSpeed
    {
        get => currentSpeed;
        set => currentSpeed = value.Min(0);
    }
    [HideInEditMode, DisableInPlayMode, SerializeField] private float currentSpeed;

    public static System.Action MovingUpdateAction;
    private Timer.Loop _movingUpdateActionTimer = new(0.2f);

    private VolcanicVent _currentVent;
    #endregion

    #region Internals

    /// <summary>
    /// The initial velocity used in the current physics step (scaled by fixedDeltaTime).
    /// </summary>
    Vector3 initVelocity;
    /// <summary>
    /// The initial normal used in the current physics step.
    /// </summary>
    Vector3 initNormal;

    public string moveTestString = "";

    #endregion

    #region GetSet

    public void VelocitySet(float? x = null, float? y = null, float? z = null)
    {
        velocity = new Vector3(
            x ?? velocity.x,
            y ?? velocity.y,
            z ?? velocity.z
            );
    }

    /// <summary>
    /// Sets the position even if the Rigidbody is kinematic.
    /// </summary>
    /// <param name="newPosition">The new position.</param>
    public void ForceSetPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
        RB.position = newPosition;
        RB.MovePosition(newPosition);
    }

    public void DirectionSet(Vector3 target, float maxTurnSpeed)
    {
        if (target == Vector3.zero) return;
        direction = Vector3.RotateTowards(direction, target.normalized, maxTurnSpeed * Mathf.PI * Time.deltaTime, 1);
        RotationQ = Quaternion.LookRotation(direction, Vector3.up);
    }
    public void DirectionSet(float maxTurnSpeed) { if (playerController != null) DirectionSet(playerController.camAdjustedMovement, maxTurnSpeed); }
    public void InstantDirectionChange(Vector3 target)
    {
        if (target.sqrMagnitude == 0) return;
        direction = target;
        RotationQ = Quaternion.LookRotation(direction, Vector3.up);
    }

    public VolcanicVent currentVent
    {
        get => _currentVent;
        set
        {
            _currentVent = value;
            Machine?.SendSignal(new(value != null ? "EnterVent" : "ExitVent", 0, true));
        }
    }
    public bool isOverVent => _currentVent != null;

    #endregion

    #region GetSets (Position/Rotation)

    /// <summary>
    /// Gets or sets the position of the character.
    /// </summary>
    public Vector3 Position
    {
        get => RB != null && RB.isKinematic ? transform.position : RB.position;
        set
        {
            if (RB != null && RB.isKinematic)
                return;
            transform.position = value;
            if (RB != null)
            {
                RB.position = value;
                RB.MovePosition(value);
            }
            OnSetPosition(value);
        }
    }
    /// <summary>
    /// Gets or sets the rotation of the Rigidbody as a Quaternion.
    /// </summary>
    public Quaternion RotationQ
    { get => RB != null ? RB.rotation : transform.rotation; set { if (RB != null) RB.rotation = value; transform.rotation = value; } }
    /// <summary>
    /// Gets or sets the rotation of the character in Euler angles.
    /// </summary>
    public Vector3 Rotation
    {
        get => transform.eulerAngles;
        set => transform.eulerAngles = value;
    }

    #endregion

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
    public Vector3 center => Position + (Collider != null ? Collider.center : Vector3.zero);

    #endregion

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

    protected virtual void OnSetPosition(Vector3 newPos) { }

    #endregion

    #region Singleton Stuff
    protected static PlayerMovementBody Instance;
    protected ISingleton<PlayerMovementBody> Interface => this;
    public static PlayerMovementBody Get() => ISingleton<PlayerMovementBody>.Get(ref Instance);
    public static bool TryGet(out PlayerMovementBody result) => ISingleton<PlayerMovementBody>.TryGet(Get, out result);
    public static bool Loaded => Instance != null;
    #endregion

    protected void Awake()
    {
        if (RB == null) RB = GetComponent<Rigidbody>();
        if (Collider == null) Collider = GetComponent<CapsuleCollider>();

        // Snap to floor if possible (CharacterMovementBody behavior)
        if (InstantSnapToFloor(out RaycastHit hit)) Land(hit);

        TryGetComponent(out animator);
        direction = Vector3.forward;
        RotationQ = Quaternion.LookRotation(direction, Vector3.up);

        Interface.Initialize(ref Instance);
    }

    private void OnDestroy() => Interface.DeInitialize(ref Instance);

    protected void OnEnable()
    {
        if (_rbState == BodyState.OFF)
            RBState = BodyState.Enabled;
    }

    private void OnDisable()
    {
        RBState = BodyState.OFF;
    }

    protected void FixedUpdate()
    {
        // Player pre-fixed update behavior
        Player.Animator.SetFloat("CurrentSpeed", currentSpeed);
        if (Upgrades.Active.d_moonJump && Input.Jump.IsPressed()) VelocitySet(y: 10f);

        Vector3 prePos = Position;

        DebugRR.DebugTextOverlay.SetText($"PMB : Velocity: {velocity}");

        // --- Begin CharacterMovementBody.FixedUpdate logic ---
        if (RBState != BodyState.Enabled) return;

        if (RB != null)
        {
            RB.linearVelocity = Vector3.zero;
            RB.angularVelocity = Vector3.zero;
        }

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

        moveTestString = "";
        Move(initVelocity, initNormal);

        if (autoApplyGravity && !Grounded) ApplyGravity();
        // --- End CharacterMovementBody.FixedUpdate logic ---

        if (prePos != Position) _movingUpdateActionTimer.Tick(MovingUpdateAction);
    }

    /// <summary>
    /// The Collide and Slide Algorithm.
    /// </summary>
    /// <param name="vel">Input Velocity.</param>
    /// <param name="prevNormal">The Normal of the previous Step.</param>
    /// <param name="step">The current step. Starts at 0.</param>
    protected virtual void Move(Vector3 vel, Vector3 prevNormal, int step = 0, bool testString = false)
    {
        if (testString) moveTestString += $"Step {step}: {vel}\n";

        if (step == 0 && vel.y <= 0)
        {
            bool tryGround = GroundCheck(out var groundRes);
            if (Grounded && !tryGround) UnLand();
            else if (!Grounded && tryGround) Land(groundRes);
        }

        if (DirectionCast(vel.normalized, vel.magnitude, groundCheckBuffer, out RaycastHit hit))
        {
            if (testString) moveTestString += $"Hit: {hit.normal} at distance {hit.distance}\n";
            Vector3 snapToSurface = vel.normalized * hit.distance;
            Vector3 leftover = vel - snapToSurface;
            Vector3 nextNormal = hit.normal;
            bool scaleByDot = false;

            if (step == movementProjectionSteps) return;

            if (!MoveForward(snapToSurface)) return;

            else if (Grounded)
            {
                if (testString) moveTestString += "Is Grounded.\n";

                if (Mathf.Approximately(hit.normal.y, 0))
                {
                    if (testString) moveTestString += "Hit a wall.\n";
                    scaleByDot = true;
                    leftover.y = 0;
                    if (StopForward(ref nextNormal, hit.normal)) return;
                }
                else if (hit.normal.y > 0 && !WithinSlopeAngle(hit.normal))
                {
                    if (testString) moveTestString += "Hit a steep slope.\n";
                    scaleByDot = true;
                    leftover.y = 0;
                    if (StopForward(ref nextNormal, hit.normal)) return;
                }

                if (Grounded && prevNormal.y > 0 && hit.normal.y < 0) //Floor to Ceiling
                {
                    if (FloorCeilingLock(prevNormal, hit.normal)) return;
                }
                else if (Grounded && prevNormal.y < 0 && hit.normal.y > 0) //Ceiling to Floor
                {
                    if (FloorCeilingLock(hit.normal, prevNormal)) return;
                }

                bool FloorCeilingLock(Vector3 floorNormal, Vector3 ceilingNormal)
                {
                    if (testString) moveTestString += "Encountered Vertical Squish.\n";
                    scaleByDot = true;
                    return StopForward(ref nextNormal, floorNormal.y != floorNormal.magnitude ? floorNormal : ceilingNormal);
                }

            }
            else
            {
                if (testString) moveTestString += "Isnt Grounded.\n";

                if (Mathf.Approximately(hit.normal.y, 0))
                {
                    if (testString) moveTestString += "Hit a Wall.\n";
                    if (StopForward(ref nextNormal, hit.normal)) return;
                }
                else if (hit.normal.y > 0)
                {
                    if (WithinSlopeAngle(hit.normal))
                    {
                        if (testString) moveTestString += "Landed on a standable ground.\n";
                        Land(hit);
                        leftover.y = 0;
                    }
                    else
                    {
                        if (testString) moveTestString += "Hit a steep slope while falling.\n";
                    }
                }
                else
                {
                    if (testString) moveTestString += "Hit a sloped ceiling while jumping.\n";
                }
            }

            Vector3 newDir = leftover.ProjectAndScale(nextNormal);
            if (scaleByDot && leftover.sqrMagnitude > 0f) newDir *= Vector3.Dot(leftover.normalized, nextNormal) + 1;
            Move(newDir, nextNormal, step + 1);
        }
        else
        {
            if (testString) moveTestString += "No Hit\n";

            if (step == movementProjectionSteps) return;
            if (!MoveForward(vel)) return;

            // Snap to ground when walking on a downward slope.
            if (Grounded && initVelocity.y <= 0)
            {
                if (DirectionCast(Vector3.down, 0.5f, groundCheckBuffer, out RaycastHit groundHit))
                {
                    // Make sure the hit is under the character's feet (not beside it).
                    // Compute the bottom-center point of the capsule in world space.
                    Vector3 bottomCenter = Position + Collider.center - Vector3.up * (Collider.height * 0.5f - Collider.radius);
                    Vector3 horizontalDelta = new(groundHit.point.x - bottomCenter.x, 0f, groundHit.point.z - bottomCenter.z);

                    // Allow a small tolerance because of floating precision and scale.
                    float allowedRadius = Collider.radius + 0.05f;

                    if (horizontalDelta.sqrMagnitude <= allowedRadius * allowedRadius)
                    {
                        // Ground is under the feet -> snap down.
                        Position += Vector3.down * groundHit.distance;
                    }
                    else
                    {
                        // Hit was off to the side (ledge), so walk off instead of snapping.
                        WalkOff();
                    }
                }
                else
                {
                    WalkOff();
                }
            }
        }
    }

    /// <summary>
    /// Called during Move to stop the forward movement and update the next normal vector. Overridable.
    /// </summary>
    /// <param name="nextNormal">A reference to the vector that will be updated to the normalized XZ components of newNormal.</param>
    /// <param name="newNormal">The vector whose XZ components are used to calculate the updated normal.</param>
    /// <returns>Whether the Collide and Slide Algorithm should truly stop..</returns>
    protected virtual bool StopForward(ref Vector3 nextNormal, Vector3 newNormal)
    {
        nextNormal = newNormal.XZ().normalized;
        // Player-specific behavior: send Bonk signal, return whether it locked movement
        return Machine != null ? Machine.SendSignal(new("Bonk", 0, true)) : false;
    }

    /// <summary>
    /// Default forward movement behavior.
    /// </summary>
    protected virtual bool DefaultMoveForward(Vector3 offset)
    {
        Position += offset;
        return true;
    }

    /// <summary>
    /// Called during Move to move this body forward. Overridable.
    /// </summary>
    /// <param name="offset">The offset to move by.</param>
    /// <returns>True if the movement was successful, false otherwise.</returns>
    protected virtual bool MoveForward(Vector3 offset)
    {
        if (Mario64StyleAntiVoid && !Physics.Raycast(transform.position + Vector3.up + offset, Vector3.down, 5000, nonVoidLayerMask, QueryTriggerInteraction.Collide))
        {
            velocity = Vector3.zero;
            return false;
        }
        else return DefaultMoveForward(offset);
    }

    /// <summary>
    /// Called during Move when the character walks off a ledge or platform. Overridable.
    /// </summary>
    protected virtual void WalkOff()
    {
        UnLand();
        Machine?.SendSignal(new("WalkOff", ignoreLock: true));
    }

    #region Casting

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
        hit = default;
        if (RB == null) return Physics.Raycast(transform.position, direction, out hit, distance, ~0, QueryTriggerInteraction.Ignore);

        if (buffer > 0) RB.MovePosition(RB.position - direction * buffer);
        bool result = RB.SweepTest(direction.normalized, out hit, distance + buffer, QueryTriggerInteraction.Ignore);
        if (buffer > 0) RB.MovePosition(RB.position + direction * buffer);
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
        if (RB == null)
        {
            hit = Physics.SphereCastAll(transform.position, 0.5f, direction, distance, ~0, QueryTriggerInteraction.Ignore);
            return hit.Length > 0;
        }

        if (buffer > 0) RB.MovePosition(RB.position - direction * buffer);
        hit = RB.SweepTestAll(direction.normalized, distance + buffer, QueryTriggerInteraction.Ignore);
        if (buffer > 0) RB.MovePosition(RB.position + direction * buffer);
        if (hit.Length > 0) hit[0].distance -= buffer;
        return hit.Length > 0;
    }

    #endregion

    #region Grounding / Landing

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
            movingAnchor?.SetPlayerInfluence(false);
            movingAnchor = anchorPoint.transform.GetComponent<IMovablePlatform>();
            movingAnchor?.SetPlayerInfluence(true);
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
            movingAnchor.SetPlayerInfluence(false);
            movingAnchor = null;
        }
    }

    #endregion

    #region Snap / Collision

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

    #endregion

    private bool WithinSlopeAngle(Vector3 inNormal) => Vector3.Angle(Vector3.up, inNormal) < maxSlopeNormalAngle;

    /// <summary>
    /// Runs the calculations to automatically apply the current gravity to this body.
    /// </summary>
    public virtual void ApplyGravity() => velocity -= gravity * Time.fixedDeltaTime;

    #region Player Utility / Front Checks

    public T CheckForTypeInFront<T>(Vector3 sphereOffset, float checkSphereRadius)
    {
        Collider[] results = Physics.OverlapSphere(center + transform.TransformDirection(sphereOffset),
                                                   checkSphereRadius);
        foreach (Collider r in results)
            if (r.TryGetComponent(out T result))
                return result;
        return default;
    }
    public T CheckForTypeInFront<T>()
    {
        Collider[] results = Physics.OverlapSphere(center + transform.TransformDirection(frontCheckDefaultOffset),
                                                   frontCheckDefaultRadius);
        foreach (Collider r in results)
            if (r.gameObject != gameObject && r.TryGetComponent(out T result))
                return result;
        return default;
    }

    public void ReturnToNeutral(bool doCrossFade = true)
    {
        if (GroundCheck(out _))
        {
            idleState.Enter();
            if (doCrossFade && animator != null) animator.CrossFade("GroundBasic", .1f);
        }
        else
            airNeutralState.Enter();
    }

    private CoroutinePlus QuickTurnRoutine;
    public void QuickTurnTime(Vector3 newForward, float length)
    {
        newForward = newForward.XZ(); //Ensure no weird rotations

        if (length <= 0f)
        {
            direction = newForward;
            RotationQ = Quaternion.LookRotation(direction, Vector3.up);
            return;
        }

        QuickTurnRoutine = Enum().Begin(Player.MovementBody);
        IEnumerator Enum()
        {
            float deltaRad = Vector3.Angle(direction, newForward) * Mathf.Deg2Rad;
            float rateRadPerSec = deltaRad / length; // radians per second

            while (deltaRad > 0f)
            {
                direction = Vector3.RotateTowards(direction, newForward, rateRadPerSec * Time.fixedDeltaTime, 0f);
                RotationQ = Quaternion.LookRotation(direction, Vector3.up);
                yield return new WaitForFixedUpdate();
                deltaRad -= rateRadPerSec * Time.fixedDeltaTime;
            }
            direction = newForward;
            RotationQ = Quaternion.LookRotation(direction, Vector3.up);
        }
    }
    public void QuickTurnLimited(Vector3 newForward, float maxDelta)
    {
        newForward = newForward.XZ(); //Ensure no weird rotations
        if (maxDelta <= 0f) return;

        QuickTurnRoutine = Enum().Begin(Player.MovementBody);
        IEnumerator Enum()
        {
            float fullDelta = Vector3.Angle(direction, newForward) * Mathf.Deg2Rad;

            while (fullDelta > 0f)
            {
                direction = Vector3.RotateTowards(direction, newForward, maxDelta * Time.fixedDeltaTime, 0f);
                RotationQ = Quaternion.LookRotation(direction, Vector3.up);
                yield return null;
                fullDelta -= maxDelta * Time.fixedDeltaTime;
            }

            direction = newForward;
            RotationQ = Quaternion.LookRotation(direction, Vector3.up);
        }
    }

    #endregion

#if UNITY_EDITOR

    private List<HitNormalDisplay> queuedHits = new();
    private void AddToQueuedHits(HitNormalDisplay hit)
    {
        queuedHits.Add(hit);
        if (queuedHits.Count > 100) queuedHits.RemoveAt(0);
    }
    private void OnDrawGizmos()
    {
        foreach (HitNormalDisplay item in queuedHits) Debug.DrawRay(item.position, item.normal / 10);
        foreach (Vector3 item in jumpMarkers) Handles.DrawWireDisc(item, Vector3.up, 0.5f);
    }

    public List<Vector3> jumpMarkers = new();

#endif

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
}