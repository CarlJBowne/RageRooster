using System;
using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using SLS.ISingleton;
using SLS.StateMachineH;
using UnityEditor;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(StateMachine))]
public sealed class PlayerMovementBody : MonoBehaviour, ISingleton<PlayerMovementBody>
{
    #region Config
    /// <summary>
    /// The default gravity vector for this <see cref="CharacterMovementBody"/>.
    /// </summary>
    [SerializeField] Vector3 defaultGravity = new(0, 1, 0);
    /// <summary>
    /// Whether Gravity should be automaticall applied or applied by some behavior
    /// </summary>
    public bool autoApplyGravity = false;
    /// <summary>
    /// The maximum angle (in degrees) of a slope this <see cref="CharacterMovementBody"/> can stand on.
    /// </summary>
    [SerializeField] float maxSlopeNormalAngle = 45f;
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
    /// <summary>
    /// Angle threshold for Bonking.
    /// </summary>
    public float bonkThreshold = 15;

    /// <summary>
    /// Whether the player can currnetly do a double jump.
    /// </summary>
    public PlayerAirborneMovement doubleJump;
    /// <summary>
    /// The default offset for a Front-ways collision Check.
    /// </summary>
    public Vector3 frontCheckDefaultOffset;
    /// <summary>
    /// the default radius for a Front-ways collision Check.
    /// </summary>
    public float frontCheckDefaultRadius;

    //public PlatformDetectionMethod platformDetection;
    //[Tooltip("An unconventional means of hopefully avoiding falling through the floor. If true, the player will check if there is any ground below them before moving, and if not, their velocity will be set to 0 to prevent them from moving further. This is jank, but it might help with some edge cases and it doesn't require any extra components or setup.")]
    //public PlatformDetectionMethod mario64StyleAntiVoid;
    /// <summary>
    /// The LayerMask used for ground checks. Should be set to anything that can be stood on.
    /// </summary>
    public LayerMask validGroundMask;

    public bool cantWalkOff;
    public float platformDetectionFactor = 3;
    public float platformLockRadius = .25f;


    #endregion

    #region Components

    /// <summary>
    /// The Rigidbody component attached to this <see cref="CharacterMovementBody"/>.
    /// </summary>
    [field: SerializeField, HideInInspector] public Rigidbody RB { get; private set; }
    /// <summary>
    /// The <see cref="CapsuleCollider"/> component attached to this <see cref="CharacterMovementBody"/>.
    /// </summary>
    [field: SerializeField, HideInInspector] public CapsuleCollider Collider { get; private set; }
    //[RelatedComponent(true)] public NavMeshAgent NavAgent;

    #endregion


    #region LifeCycle

    void Awake()
    {
        if (RB == null) RB = GetComponent<Rigidbody>();
        if (Collider == null) Collider = GetComponent<CapsuleCollider>();

        if (InstantSnapToFloor(out RaycastHit hit)) Land(hit);

        direction = Vector3.forward;
        Interface.Initialize(ref Instance);

        //NavAgent.updatePosition = false;
        //NavAgent.updateRotation = false;
        //NavAgent.updateUpAxis = false;
    }

    /// <summary>
    /// Called when the component is enabled.
    /// </summary>
    void OnEnable()
    {
        if (_rbState == BodyState.OFF)
            RBState = BodyState.Enabled;
    }
    /// <summary>
    /// Called when the component is disabled.
    /// </summary>
    void OnDisable()
    {
        RBState = BodyState.OFF;
    }

    void OnDestroy() => Interface.DeInitialize(ref Instance);

    #region Singleton Stuff
    static PlayerMovementBody Instance;
    ISingleton<PlayerMovementBody> Interface => this;
    public static PlayerMovementBody Get() => ISingleton<PlayerMovementBody>.Get(ref Instance);
    public static bool TryGet(out PlayerMovementBody result) => ISingleton<PlayerMovementBody>.TryGet(Get, out result);
    public static bool Loaded => Instance != null;
    #endregion

    #endregion LifeCycle


    #region Move Cycle

    void FixedUpdate()
    {
        sweepsThisPhysUpdate.Clear();
        Player.Animator.SetFloat("CurrentSpeed", currentSpeed);
        if (Upgrades.Active.d_moonJump && Input.Jump.IsPressed()) VelocitySet(y: 10f);

        //NavAgent.nextPosition = Position;

        Vector3 prePos = Position;

        DebugRR.DebugTextOverlay.SetText($"PMB : Velocity: {velocity}");

        if (RBState != BodyState.Enabled) return;
        RB.linearVelocity = Vector3.zero;
        RB.angularVelocity = Vector3.zero;

        stepZeroVelocity = velocity * Time.fixedDeltaTime;
        stepZeroAnchor = anchorPoint;

        moveTestString = "";

        if (velocity != Vector3.zero) Move(stepZeroVelocity);

        if (velocity.y <= 0)
        {
            if (GroundCheck(out AnchorPoint groundHit))
            {
                if (!Grounded)
                {
                    Land(groundHit);
                    velocity.y = 0;
                }
            }
            else if (Grounded)
            {
                moveTestString += "Walk Off.\n";
                Player.StateMachine.SendSignal("WalkOff");
                UnLand(JumpState.Hangtime);
            }
        }

        DebugRR.DebugTextOverlay.SetText(moveTestString);

        if (autoApplyGravity && !Grounded) ApplyGravity();

        if (prePos != Position) _movingUpdateActionTimer.Tick(MovingUpdateAction);
    }


    void Move(Vector3 stepVelocity, int step = 0)
    {
        moveTestString += $"Step {step}: {stepVelocity}\n";

        if (stepVelocity == Vector3.zero) return;

        if (Grounded) stepVelocity = stepVelocity.ProjectAndScale(anchorPoint.normal);

        float stopDistance = -1;
        Vector3 nextNormal = Vector3.zero;
        bool scaleByDot = false;
        bool deleteVerticalLeftover = false;

        bool sweepHit = SweepBody(stepVelocity, out RaycastHit hit, groundCheckBuffer) && !(stepVelocity.y == 0 && hit.normal == Vector3.up);


        if (Grounded && !sweepHit)
        {
            moveTestString += $"Grounded, Hit Nothing.\n";

            {
                moveTestString += $"Doing Alt-Stop checks without any NavMesh connection. \n";

                Vector3 platformCheckDistance = stepVelocity.normalized * platformDetectionFactor;
                bool forwardCheckOp = SweepBody(Vector3.down * 5000, out RaycastHit platformCheckHit, groundCheckBuffer, Position + platformCheckDistance);

                if (forwardCheckOp && platformCheckHit.distance <= groundCheckBuffer + .001f && WithinSlopeAngle(platformCheckHit.normal)) { }
                else if (cantWalkOff || !forwardCheckOp)
                {
                    //Either didn't hit anything, meaning the player has reached the void,
                    //or cantWalkOff is currently enabled and the distance the check got was larger than platform detection.
                    moveTestString += cantWalkOff ? "Player is not allowed to walk off.\n" : "Hit the void while walking.\n";
                    Vector3 reachAroundPos = Position + (platformCheckDistance * 1.01f) - (Vector3.up * Collider.height / 2);
                    if (SweepBody(platformCheckDistance.XZ() * -2f, out RaycastHit reachAroundResult, 0, reachAroundPos))
                    {// Assume able to reach Platform from below.

                        nextNormal = -reachAroundResult.normal.XZ();
                        Plane P = new(nextNormal, reachAroundResult.point + (nextNormal * .6f));
                        P.Raycast(new(Position, stepVelocity), out stopDistance);

                        scaleByDot = true;
                        moveTestString += $"Found Platform to Lock at, nextNormal: {nextNormal}\n";
                    }
                    else moveTestString += "Walking off platform when not allowed but reach around check failed. Failsafe situation, report to CJ.\n";
                }
            }

            if (stopDistance == -1)
            {
                if (GroundCheck(out _, out RaycastHit groundCast, true) && groundCast.normal != anchorPoint.normal)
                {
                    Ray cornerCheckRay = new(groundCast.barycentricCoordinate + new Vector3(0, .1f, 0), Vector3.down);
                    bool different = groundCast.collider.Raycast(cornerCheckRay, out RaycastHit baryHit, .11f)
                        && baryHit.normal != groundCast.normal;

                    if (groundCast.distance >= float.Epsilon && groundCast.distance <= groundCheckBuffer && !different)
                    {
                        moveTestString += "Snapping to lowerGround.\n";
                        Position += Vector3.down * groundCast.distance;
                        anchorPoint = groundCast;
                    }
                }
            }
        }
        else if (Grounded && sweepHit)
        {
            moveTestString += $"Grounded, Hit: {hit.normal} at distance {hit.distance} \n";
            stopDistance = hit.distance;
            nextNormal = hit.normal;

            if (Mathf.Approximately(hit.normal.y, 0))
            {
                moveTestString += "Hit a wall.\n";
                scaleByDot = true;
                deleteVerticalLeftover = true;
                nextNormal = nextNormal.XZ().normalized;
            }
            else if (hit.normal.y > 0 && !WithinSlopeAngle(hit.normal))
            {
                moveTestString += "Hit a steep slope.\n";
                scaleByDot = true;
                deleteVerticalLeftover = true;
                nextNormal = nextNormal.XZ().normalized;
            }

            if (Grounded && anchorPoint.normal.y > 0 && hit.normal.y < 0) FloorCeilingLock(anchorPoint.normal, hit.normal);
            //Floor to Cieling
            else if (Grounded && anchorPoint.normal.y < 0 && hit.normal.y > 0) FloorCeilingLock(hit.normal, anchorPoint.normal);
            //Ceiling to Floor

            void FloorCeilingLock(Vector3 floorNormal, Vector3 ceilingNormal)
            {
                moveTestString += "Encountered Vertical Squish.\n";
                scaleByDot = true;
                nextNormal = floorNormal.y != floorNormal.magnitude ? floorNormal : ceilingNormal;
            }

            if (hit.normal.y > 0 && WithinSlopeAngle(hit.normal) && stepVelocity.y <= 0) anchorPoint = hit;
        }
        else if (!Grounded && sweepHit)
        {
            moveTestString += $"Airborne, Hit: {hit.normal} at distance {hit.distance} \n";
            stopDistance = hit.distance;
            nextNormal = hit.normal;

            if (Mathf.Approximately(hit.normal.y, 0)) moveTestString += "Hit a Wall mid-air.\n";
            else if (hit.normal.y > 0)
            {
                if (WithinSlopeAngle(hit.normal))
                {
                    moveTestString += "Landed on a standable ground.\n";
                    Land(hit);
                    deleteVerticalLeftover = true;
                }
                else moveTestString += "Hit a steep slope while falling.\n";
            }
            else if (!WithinSlopeAngle(-hit.normal))
            {
                moveTestString += "Hit a sloped ceiling while jumping.\n";
            }
            else
            {
                moveTestString += "Hit a ceiling while jumping.\n";
                deleteVerticalLeftover = true;
                velocity.y = -0.1f;
                UnLand(JumpState.Falling);
            }

            if (hit.normal.y > 0 && WithinSlopeAngle(hit.normal) && stepVelocity.y <= 0) anchorPoint = hit;
        }
        else
        {
            moveTestString += $"Airborne, Hit Nothing.\n";

            { //If not Grounded, skip straight to Checking for void.
                if (!SweepBody(Vector3.down * 5000, out _, 0, Position + stepVelocity))
                {//Since not necessarily anywhere near a platform, just stop the player in their tracks for now.
                    moveTestString += "Hit the void while falling.\n";
                    stopDistance = 0;
                    nextNormal = -stepVelocity.XZ();
                }
            }
        }


        Vector3 snapToSurface = stopDistance != -1 ? stepVelocity.normalized * stopDistance : stepVelocity;
        Position += snapToSurface;

        if (stopDistance == -1 || step + 1 >= movementProjectionSteps) return;
        else if (Vector3.Angle(stepVelocity.XZ(), -nextNormal.XZ()) < bonkThreshold && Player.StateMachine.SendSignal(new("Bonk", 0, true)))
        {
            this.velocity = Vector3.zero;
            return;
        }

        Vector3 leftover = stepVelocity - snapToSurface;
        if (deleteVerticalLeftover) leftover.y = 0;
        Vector3 newDir = leftover.ProjectAndScale(nextNormal);
        if (scaleByDot) newDir *= Vector3.Dot(leftover.normalized, nextNormal) + 1;
        Move(newDir, step + 1);
    }

    /// <summary>
    /// The initial (Unprojected) velocity prior to the current physics step. Used for reference in the Collide and Slide algorithm.
    /// </summary>
    Vector3 baseVelocity;
    /// <summary>
    /// The (Projected) velocity used in the very first physics step, kept for reference during later steps of Collide and Slide.
    /// </summary>
    Vector3 stepZeroVelocity;
    /// <summary>
    /// The AnchorPoint used in the very first physics step, kept for reference during later steps of Collide and Slide.
    /// </summary>
    AnchorPoint stepZeroAnchor;

    string moveTestString = "";

    #endregion Move Cycle


    #region Position

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
    /// Sets the position even if the Rigidbody is kinematic.
    /// </summary>
    /// <param name="newPosition">The new position.</param>
    public void ForceSetPosition(Vector3 newPosition)
    {
        transform.position = newPosition;
        RB.position = newPosition;
        RB.MovePosition(newPosition);
    }

    /// <summary>
    /// The center position of the character's collider.
    /// </summary>
    public Vector3 center => Position + Collider.center;

    void OnSetPosition(Vector3 newPos) { }


    #endregion Position

    #region Direction

    public Vector3 direction
    {
        get => _direction;
        private set
        {
            if (_direction == value) return;
            _direction = value;
            RotationQ = Quaternion.LookRotation(value, Vector3.up);
        }
    }

    /// <summary>
    /// The active direction of this <see cref="CharacterMovementBody"/>. Simpler controllers can probably avoid using this.
    /// </summary>
    public Vector3 _direction = new(0, 0, 1);


    public void DirectionSet(Vector3 target, float maxTurnSpeed)
    {
        if (target == Vector3.zero) return;
        direction = Vector3.RotateTowards(direction, target.normalized, maxTurnSpeed * Mathf.PI * Time.deltaTime, 1);
    }
    public void DirectionSet(float maxTurnSpeed) => DirectionSet(Player.Controller.camAdjustedMovement, maxTurnSpeed);
    public void InstantDirectionChange(Vector3 target)
    {
        if (target.sqrMagnitude == 0) return;
        direction = target;
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

    public void QuickTurnTime(Vector3 newForward, float length)
    {
        newForward = newForward.XZ(); //Ensure no weird rotations

        if (length <= 0f)
        {
            direction = newForward;
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
                yield return new WaitForFixedUpdate();
                deltaRad -= rateRadPerSec * Time.fixedDeltaTime;
            }
            direction = newForward;
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
                yield return null;
                fullDelta -= maxDelta * Time.fixedDeltaTime;
            }

            direction = newForward;
        }
    }
    private CoroutinePlus QuickTurnRoutine;




    #endregion Direction

    #region Velocity

    /// <summary>
    /// Custom velocity value.
    /// </summary>
    public Vector3 velocity = new(0, 0, 0);
    /// <summary>
    /// Custom angular velocity value.
    /// </summary>
    [NonSerialized] public Vector3 angularVelocity = new(0, 0, 0);

    public void VelocitySet(float? x = null, float? y = null, float? z = null)
    {
        velocity = new Vector3(
            x ?? velocity.x,
            y ?? velocity.y,
            z ?? velocity.z
            );
    }

    public float CurrentSpeed
    {
        get => currentSpeed;
        set => currentSpeed = value.Min(0);
    }
    [HideInEditMode, DisableInPlayMode, SerializeField] private float currentSpeed;

    public float movementModifier = 1;

    [HideInInspector] public bool baseMovability = true;
    [HideInInspector] public bool canJump = true;


    #endregion Velocity

    #region Gravity

    /// <summary>
    /// The active gravity value. (Inverted. y=1 is down.)
    /// </summary>
    [NonSerialized] private Vector3 gravity = new(0, 9.8f, 0);


    /// <summary>
    /// Runs the calculations to automatically apply the current gravity to this body.
    /// </summary>
    public void ApplyGravity() => velocity -= gravity * Time.fixedDeltaTime;

    /// <summary>
    /// Returns the current gravity vector. (Inverted. y=1 is downwards, y=-1 is upwards.)
    /// </summary>
    public Vector3 Get3DGravity() => gravity;
    /// <summary>
    /// Returns the current gravity value on the Y axis. (Inverted. 1 is downwards, -1 is upwards.)
    /// </summary>
    public float GetGravity() => gravity.y;

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



    #endregion Gravity


    #region Checks

    /// <summary>
    /// Casts the Rigidbody in a direction to check for collision using SweepTest. (Includes optional buffer)
    /// </summary>
    /// <param name="offset"></param>
    /// <param name="hit">The resulting Hit.</param>
    /// <param name="buffer">A buffer that the Rigidbody is temporarily moved backwards by before the Sweep Test.</param>
    /// <param name="tempOrigin">An optional temporary origin to move the Rigidbody to before the Sweep Test.</param>
    /// <param name="queryTriggerInteraction">Override to include trigger colliders in the Sweep Test.</param>
    /// <returns>Whether anything was Hit.</returns>
    public bool SweepBody(Vector3 offset, out RaycastHit hit,
        float buffer = 0, Vector3? tempOrigin = null, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        Vector3 originalPos = RB.position;
        if (tempOrigin.HasValue) RB.MovePosition(tempOrigin.Value);
        if (buffer > 0) RB.MovePosition(RB.position - (offset.normalized * buffer));
        bool result = RB.SweepTest(offset.normalized, out hit, offset.magnitude + buffer, queryTriggerInteraction);
        RB.MovePosition(originalPos);
        hit.distance = (hit.distance - buffer).Min(0);
        sweepsThisPhysUpdate.Add(new()
        {
            origin = tempOrigin.GetValueOrDefault(),
            direction = offset,
            hit = result,
            hitDistance = hit.distance,
            hitNormal = hit.normal
        });
        return result;
    }


    /// <summary>
    /// Checks if the character is grounded and outputs the ground hit information.
    /// </summary>
    /// <param name="groundHit">The anchor point of the ground hit.</param>
    /// <returns>True if grounded, false otherwise.</returns>
    public bool GroundCheck(out AnchorPoint groundHit, bool dontApply = false)
    {
        bool result = SweepBody(Vector3.down * groundCheckBuffer, out RaycastHit raycast, groundCheckBuffer) && WithinSlopeAngle(raycast.normal);
        groundHit = default;
        if (!dontApply) groundHit = raycast;
        return result;
    }
    /// <summary>
    /// Checks if the character is grounded and outputs the ground hit information.
    /// </summary>
    /// <param name="groundHit">The anchor point of the ground hit.</param>
    /// <returns>True if grounded, false otherwise.</returns>
    public bool GroundCheck(out AnchorPoint groundHit, out RaycastHit raycast, bool dontApply = false)
    {
        bool result = SweepBody(Vector3.down * groundCheckBuffer, out raycast, groundCheckBuffer) && WithinSlopeAngle(raycast.normal);
        groundHit = default;
        if (!dontApply) groundHit = raycast;
        return result;
    }

    public bool OverVoidCheck(Vector3 offset) => !SweepBody(Vector3.down * 5000, out _, 0, offset, QueryTriggerInteraction.Collide);

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

    #endregion

    #region Ground


    /// <summary>
    /// Handles collision events with other objects.
    /// </summary>
    /// <param name="collision">The collision information.</param>
    void OnCollisionEnter(Collision collision)
    {
        Vector3 contactPoint = collision.GetContact(0).normal;
        if (!Grounded && velocity.y > .1f && Vector3.Dot(contactPoint, Vector3.up) < -0.75f) velocity.y = 0;
        else if (!Grounded && WithinSlopeAngle(contactPoint))
            Land(collision.GetContact(0));

    }

    public void Land(AnchorPoint groundHit)
    {
        bool wasntGrounded = jumpState != JumpState.Grounded;
        bool objectChange = anchorPoint.collider != groundHit.collider;
        doubleJump.allowDoubleJump = true;

        if (!wasntGrounded && !objectChange) return;

        jumpState = JumpState.Grounded;
        anchorPoint = groundHit;
        velocity.y = 0;

        if (objectChange)
        {
            movingAnchor?.SetPlayerInfluence(false);
            movingAnchor = anchorPoint.collider.GetComponent<IMovablePlatform>();
            movingAnchor?.SetPlayerInfluence(true);
        }

        if (wasntGrounded)
        {
            LandEvent?.Invoke();
            Player.StateMachine.SendSignal(new("Land", ignoreLock: true));
            if (Player.Controller.CheckJumpBuffer()) Player.StateMachine.SendSignal("Jump");
        }
    }
    /// <summary>
    /// Lands the body on the ground described by the AnchorPoint.
    /// </summary>
    /// <param name="groundHit">The anchor point of the ground hit.</param>
    public void Land()
    {
        if (!GroundCheck(out AnchorPoint groundHit)) return;
        Land(groundHit);
        doubleJump.allowDoubleJump = true;
    }
    /// <summary>
    /// Event invoked when the character lands.
    /// </summary>
    public Action LandEvent;
    /// <summary>
    /// Tells this body it is leaving the ground and what JumpState to enter.
    /// </summary>
    /// <param name="newState">The new jump state to set. Defaults to Falling.</param>
    public void UnLand(JumpState newState = JumpState.Falling)
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

    void WalkOff()
    {
        UnLand();
        Player.StateMachine.SendSignal(new("WalkOff", ignoreLock: true));
    }

    /// <summary>
    /// Instantly snaps the character to the floor below, if any.
    /// </summary>
    /// <returns>True if snapped to floor, false otherwise.</returns>
    public bool InstantSnapToFloor()
    {
        if (SweepBody(Vector3.down * 1000, out RaycastHit hit, .5f))
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
        if (SweepBody(Vector3.down * 1000, out hit, .5f))
        {
            Position += Vector3.down * hit.distance;
            return true;
        }
        return false;
    }

    /// <summary>
    /// Determines if the given normal is within the allowed slope angle.
    /// </summary>
    /// <param name="inNormal">The normal to check.</param>
    /// <returns>True if within the slope angle, false otherwise.</returns>
    private bool WithinSlopeAngle(Vector3 inNormal) => Vector3.Angle(Vector3.up, inNormal) < maxSlopeNormalAngle;

    /// <summary>
    /// The current anchor point this body is attached to.
    /// </summary>
    AnchorPoint anchorPoint = AnchorPoint.Null;
    /// <summary>
    /// The current moving platform this body is anchored to, if any.
    /// </summary>
    IMovablePlatform movingAnchor;

    #endregion Ground

    #region States

    /// <summary>
    /// The possible states for a <see cref="CharacterMovementBody"/>.
    /// </summary>
    public enum BodyState
    {
        Enabled,
        Kinematic,
        Ragdoll,
        OFF
    }
    /// <summary>
    /// The current state of this <see cref="CharacterMovementBody"/>.
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
    /// Whether the character is currently grounded.
    /// </summary>
    public bool Grounded => jumpState == JumpState.Grounded;
    /// <summary>
    /// The current jump state of the character.
    /// </summary>
    public JumpState JumpState => jumpState;

    /// <summary>
    /// The current jump state of this body.
    /// </summary>
    JumpState jumpState = JumpState.Grounded;

    public void ReturnToNeutral(bool doCrossFade = true)
    {
        if (GroundCheck(out _))
        {
            Player.StateMachine.IdleWalk.Enter();
            if (doCrossFade) Player.Animator.CrossFade("GroundBasic", .1f);
        }
        else Player.StateMachine.Airborne.Enter();
    }


    #endregion States


    #region Other

    public static System.Action MovingUpdateAction;
    private Timer.Loop _movingUpdateActionTimer = new(0.2f);


    private VolcanicVent _currentVent;

    public VolcanicVent currentVent
    {
        get => _currentVent;
        set
        {
            _currentVent = value;
            Player.StateMachine.SendSignal(new(value != null ? "EnterVent" : "ExitVent", 0, true));
        }
    }
    public bool isOverVent => _currentVent != null;


    #endregion Other


    //DEBUG
#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (!DebugRR.DebugTextOverlay.Visible) return;

        foreach (HitNormalDisplay item in queuedHits) Debug.DrawRay(item.position, item.normal / 10);
        foreach (Vector3 item in jumpMarkers) Handles.DrawWireDisc(item, Vector3.up, 0.5f);
        foreach (var sweep in sweepsThisPhysUpdate)
        {
            Color color = sweep.hit ? Color.green : Color.red;
            Color colorE = color.SetAlpha(.5f);
            Vector3 height = Vector3.up * Collider.height / 2;

            DrawWireCapsule(sweep.origin + height, Quaternion.identity, Collider.radius, Collider.height, color);
            DrawWireCapsule(sweep.origin + height + (sweep.hit ? sweep.direction.normalized * sweep.hitDistance : sweep.direction),
                Quaternion.identity, Collider.radius, Collider.height, colorE);
            if (sweep.hit)
            {
                Vector3 start = sweep.origin + (sweep.direction.normalized * sweep.hitDistance);
                Vector3 end = start + sweep.hitNormal;
                Gizmos.DrawLine(start, end);
            }
        }
    }

    private List<HitNormalDisplay> queuedHits = new();
    private void AddToQueuedHits(HitNormalDisplay hit)
    {
        queuedHits.Add(hit);
        if (queuedHits.Count > 100) queuedHits.RemoveAt(0);
    }

    public List<Vector3> jumpMarkers = new();

    private struct HitNormalDisplay
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
    private struct SweepTestDisplay
    {
        public Vector3 origin;
        public Vector3 direction;
        public bool hit;
        public float hitDistance;
        public Vector3 hitNormal;
    }
    private List<SweepTestDisplay> sweepsThisPhysUpdate = new();

    public static void DrawWireCapsule(Vector3 _pos, Quaternion _rot, float _radius, float _height, Color _color = default(Color))
    {
        if (_color != default(Color))
            Handles.color = _color;
        Matrix4x4 angleMatrix = Matrix4x4.TRS(_pos, _rot, Handles.matrix.lossyScale);
        using (new Handles.DrawingScope(angleMatrix))
        {
            var pointOffset = (_height - (_radius * 2)) / 2;

            //draw sideways
            Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.left, Vector3.back, -180, _radius);
            Handles.DrawLine(new Vector3(0, pointOffset, -_radius), new Vector3(0, -pointOffset, -_radius));
            Handles.DrawLine(new Vector3(0, pointOffset, _radius), new Vector3(0, -pointOffset, _radius));
            Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.left, Vector3.back, 180, _radius);
            //draw frontways
            Handles.DrawWireArc(Vector3.up * pointOffset, Vector3.back, Vector3.left, 180, _radius);
            Handles.DrawLine(new Vector3(-_radius, pointOffset, 0), new Vector3(-_radius, -pointOffset, 0));
            Handles.DrawLine(new Vector3(_radius, pointOffset, 0), new Vector3(_radius, -pointOffset, 0));
            Handles.DrawWireArc(Vector3.down * pointOffset, Vector3.back, Vector3.left, -180, _radius);
            //draw center
            Handles.DrawWireDisc(Vector3.up * pointOffset, Vector3.up, _radius);
            Handles.DrawWireDisc(Vector3.down * pointOffset, Vector3.up, _radius);

        }
    }




#endif

}

/*
 PLAN FOR FIXING PROBLEM.
 
 Re-consolidate all functionality called by Move() to be within Move().
 (WHILE: making comment notes to denote each step for later organization.)
 
 !!!!! Consider making "CollideAndSlide" CLASS! With Methods for each step that could be overridden.
 
 Locate the Grounded "Move Forward" step and create a check that acts as if the Player is at the destination, and checks for ground below.

 */