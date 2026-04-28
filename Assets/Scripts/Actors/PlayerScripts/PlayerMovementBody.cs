using System;
using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;
using Utilities.Singletons;
using Utilities.Xtensions.Unity;
using Utilities.Xtensions;

#if UNITY_EDITOR
using UnityEditor.UIElements;
using UnityEditor;
using System.Reflection;
#endif


[RequireComponent(typeof(Rigidbody), typeof(CapsuleCollider), typeof(NavMeshAgent))]
public sealed class PlayerMovementBody : MonoBehaviour
{
    #region Config

    /// <summary>
    /// The Rigidbody component attached to this <see cref="CharacterMovementBody"/>.
    /// </summary>
    [field: SerializeField, RelatedComponent(true)] public Rigidbody RB { get; private set; }
    /// <summary>
    /// The <see cref="CapsuleCollider"/> component attached to this <see cref="CharacterMovementBody"/>.
    /// </summary>
    [field: SerializeField, RelatedComponent(true)] public CapsuleCollider Collider { get; private set; }
    /// <summary>
    /// The <see cref="CapsuleCollider"/> component attached to this <see cref="CharacterMovementBody"/>.
    /// </summary>
    [field: SerializeField, RelatedComponent(true)] public NavMeshAgent NavAgent { get; private set; }
    /// <summary>
    /// The default gravity vector for this <see cref="CharacterMovementBody"/>.
    /// </summary>
    [SerializeField] Vector3 defaultGravity = new(0, 1, 0);
    /// <summary>
    /// Whether Gravity should be automaticall applied or applied by some behavior
    /// </summary>
    [SerializeField] bool autoApplyGravity = false;
    /// <summary>
    /// The maximum angle (in degrees) of a slope this <see cref="CharacterMovementBody"/> can stand on.
    /// </summary>
    [SerializeField] float maxSlopeNormalAngle = 45f;
    /// <summary>
    /// The buffer used to check for ground.
    /// </summary>
    [SerializeField] float groundCheckBuffer = 0.1f;
    /// <summary>
    /// The number of steps used in the Collide & Slide Algorithm.
    /// </summary>
    [SerializeField] int movementProjectionSteps = 5;
    /// <summary>
    /// Angle threshold for Bonking.
    /// </summary>
    [SerializeField] float bonkThreshold = 15;

    /// <summary>
    /// The default offset for a Front-ways collision Check.
    /// </summary>
    [SerializeField] Vector3 frontCheckDefaultOffset;
    /// <summary>
    /// the default radius for a Front-ways collision Check.
    /// </summary>
    [SerializeField] float frontCheckDefaultRadius;

    //public PlatformDetectionMethod platformDetection;
    //[Tooltip("An unconventional means of hopefully avoiding falling through the floor. If true, the player will check if there is any ground below them before moving, and if not, their velocity will be set to 0 to prevent them from moving further. This is jank, but it might help with some edge cases and it doesn't require any extra components or setup.")]
    //public PlatformDetectionMethod mario64StyleAntiVoid;
    /// <summary>
    /// The LayerMask used for ground checks. Should be set to anything that can be stood on.
    /// </summary>
    [SerializeField] LayerMask validGroundMask;

    [SerializeField] float platformDetectionFactor = 3;
    [SerializeField] float platformLockRadius = .25f;
    [SerializeField, InspectorName("Use Nav Mesh")] bool _useNavMeshIfPossible = true;
    [SerializeField] bool lockToNavMesh = false;

    internal bool canDoDoubleJump = true;

    #endregion

    #region LifeCycle

    void Reset()
    {
        RB = GetComponent<Rigidbody>();
        Collider = GetComponent<CapsuleCollider>();
        NavAgent = GetComponent<NavMeshAgent>();
    }

    void Awake()
    {
        if (RB == null) RB = GetComponent<Rigidbody>();
        if (Collider == null) Collider = GetComponent<CapsuleCollider>();

        if (InstantSnapToFloor(out RaycastHit hit)) Land(hit);

        Singleton.Register(ref instance, this);
        DirectionGet = Vector3.forward;

        NavAgent.enabled = false;
        //NavAgent.updateRotation = false;
        NavAgent.updateUpAxis = false;
        gravity = defaultGravity.y;
        velocity = new(transform);
    }

    /// <summary>
    /// Called when the component is enabled.
    /// </summary>
    void OnEnable() { if (_rbState == BodyStates.OFF) BodyState = BodyStates.Enabled; }
    /// <summary>
    /// Called when the component is disabled.
    /// </summary>
    void OnDisable() => BodyState = BodyStates.OFF;

    void OnDestroy() => Singleton.Deregister(ref instance, this);

    #region Singleton Stuff
    static PlayerMovementBody instance;
    public static PlayerMovementBody Get => Singleton.Get(ref instance);
    public static bool TryGet(out PlayerMovementBody result) => Singleton.TryGet(Get, out result);
    public static bool Loaded => instance != null;
    #endregion

    /// <summary>
    /// The possible states for a <see cref="CharacterMovementBody"/>.
    /// </summary>
    public enum BodyStates
    {
        Enabled,
        Ragdoll,
        OFF
    }

    /// <summary>
    /// The current state of this <see cref="CharacterMovementBody"/>.
    /// </summary>
    public BodyStates BodyState
    {
        get => _rbState;
        set
        {
            _rbState = value;
            switch (value)
            {
                case BodyStates.Enabled:
                    RB.isKinematic = false;
                    RB.detectCollisions = true;
                    RB.useGravity = false;
                    Collider.enabled = true;
                    break;
                case BodyStates.Ragdoll:
                    RB.isKinematic = false;
                    RB.detectCollisions = true;
                    RB.useGravity = true;
                    Collider.enabled = false;
                    break;
                case BodyStates.OFF:
                    RB.isKinematic = true;
                    RB.detectCollisions = false;
                    RB.useGravity = false;
                    Collider.enabled = false;
                    break;
            }
        }
    }
    BodyStates _rbState = BodyStates.Enabled;



    public void ReturnToNeutral(bool doCrossFade = true)
    {
        if (GroundCheck(out _))
        {
            Player.StateMachine.IdleWalk.State.Enter();
            if (doCrossFade) Player.Animator.CrossFade("GroundBasic", .1f);
        }
        else Player.StateMachine.Airborne.Enter();
    }


    #endregion LifeCycle

    #region Move Cycle

    void FixedUpdate()
    {
        sweepsThisPhysUpdate.Clear();
        Player.Animator.SetFloat("CurrentSpeed", velocity.magnitudeH);
        if (Upgrades.Active.d_moonJump && Input.Jump.IsPressed()) velocity.u = 10f;

        //NavAgent.nextPosition = Position;

        Vector3 prePos = Position;

        DebugRR.DebugTextOverlay.SetText($"PMB : Velocity: {velocity}");

        if (BodyState != BodyStates.Enabled) return;

        RB.linearVelocity = Vector3.zero;
        RB.angularVelocity = Vector3.zero;

        if (navDestinationDriven)
        {
            DirectionSet(NavAgent.desiredVelocity, NavAgent.angularSpeed * Time.fixedDeltaTime);
            NavAgent.velocity = Vector3.zero;

            velocity.Global = (Vector3.Dot(NavAgent.desiredVelocity, DirectionGet) + 1) * NavAgent.desiredVelocity.magnitude * DirectionGet;
            if (NavAgent.remainingDistance < 0.1f) NavDestination(false);
        }

        if (velocity.r != 0f) Rotation = new(Rotation.x, Rotation.y + (velocity.r * Time.fixedDeltaTime), Rotation.z);

        stepZeroVelocity = velocity.Global * Time.fixedDeltaTime;
        stepZeroAnchor = anchorPoint;

        SetupDebugText(false);

        if (stepZeroVelocity != Vector3.zero) MoveNext(stepZeroVelocity);

        SetupDebugText(true);

        if (velocity.y <= 0)
        {
            if (GroundCheck(out AnchorPoint groundHit))
            {
                if (!isGrounded)
                {
                    Land(groundHit);
                    velocity.y = 0;
                }
            }
            else if (isGrounded)
            {
                AddDebugText("Walk Off.");
                Player.StateMachine.SendSignal("WalkOff");
                UnLand(JumpState.Hangtime);
            }
        }

        if (autoApplyGravity && !isGrounded) ApplyGravity();

        if (prePos != Position) _movingUpdateActionTimer.Tick(MovingUpdateAction);
    }

    /// <summary>
    /// Moves body via Nav Mesh.
    /// </summary>
    void MoveNav(Vector3 stepVelocity, int step = 0)
    {
        AddDebugText($"Step (Nav) {step}: {stepVelocity}");

        if (stepVelocity == Vector3.zero) return;

        if (!OnNavMesh)
        {
            AddDebugText($"Not sure why this is happening but MoveNav was somehow called despite not being on the NavMesh.");
            MoveSlide(stepVelocity, step);
            return;
        }

        if (!NavAgent.Raycast(Position + stepVelocity, out NavMeshHit hit))
        {
            AddDebugText("No hit, moving forward and ending loop.");
            NavAgent.Move(stepVelocity);
        }
        else
        {
            AddDebugText($"Hit NavMesh edge, normal {hit.normal} at position {hit.position}");
            Vector3 snapToSurface = stepVelocity.normalized * hit.distance;

            NavAgent.Move(snapToSurface);

            if (++step >= movementProjectionSteps) return;

            Vector3 leftover = stepVelocity - snapToSurface;
            if (lockToNavMesh) leftover = leftover.ProjectAndScale(hit.normal);
            else OnNavMesh = false;

            MoveNext(leftover, step);
        }

    }
    /// <summary>
    /// Moves body via Collide And Slide Algorithm.
    /// </summary>
    void MoveSlide(Vector3 stepVelocity, int step = 0)
    {
        AddDebugText($"Step (Slide) {step}: {stepVelocity}");

        if (stepVelocity == Vector3.zero) return;

        if (isGrounded) stepVelocity.y = 0;
        stepVelocity = stepVelocity.ProjectAndScale(anchorPoint.normal);

        float stopDistance = -1;
        Vector3 nextNormal = Vector3.zero;
        bool scaleByDot = false;
        bool negateVerticalLefover = false;

        // Sweep for any obstacle in the trajectory (ignore flat-floor hits when moving purely horizontally).
        // Sweep for any obstacle in the trajectory (ignore flat-floor hits when moving purely horizontally).
        bool sweepHit = SweepBody(stepVelocity, out RaycastHit hit, groundCheckBuffer) && !(stepVelocity.y == 0 && hit.normal == Vector3.up);

        if (!sweepHit)
        {
            AddDebugText("Grounded, Hit Nothing.");

            // Keep platform lock behavior for grounded movement (attempt to detect unreachable edges and snap behavior).
            // Keep platform lock behavior for grounded movement (attempt to detect unreachable edges and snap behavior).
            if (lockToNavMesh)
            {
                Vector3 platformCheckDistance = stepVelocity.normalized * platformDetectionFactor;

                if (!SweepBody(Vector3.down * groundCheckBuffer, out RaycastHit platformCheckHit,
                    groundCheckBuffer, Position + platformCheckDistance))
                {
                    Vector3 reachAroundPos = Position + (platformCheckDistance * 1.01f) - (Vector3.up * Collider.height / 2);
                    if (SweepBody(platformCheckDistance.XZ() * -2f, out RaycastHit reachAroundResult, 0, reachAroundPos))
                    {
                        nextNormal = -reachAroundResult.normal.XZ();
                        Plane P = new(nextNormal, reachAroundResult.point + (nextNormal * .6f));
                        P.Raycast(new(Position, stepVelocity), out float hitDistance);
                        if (hitDistance <= stepVelocity.magnitude) stopDistance = hitDistance;

                        scaleByDot = true;
                        AddDebugText($"Platform Locked onto non-NavMesh Platform, nextNormal: {nextNormal}");
                    }
                    else AddDebugText("Walking off platform when not allowed but reach around check failed. Failsafe situation, report to CJ.");
                }
            }


            if (stopDistance == -1)
            {
                // Snap down to a slightly lower ground if detected (small ledge correction).
                if (GroundCheck(out _, out RaycastHit groundCast, true) && groundCast.normal != anchorPoint.normal)
                {
                    Ray cornerCheckRay = new(groundCast.barycentricCoordinate + new Vector3(0, .1f, 0), Vector3.down);
                    bool different = groundCast.collider.Raycast(cornerCheckRay, out RaycastHit baryHit, .11f)
                        && baryHit.normal != groundCast.normal;

                    if (groundCast.distance >= float.Epsilon && groundCast.distance <= groundCheckBuffer && !different)
                    {
                        AddDebugText("Snapping to lowerGround.");
                        Position += Vector3.down * groundCast.distance;
                        anchorPoint = groundCast;
                    }
                }
            }
        }
        else // grounded sweep hit handling
        {
            AddDebugText($"Grounded, Hit: {hit.normal} at distance {hit.distance}");
            stopDistance = hit.distance;
            nextNormal = hit.normal;

            if (Mathf.Approximately(hit.normal.y, 0))
            {
                AddDebugText("Hit a wall.");
                scaleByDot = true;
                negateVerticalLefover = true;
                negateVerticalLefover = true;
                nextNormal = nextNormal.XZ().normalized;
            }
            else if (hit.normal.y > 0 && !WithinSlopeAngle(hit.normal))
            {
                AddDebugText("Hit a steep slope.");
                scaleByDot = true;
                negateVerticalLefover = true;
                negateVerticalLefover = true;
                nextNormal = nextNormal.XZ().normalized;
            }

            if (anchorPoint.normal.y > 0 && hit.normal.y < 0) FloorCeilingLock(anchorPoint.normal, hit.normal);
            else if (anchorPoint.normal.y < 0 && hit.normal.y > 0) FloorCeilingLock(hit.normal, anchorPoint.normal);
            if (anchorPoint.normal.y > 0 && hit.normal.y < 0) FloorCeilingLock(anchorPoint.normal, hit.normal);
            else if (anchorPoint.normal.y < 0 && hit.normal.y > 0) FloorCeilingLock(hit.normal, anchorPoint.normal);

            void FloorCeilingLock(Vector3 floorNormal, Vector3 ceilingNormal)
            {
                AddDebugText("Encountered Vertical Squish.");
                scaleByDot = true;
                nextNormal = floorNormal.y != floorNormal.magnitude ? floorNormal : ceilingNormal;
            }

            if (hit.normal.y > 0 && WithinSlopeAngle(hit.normal) && stepVelocity.y <= 0) anchorPoint = hit;
        }

        Vector3 snapToSurface = stopDistance != -1 ? stepVelocity.normalized * stopDistance : stepVelocity;

        // Make sure we aren't moving off into the void at the destination
        if (!SweepBody(Vector3.down * 5000, out _, groundCheckBuffer, snapToSurface, QueryTriggerInteraction.Collide)) return;

        Position += snapToSurface;

        if (stopDistance < 0 || ++step >= movementProjectionSteps) return;
        else if (Vector3.Angle(stepVelocity.XZ(), -nextNormal.XZ()) < bonkThreshold && Player.StateMachine.SendSignal(new("Bonk", 0, true)))
        {
            this.velocity.Global = Vector3.zero;
            return;
        }

        Vector3 leftover = stepVelocity - snapToSurface;
        if (negateVerticalLefover)
        {
            leftover.y = 0;
            Land(hit);
        }
        Vector3 newDir = leftover.ProjectAndScale(nextNormal);
        if (scaleByDot) newDir *= Vector3.Dot(leftover.normalized, nextNormal) + 1;

        MoveNext(newDir, step);
    }
    /// <summary>
    /// Moves body in mid-air via a simplified airborne-focused Collide-And-Slide algorithm.
    /// </summary>
    void MoveAir(Vector3 stepVelocity, int step = 0)
    {
        AddDebugText($"Step (Slide) {step}: {stepVelocity}");

        if (isGrounded) stepVelocity.y = 0;
        if (stepVelocity == Vector3.zero) return;

        // Air-specific: do NOT project movement to anchor normal (we are airborne).
        float stopDistance = -1;
        Vector3 nextNormal = Vector3.zero;
        bool land = false;

        bool sweepHit = SweepBody(stepVelocity, out RaycastHit hit, groundCheckBuffer);

        if (sweepHit)
        {
            AddDebugText($"Airborne, Hit: {hit.normal} at distance {hit.distance}");
            stopDistance = hit.distance;
            nextNormal = hit.normal;

            if (Mathf.Approximately(hit.normal.y, 0)) AddDebugText("Hit a Wall mid-air.");
            else if (hit.normal.y > 0)
            {
                if (WithinSlopeAngle(hit.normal))
                {
                    AddDebugText("Landed on a standable ground.");
                    land = true;
                }
                else AddDebugText("Hit a steep slope while falling.");
            }
            else if (!WithinSlopeAngle(-hit.normal))
            {
                AddDebugText("Hit a sloped ceiling while jumping.");
            }
            else
            {
                AddDebugText("Hit a ceiling while jumping.");
                land = true;
                velocity.y = -0.1f;
                UnLand(JumpState.Falling);
            }

            if (hit.normal.y > 0 && WithinSlopeAngle(hit.normal) && stepVelocity.y <= 0) anchorPoint = hit;
        }
        else
        {
            AddDebugText("Airborne, Hit Nothing.");

            // If airborne and there is no surface below the destination, treat as void and stop forward motion.
            if (!SweepBody(Vector3.down * 5000, out _, 0, Position + stepVelocity))
            {
                AddDebugText("Hit the void while falling.");
                stopDistance = 0;
                nextNormal = -stepVelocity.XZ();
            }
        }

        Vector3 snapToSurface = stopDistance != -1 ? stepVelocity.normalized * stopDistance : stepVelocity;

        // Void check for the destination
        if (!SweepBody(Vector3.down * 5000, out _, groundCheckBuffer, snapToSurface, QueryTriggerInteraction.Collide)) return;

        Position += snapToSurface;

        if (stopDistance < 0 || ++step >= movementProjectionSteps) return;
        else if (Vector3.Angle(stepVelocity.XZ(), -nextNormal.XZ()) < bonkThreshold && Player.StateMachine.SendSignal(new("Bonk", 0, true)))
        {
            this.velocity.Global = Vector3.zero;
            return;
        }

        Vector3 leftover = stepVelocity - snapToSurface;
        if (land)
        {
            leftover.y = 0;
            Land(hit);
        }
        Vector3 newDir = leftover.ProjectAndScale(nextNormal);

        MoveNext(newDir, step);
    }

    /// <summary>
    /// Moves body via either the Nav Mesh, if appropriate, or via the Collide and Slide Algorithm.
    /// </summary>
    void MoveNext(Vector3 stepVelocity, int step = 0)
    {
        if (OnNavMesh) MoveNav(stepVelocity, step);
        else if (isGrounded) MoveSlide(stepVelocity, step);
        else MoveAir(stepVelocity, step);
    }


    /// <summary>
    /// The (Projected) velocity used in the very first physics step, kept for reference during later steps of Collide and Slide.
    /// </summary>
    Vector3 stepZeroVelocity;
    /// <summary>
    /// (Currently unimplemented) A separate velocity to handle the "Floating Collider" spring forces.
    /// </summary>
    Vector3 springVelocity;
    /// <summary>
    /// The AnchorPoint used in the very first physics step, kept for reference during later steps of Collide and Slide.
    /// </summary>
    AnchorPoint stepZeroAnchor;


    #endregion Move Cycle

    #region Position

    /// <summary>
    /// Gets or sets the position of the character.
    /// </summary>
    public Vector3 Position
    {
        get => BodyState == BodyStates.Enabled
            ? OnNavMesh
                ? NavAgent.nextPosition
                : RB.position
            : transform.position;
        set
        {
            if (BodyState != BodyStates.Enabled) return;

            if (OnNavMesh) NavAgent.nextPosition = value;
            else RB.MovePosition(value);
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

    /// <summary>
    /// The active forward direction of this <see cref="PlayerMovementBody"/>. Simpler controllers can probably avoid using this.
    /// </summary>
    public Vector3 DirectionGet { get; private set; }

    /// <summary>
    /// Sets the active forward direction of this <see cref="PlayerMovementBody"/>. 
    /// </summary>
    public void DirectionSet(Vector3 target, float maxTurnDegrees)
    {
        if (target == Vector3.zero) return;
        Vector3 res = Vector3.RotateTowards(DirectionGet, target.normalized, maxTurnDegrees * Mathf.PI, 1);
        DirectionSet(res);
    }
    /// <summary>
    /// Sets the active forward direction of this <see cref="PlayerMovementBody"/> to the direction the player's thumbstick is going in.
    /// </summary>
    public void DirectionSet(float maxTurnDegrees) => DirectionSet(Player.Controller.camAdjustedMovement, maxTurnDegrees);
    /// <summary>
    /// Instantly Sets the active forward direction of this <see cref="PlayerMovementBody"/>.
    /// </summary>
    public void DirectionSet(Vector3 target)
    {
        if (DirectionGet == target || target == Vector3.zero) return;
        DirectionGet = target;
        RotationQ = Quaternion.LookRotation(target, Vector3.up);
        velocity.CallThisPostRotation();
    }

    /// <summary>
    /// Gets or sets the rotation of the Rigidbody as a Quaternion.
    /// </summary>
    public Quaternion RotationQ
    {
        get => RB.rotation;
        set
        {
            RB.rotation = value;
            velocity.CallThisPostRotation();
        }
    }
    /// <summary>
    /// Gets or sets the rotation of the character in Euler angles.
    /// </summary>
    public Vector3 Rotation
    {
        get => transform.eulerAngles;
        set
        {
            transform.eulerAngles = value;
            velocity.CallThisPostRotation();
        }
    }

    public void QuickTurnTime(Vector3 target, float lengthSeconds)
    {
        target = target.XZ(); //Ensure no weird rotations

        if (lengthSeconds <= 0f)
        {
            DirectionGet = target;
            return;
        }

        QuickTurnRoutine = Enum().Begin(Player.MovementBody);
        IEnumerator Enum()
        {
            float deltaRad = Vector3.Angle(DirectionGet, target) * Mathf.Deg2Rad;
            float rateRadPerSec = deltaRad / lengthSeconds; // radians per second

            while (deltaRad > 0f)
            {
                DirectionGet = Vector3.RotateTowards(DirectionGet, target, rateRadPerSec * Time.fixedDeltaTime, 0f);
                yield return new WaitForFixedUpdate();
                deltaRad -= rateRadPerSec * Time.fixedDeltaTime;
            }
            DirectionGet = target;
        }
    }
    public void QuickTurnLimited(Vector3 target, float maxDelta)
    {
        target = target.XZ(); //Ensure no weird rotations
        if (maxDelta <= 0f) return;

        QuickTurnRoutine = Enum().Begin(Player.MovementBody);
        IEnumerator Enum()
        {
            float fullDelta = Vector3.Angle(DirectionGet, target) * Mathf.Deg2Rad;

            while (fullDelta > 0f)
            {
                DirectionGet = Vector3.RotateTowards(DirectionGet, target, maxDelta * Time.fixedDeltaTime, 0f);
                yield return null;
                fullDelta -= maxDelta * Time.fixedDeltaTime;
            }

            DirectionGet = target;
        }
    }
    private Coroutine QuickTurnRoutine;




    #endregion Direction

    #region Velocity

    /// <summary>
    /// Custom Velocity handler that has velocity in both Global and Local spaces.
    /// </summary>
    public class BodyVelocityState
    {
        public BodyVelocityState(Transform This) => this.transform = This;

        public Transform transform;

        /// <summary>
        /// Forward Velocity
        /// <br/> Setting this will rebuild the x and z values to match the new forward velocity.
        /// </summary>
        public float f
        {
            get => fValue;
            set
            {
                fValue = value;
                if(!allowBackwards && value < 0) fValue = 0;
                Vector3 global = transform.TransformVector(Local);
                zValue = global.z;
                xValue = global.x;
            }
        }
        float fValue;
        /// <summary>
        /// Upward Velocity (Identical to y)
        /// </summary>
        public float u { get => yValue; set => yValue = value; }
        /// <summary>
        /// Sideways Velocity
        /// <br/> Setting this will rebuild the x and z values to match the new sideways velocity.
        /// </summary>
        public float s
        {
            get => sValue;
            set
            {
                sValue = value;
                Vector3 global = transform.TransformVector(Local);
                zValue = global.z;
                xValue = global.x;
            }
        }
        float sValue;

        /// <summary>
        /// Velocity on the X global direction
        /// <br/> Setting this will rebuild the f and s values to match the new x velocity.
        /// </summary>
        public float x
        {
            get => xValue;
            set
            {
                xValue = value;
                Vector3 local = transform.InverseTransformVector(Global);
                fValue = local.z;
                sValue = local.x;
            }
        }
        float xValue;
        /// <summary>
        /// Velocity on the Y global direction (Identical to u)
        /// </summary>
        public float y
        {
            get => yValue; set => yValue = value;
        }
        float yValue;
        /// <summary>
        /// Velocity on the Z global direction
        /// <br/> Setting this will rebuild the f and s values to match the new z velocity.
        /// </summary>
        public float z
        {
            get => zValue;
            set
            {
                zValue = value;
                Vector3 local = transform.InverseTransformVector(Global);
                fValue = local.z;
                sValue = local.x;
            }
        }
        float zValue;



        public Vector3 Global
        {
            get => new(xValue, yValue, zValue);
            set
            {
                xValue = value.x;
                yValue = value.y;
                zValue = value.z;

                Vector3 local = transform.InverseTransformVector(Global);
                fValue = local.z;
                sValue = local.x;
            }
        }
        public Vector3 Local
        {
            get => new(sValue, yValue, fValue);
            set
            {
                sValue = value.x;
                yValue = value.y;
                fValue = value.z;

                Vector3 global = transform.TransformVector(Local);
                zValue = global.z;
                xValue = global.x;
            }
        }

        /// <summary>
        /// Rotational Velocity, clockwise on the Y/U axis.
        /// </summary>
        public float r
        {
            get => rValue;
            set => rValue = value;
        }
        float rValue;
        /// <summary>
        /// How much Local Velocity is carried over upon rotation. 0-1
        /// </summary>
        public float cL
        {
            get => cLValue;
            set
            {
                cLValue = Mathf.Clamp01(value);
                if (cLValue + cGValue > 1) cGValue = 1 - cLValue;
            }
        }
        float cLValue = 1f;
        /// <summary>
        /// How much Global Velocity is carried over upon rotation. 0-1
        /// </summary>
        public float cG
        {
            get => cGValue;
            set
            {
                cLValue = Mathf.Clamp01(value);
                if (cLValue + cGValue > 1) cGValue = 1 - cLValue;
            }
        }
        float cGValue;

        public bool allowBackwards = true;

        public void CallThisPostRotation()
        {
            if (cLValue == 0 && cGValue == 0) { xValue = 0; zValue = 0; fValue = 0; sValue = 0; return; }

            Vector3 adjustedGlobalValues = transform.InverseTransformVector(Global);

            float fFinal = (fValue * cLValue) + (adjustedGlobalValues.z * cGValue),
                  sFinal = (sValue * cLValue) + (adjustedGlobalValues.x * cGValue);

            Vector3 finalGlobalValues = transform.TransformVector(new(sFinal, 0, fFinal));

            fValue = fFinal;
            sValue = sFinal;
            xValue = finalGlobalValues.x;
            zValue = finalGlobalValues.z;
        }

        public void Zero(bool horizontal = true, bool vertical = true, bool rotational = true)
        {
            if (horizontal)
            {
                fValue = 0;
                sValue = 0;
                xValue = 0;
                zValue = 0;
            }
            if (vertical) yValue = 0;
            if (rotational) rValue = 0;
        }

        public float magnitudeH =>
            sValue != 0 ? MathF.Sqrt((fValue * fValue) + (sValue * sValue))
            : fValue;
        public float sqrMagnitudeH =>
            sValue != 0 ? (fValue * fValue) + (sValue * sValue)
            : fValue * fValue;
        public float magnitude =>
            fValue != 0 && sValue != 0 && yValue != 0         ? Mathf.Sqrt((fValue * fValue) + (sValue * sValue) + (yValue * yValue)) //All 3 NonZero
                  : fValue != 0 && sValue == 0 && yValue == 0 ? Mathf.Sqrt(fValue * fValue) //Only F
                  : fValue == 0 && sValue == 0 && yValue != 0 ? Mathf.Sqrt(yValue * yValue) //Only Y
                  : fValue == 0 && sValue != 0 && yValue == 0 ? Mathf.Sqrt(sValue * sValue) //Only S
                  : fValue != 0 && sValue == 0 && yValue != 0 ? Mathf.Sqrt((fValue * fValue) + (yValue * yValue)) // F+Y
                  : fValue != 0 && sValue != 0 && yValue == 0 ? Mathf.Sqrt((fValue * fValue) + (sValue * sValue)) // F+S
                  : fValue == 0 && sValue != 0 && yValue != 0 ? Mathf.Sqrt((sValue * sValue) + (yValue * yValue)) // S+Y
            : 0; //All 3 Zero

        public float sqrMagnitude =>
            fValue != 0 && sValue != 0 && yValue != 0         ? (fValue * fValue) + (sValue * sValue) + (yValue * yValue) //All 3 NonZero
                  : fValue != 0 && sValue == 0 && yValue == 0 ? fValue * fValue //Only F
                  : fValue == 0 && sValue == 0 && yValue != 0 ? yValue * yValue //Only Y
                  : fValue == 0 && sValue != 0 && yValue == 0 ? sValue * sValue //Only S
                  : fValue != 0 && sValue == 0 && yValue != 0 ? (fValue * fValue) + (yValue * yValue) // F+Y
                  : fValue != 0 && sValue != 0 && yValue == 0 ? (fValue * fValue) + (sValue * sValue) // F+S
                  : fValue == 0 && sValue != 0 && yValue != 0 ? (sValue * sValue) + (yValue * yValue) // S+Y
            : 0; //All 3 Zero
    }

    /// <summary>
    /// Specialzed Doubly-Direction Velocity Property. 
    /// <br/>Contains Local and Global velocities and automatically adjusts the other when one is changed.
    /// <br/>Members: x-y-z f-u-s r-cL-cG 
    /// </summary>
    public BodyVelocityState velocity { get; private set; }

    public float movementModifier = 1;

    [HideInInspector] public bool baseMovability = true;
    [HideInInspector] public bool canJump = true;


    #endregion Velocity

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


    private static readonly RaycastHit[] s_capsuleCastResults = new RaycastHit[32];

    public bool SweepBodyAlt(Vector3 offset, out RaycastHit hit,
        float buffer = 0, Vector3? tempOrigin = null, QueryTriggerInteraction queryTriggerInteraction = QueryTriggerInteraction.Ignore)
    {
        hit = default;

        // world-space origin for the capsule
        Vector3 originPos = tempOrigin ?? RB.position;
        Vector3 worldCenter = originPos + transform.rotation * Collider.center;

        // account for transform scale when computing radius and height
        Vector3 lossy = transform.lossyScale;
        float radius = Collider.radius * Mathf.Max(lossy.x, lossy.z);
        float height = Mathf.Max(Collider.height * lossy.y, radius * 2f); // ensure valid capsule
        float halfHeight = Mathf.Max(0f, (height / 2f) - radius);

        // capsule endpoints in world space
        Vector3 up = transform.up;
        Vector3 p1 = worldCenter + up * halfHeight;
        Vector3 p2 = worldCenter - up * halfHeight;

        Vector3 dir = offset.normalized;
        float maxDistance = offset.magnitude + buffer;

        // Build a layer mask that includes layers this object's layer can collide with
        // If we actually go down this road, change this to store the layerMask once at the beginning.
        int selfLayer = gameObject.layer;
        int layerMask = 0;
        for (int i = 0; i < 32; i++)
            if (!Physics.GetIgnoreLayerCollision(selfLayer, i))
                layerMask |= 1 << i;

        // Perform non-mutating capsule cast using the NonAlloc API
        int count = Physics.CapsuleCastNonAlloc(p1, p2, radius, dir, s_capsuleCastResults, maxDistance, layerMask, queryTriggerInteraction);
        if (count == 0)
        {
            sweepsThisPhysUpdate.Add(new()
            {
                origin = tempOrigin.GetValueOrDefault(),
                direction = offset,
                hit = false,
                hitDistance = 0,
                hitNormal = Vector3.zero
            });
            return false;
        }

        // Find nearest valid hit that is not this object's collider
        int nearestIndex = -1;
        float nearestDist = float.MaxValue;
        for (int i = 0; i < count; i++)
        {
            var h = s_capsuleCastResults[i];
            if (h.collider == null) continue;
            if (h.collider == Collider || h.collider.gameObject == gameObject) continue; // exclude self
            if (h.distance < nearestDist)
            {
                nearestDist = h.distance;
                nearestIndex = i;
            }
        }

        if (nearestIndex == -1)
        {
            sweepsThisPhysUpdate.Add(new()
            {
                origin = tempOrigin.GetValueOrDefault(),
                direction = offset,
                hit = false,
                hitDistance = 0,
                hitNormal = Vector3.zero
            });
            return false;
        }

        // copy and adjust distance for buffer, clamp >= 0
        RaycastHit chosen = s_capsuleCastResults[nearestIndex];
        chosen.distance = Mathf.Max(0f, chosen.distance - buffer);

        // record debug info
        sweepsThisPhysUpdate.Add(new()
        {
            origin = tempOrigin.GetValueOrDefault(),
            direction = offset,
            hit = true,
            hitDistance = chosen.distance,
            hitNormal = chosen.normal
        });

        hit = chosen;
        return true;
    }

    #endregion

    #region Ground

    /// <summary>
    /// The possible states of a jump.
    /// </summary>
    public enum JumpState
    {
        Grounded = 0,
        Jumping = 1,
        Decelerating = 2,
        Hangtime = 3,
        Falling = 4,
        TerminalVelocity = 5
    }


    /// <summary>
    /// Handles collision events with other objects.
    /// </summary>
    /// <param name="collision">The collision information.</param>
    void OnCollisionEnter(Collision collision)
    {
        Vector3 contactNormal = collision.GetContact(0).normal;
        if (!isGrounded && velocity.y > .1f && Vector3.Dot(contactNormal, Vector3.up) < -0.75f) velocity.y = 0;
        else if (!isGrounded && WithinSlopeAngle(contactNormal))
            Land(collision.GetContact(0));

    }

    public void Land(AnchorPoint groundHit)
    {
        bool wasntGrounded = _jumpState != JumpState.Grounded;
        bool objectChange = anchorPoint.collider != groundHit.collider;
        canDoDoubleJump = true;

        if (!wasntGrounded && !objectChange) return;

        _jumpState = JumpState.Grounded;
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

        OnNavMesh = true; //Attempt to bind to Nav Mesh.

    }
    /// <summary>
    /// Lands the body on the ground described by the AnchorPoint.
    /// </summary>
    /// <param name="groundHit">The anchor point of the ground hit.</param>
    public void Land()
    {
        if (!GroundCheck(out AnchorPoint groundHit)) return;
        Land(groundHit);
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
        _jumpState = newState;
        anchorPoint = AnchorPoint.Null;
        if (movingAnchor != null)
        {
            movingAnchor.SetPlayerInfluence(false);
            movingAnchor = null;
        }
        OnNavMesh = false;
    }

    void WalkOff()
    {
        UnLand();
        Player.StateMachine.SendSignal(new("WalkOff", ignoreLock: true));
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

    /// <summary>
    /// Whether the character is currently grounded.
    /// </summary>
    public bool isGrounded => _jumpState == JumpState.Grounded;
    /// <summary>
    /// The current jump state of the character.
    /// </summary>
    public JumpState JumpStateCurrent => _jumpState;

    /// <summary>
    /// The current jump state of this body.
    /// </summary>
    JumpState _jumpState = JumpState.Grounded;


    #endregion Ground

    #region NavMesh Navigation

    public bool UseNavMeshIfPossible
    {
        get => _useNavMeshIfPossible;
        set
        {
            _useNavMeshIfPossible = value;
            if (isGrounded)
            {
                if (NavMesh.SamplePosition(Position, out _, navMeshDetectionRange, NavAgent.areaMask))
                    OnNavMesh = true;
            }
            if (!value && OnNavMesh) OnNavMesh = false;
        }
    }
    public float navMeshDetectionRange = .35f;

    public bool OnNavMesh
    {
        get => UseNavMeshIfPossible && NavAgent.enabled && NavAgent.isOnNavMesh;
        set
        {
            // No-op if nothing changes (avoid repeated expensive ops)
            if (!UseNavMeshIfPossible) return;
            if (value == (NavAgent.enabled && NavAgent.isOnNavMesh)) return;

            if (value)
            {
                // Try to place the agent cleanly on the navmesh surface before enabling.
                Vector3 desiredAgentSurfacePos = Position - Vector3.up * NavAgent.baseOffset;
                if (NavMesh.SamplePosition(desiredAgentSurfacePos, out NavMeshHit sampleHit, navMeshDetectionRange, NavAgent.areaMask))
                {
                    // Place agent internal position on the navmesh
                    NavAgent.Warp(sampleHit.position);
                    NavAgent.nextPosition = sampleHit.position;
                    // Place the RB at the same surface + baseOffset so visuals/physics line up
                    ForceSetPosition(sampleHit.position + Vector3.up * NavAgent.baseOffset);

                    // We will manage character position ourselves (RB) and use NavAgent for pathfinding only.
                    NavAgent.enabled = true;
                }
                else
                {
                    // can't attach to navmesh right now
                    navDestinationDriven = false;
                    NavAgent.enabled = false;
                }
            }
            else
            {
                // Disable nav control immediately and clear nav-driven state to avoid stale Move calls.
                navDestinationDriven = false;
                NavAgent.enabled = false;
            }
        }
    }

    private bool navDestinationDriven = false;

    public float navMeshOffset
    {
        get => NavAgent.baseOffset;
        set => NavAgent.baseOffset = value;
    }

    /// <summary>
    /// Getter Variant, just returns current Destination.
    /// </summary>
    /// <returns>The current NavDestination, will be zero if there is none.</returns>
    public Vector3 NavDestination() => navDestinationDriven ? NavAgent.destination : Vector3.zero;

    /// <summary>
    /// Setter Value Variant. Sets Destination and activates Destination-driven behavior, if possible.
    /// </summary>
    /// <param name="value"></param>
    /// <returns>Success.</returns>
    public bool NavDestination(Vector3 value)
    {
        // If not attached, try to attach first (if allowed)
        if (!OnNavMesh)
        {
            if (!UseNavMeshIfPossible) return false;
            OnNavMesh = true;
            if (!OnNavMesh) return false;
        }

        navDestinationDriven = true;
        NavAgent.destination = value;
        return true;
    }
    /// <summary>
    /// Setter Activation Variant. Activates/Deactivates Destination-driven Behavior. Destination value is optional to allow False Setting.
    /// </summary>
    public bool NavDestination(bool value, Vector3 destinationValue = default)
    {
        if (value)
        {
            if (!OnNavMesh)
            {
                if (!UseNavMeshIfPossible) return false;
                OnNavMesh = true;
                if (!OnNavMesh) return false;
            }

            navDestinationDriven = true;
            NavAgent.destination = destinationValue;
            return true;
        }
        else
        {
            navDestinationDriven = false;
            NavAgent.ResetPath();
            // keep agent disabled? existing code leaves NavAgent.enabled as-is; we keep existing behavior
            return false;
        }
    }
    /// <summary>
    /// Getter Bool with Output Variant. Returns whether Destination-driven Behavior is active and outs the destination value.
    /// </summary>
    public bool NavDestination(out Vector3 result)
    {
        result = NavAgent.destination;
        return navDestinationDriven;
    }




    #endregion NavMesh Navigation

    #region Gravity

    /// <summary>
    /// Runs the calculations to automatically apply the current gravity to this body.
    /// </summary>
    public void ApplyGravity() => velocity.u -= gravity * Time.fixedDeltaTime;

    /// <summary>
    /// The active gravity value. (Inverted. y=1 is down.)
    /// </summary>
    private float gravity = 9.8f;

    /* 3D Gravity (Not Necessary for this project.)
    /// <summary>
    /// The active gravity value. (Inverted. y=1 is down.)
    /// </summary>
    [NonSerialized] private Vector3 gravity = new(0, 9.8f, 0);

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
    */


    #endregion Gravity

    #region Other

    public static System.Action MovingUpdateAction;
    private Timer.Loop _movingUpdateActionTimer = new(0.2f);

    public VolcanicVent CurrentVent
    {
        get => currentVent;
        set
        {
            currentVent = value;
            Player.StateMachine.SendSignal(new(value != null ? "EnterVent" : "ExitVent", 0, true));
        }
    }
    VolcanicVent currentVent;
    public bool isOverVent => currentVent != null;

    public float GroundCheckBuffer => groundCheckBuffer;


    #endregion Other


    #region DEBUG

    System.Text.StringBuilder moveTestString = new();

    private void SetupDebugText(bool post)
    {
#if UNITY_EDITOR
        if (!post) moveTestString.Clear();
        else DebugRR.DebugTextOverlay.SetText(moveTestString.ToString());
#endif
    }
    private void AddDebugText(string text)
    {
#if UNITY_EDITOR
        moveTestString.AppendLine(text);
#endif
    }

#if UNITY_EDITOR

    private void OnDrawGizmos()
    {
        if (!DebugRR.DebugTextOverlay.Visible) return;

        foreach (HitNormalDisplay item in queuedHits) Debug.DrawRay(item.position, item.normal / 10);
        foreach (Vector3 item in jumpMarkers) Handles.DrawWireDisc(item, Vector3.up, 0.5f);
        foreach (var sweep in sweepsThisPhysUpdate)
        {
            Color color = sweep.hit ? Color.green : Color.red;
            Color colorE = color.Changed(a: .5f); 
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

        if (NavMesh.FindClosestEdge(Position, out var hit, NavMesh.AllAreas))
            Debug.DrawRay(hit.position, hit.normal, Color.yellow);
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

    [CustomEditor(typeof(PlayerMovementBody))]
    public class Editor : UnityEditor.Editor
    {
        // Runtime name labels (cached)
        private Label _velNameLabel;
        private Label _speedNameLabel;
        private Label _jumpStateNameLabel;
        private Label _onNavMeshNameLabel;
        private Label _navDestNameLabel;
        private Label _gravityNameLabel;
        private Label _anchorNameLabel;
        private Label _directionNameLabel;
        private Label _currentVentNameLabel;

        // Runtime value labels (cached)
        private Label _velLabelA;
        private Label _velLabelB;
        private Label _jumpStateLabel;
        private Label _onNavMeshLabel;
        private Label _navDestLabel;
        private Label _gravityLabel;
        private Label _anchorLabel;
        private Label _directionLabel;
        private Label _currentVentLabel;

        private Button GoToOriginButton;

        // Row containers for layout and visibility toggles
        private VisualElement _navRow;

        private bool _subscribedToUpdate = false;

        public override VisualElement CreateInspectorGUI()
        {
            // Root tab view container (uses project's existing TabView/Tab types)
            TabView tabView = new();

            // Shortcut to serialized object
            var so = serializedObject;

            // -----------------------
            // Config Tab
            // -----------------------
            Tab configTab = new("Config");
            configTab.tabHeader.style.flexGrow = 1;

            // Helper to add property fields safely
            void AddProp(string propName, string label = null)
            {
                var prop = so.FindProperty(propName);
                if (prop != null)
                {
                    var pf = new PropertyField(prop, label ?? prop.displayName);
                    pf.Bind(so);
                    configTab.Add(pf);
                }
                else
                {
                    // fallback label so inspector isn't empty if names differ
                    configTab.Add(new Label($"Missing serialized property: {propName}"));
                }
            }

            // Add all relevant serialized/config fields present in PlayerMovementBody
            AddProp($"<{nameof(RB)}>k__BackingField", "Rigidbody");
            AddProp($"<{nameof(Collider)}>k__BackingField", "Collider");
            AddProp($"<{nameof(NavAgent)}>k__BackingField", "Nav Mesh Agent");
            AddProp(nameof(defaultGravity), "Default Gravity");
            AddProp(nameof(autoApplyGravity), "Auto Apply Gravity");
            AddProp(nameof(maxSlopeNormalAngle), "Max Slope Angle");
            AddProp(nameof(groundCheckBuffer), "Ground Check Buffer");
            AddProp(nameof(movementProjectionSteps), "Movement Projection Steps");
            AddProp(nameof(bonkThreshold), "Bonk Threshold");
            AddProp(nameof(frontCheckDefaultOffset), "Front Check Default Offset");
            AddProp(nameof(frontCheckDefaultRadius), "Front Check Default Radius");
            AddProp(nameof(validGroundMask), "Valid Ground Mask");
            AddProp(nameof(platformDetectionFactor), "Platform Detection Factor");
            AddProp(nameof(platformLockRadius), "Platform Lock Radius");
            AddProp(nameof(_useNavMeshIfPossible), "Use Nav Mesh If Possible");
            AddProp(nameof(lockToNavMesh), "Lock To Nav Mesh");
            AddProp(nameof(navMeshDetectionRange), "Nav Mesh Detection Range");

            tabView.Add(configTab);

            // -----------------------
            // Active Tab (runtime info)
            // -----------------------
            Tab activeTab = new("Active");
            activeTab.tabHeader.style.flexGrow = 1;

            // Informational label when not playing
            var notPlayingLabel = new Label("Runtime information shown here while in Play Mode.") { name = "runtime-info-label" };
            activeTab.Add(notPlayingLabel);

            // Container for runtime values
            var runtimeContainer = new VisualElement();
            runtimeContainer.style.flexDirection = FlexDirection.Column;
            runtimeContainer.style.paddingLeft = 4;
            runtimeContainer.style.paddingTop = 4;

            // Instantiate value labels
            _velLabelA = new Label();
            _velLabelB = new Label();
            _jumpStateLabel = new Label();
            _onNavMeshLabel = new Label();
            _navDestLabel = new Label();
            _gravityLabel = new Label();
            _anchorLabel = new Label();
            _directionLabel = new Label();
            _currentVentLabel = new Label();

            // Instantiate name labels
            _velNameLabel = new Label("Velocity:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _speedNameLabel = new Label("CurrentSpeed:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _jumpStateNameLabel = new Label("Jump State:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _onNavMeshNameLabel = new Label("On NavMesh:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _navDestNameLabel = new Label("Nav Destination:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _gravityNameLabel = new Label("Gravity (3D):") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _anchorNameLabel = new Label("Anchor:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _directionNameLabel = new Label("Direction:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };
            _currentVentNameLabel = new Label("Current Vent:") { style = { unityFontStyleAndWeight = FontStyle.Bold } };

            // Helper to create a horizontal row with name + value
            VisualElement CreateRow(Label nameLabel, Label valueLabel)
            {
                var row = new VisualElement();
                row.style.flexDirection = FlexDirection.Row;
                row.style.alignItems = Align.Center;
                // Reserve a consistent width for the name column for alignment
                nameLabel.style.minWidth = 150;
                nameLabel.style.marginRight = 6;
                valueLabel.style.unityTextAlign = TextAnchor.MiddleLeft;
                row.Add(nameLabel);
                row.Add(valueLabel);
                return row;
            }

            // Add rows
            runtimeContainer.Add(CreateRow(_velNameLabel, _velLabelA));
            runtimeContainer.Add(CreateRow(_velNameLabel, _velLabelB));
            runtimeContainer.Add(CreateRow(_jumpStateNameLabel, _jumpStateLabel));
            runtimeContainer.Add(CreateRow(_onNavMeshNameLabel, _onNavMeshLabel));

            // Nav row must be addressable for visibility toggling
            _navRow = CreateRow(_navDestNameLabel, _navDestLabel);
            runtimeContainer.Add(_navRow);

            runtimeContainer.Add(CreateRow(_gravityNameLabel, _gravityLabel));
            runtimeContainer.Add(CreateRow(_directionNameLabel, _directionLabel));
            runtimeContainer.Add(CreateRow(_anchorNameLabel, _anchorLabel));
            runtimeContainer.Add(CreateRow(_currentVentNameLabel, _currentVentLabel));

            GoToOriginButton = new(GoToOrigin)
            {
                name = "Go To Origin"
            };
            runtimeContainer.Add(GoToOriginButton);
            void GoToOrigin() => (target as PlayerMovementBody).NavDestination(Vector3.zero);

            activeTab.Add(runtimeContainer);

            tabView.Add(activeTab);

            // Force a redraw/update of property fields
            so.Update();

            // Setup update loop for runtime info when in Play Mode
            void SubscribeUpdate()
            {
                if (_subscribedToUpdate) return;
                EditorApplication.update += EditorUpdate;
                _subscribedToUpdate = true;
            }
            void UnsubscribeUpdate()
            {
                if (!_subscribedToUpdate) return;
                EditorApplication.update -= EditorUpdate;
                _subscribedToUpdate = false;
            }

            // Initial subscription if playing
            if (EditorApplication.isPlaying)
                SubscribeUpdate();
            else
                UnsubscribeUpdate();

            // When inspector is created, also ensure we react to play mode changes to start/stop updating
            EditorApplication.playModeStateChanged += (state) =>
            {
                if (state == PlayModeStateChange.EnteredPlayMode)
                    SubscribeUpdate();
                else if (state == PlayModeStateChange.ExitingPlayMode || state == PlayModeStateChange.EnteredEditMode)
                    UnsubscribeUpdate();
            };

            return tabView;
        }

        // Clean up subscription when editor is disabled / destroyed
        private void OnDisable()
        {
            if (_subscribedToUpdate)
            {
                EditorApplication.update -= EditorUpdate;
                _subscribedToUpdate = false;
            }
        }

        // Update labels with runtime data
        private void EditorUpdate()
        {
            if (serializedObject == null) return;
            var pb = serializedObject.targetObject as PlayerMovementBody;
            if (pb == null) return;

            // Update textual info; guard with try/catch to avoid throwing during domain reloads
            try
            {
                _velLabelA.text = pb.velocity.Local.ToString("F3");
                _velLabelB.text = pb.velocity.Global.ToString("F3");
                _jumpStateLabel.text = pb.JumpStateCurrent.ToString();
                _onNavMeshLabel.text = pb.OnNavMesh ? "Yes" : "No";

                Vector3 navDest = Vector3.zero;
                bool hasNav = pb.NavDestination(out navDest);
                bool showNav = pb.OnNavMesh && hasNav;
                _navDestLabel.text = showNav ? navDest.ToString("F3") : "(none)";
                // Toggle visibility of the nav row
                _navRow.style.display = showNav ? DisplayStyle.Flex : DisplayStyle.None;

                _gravityLabel.text = pb.gravity.ToString();

                _directionLabel.text = pb.DirectionGet.ToString("F3");

                // Anchor reflection: only show when collider is present
                var anchorObj = typeof(PlayerMovementBody)
                    .GetField("anchorPoint", BindingFlags.NonPublic | BindingFlags.Instance)
                    ?.GetValue(pb);

                string anchorText = "(none)";
                if (anchorObj != null)
                {
                    // Try to get collider property on the anchor and confirm it's non-null before showing details
                    var anchorType = anchorObj.GetType();
                    var colliderProp = anchorType.GetProperty("collider");
                    var normalProp = anchorType.GetProperty("normal");
                    var pointProp = anchorType.GetProperty("point");

                    var colObj = colliderProp?.GetValue(anchorObj) as Collider;
                    if (colObj != null)
                    {
                        string colliderName = colObj.name;
                        string normal = normalProp?.GetValue(anchorObj)?.ToString() ?? "(unknown)";
                        string point = pointProp?.GetValue(anchorObj)?.ToString() ?? "(unknown)";
                        anchorText = $"Collider: {colliderName}, Normal: {normal}, Point: {point}";
                    }
                }
                _anchorLabel.text = anchorText;

                // Current vent (if present)
                var currentVent = typeof(PlayerMovementBody)
                    .GetProperty("CurrentVent", BindingFlags.Public | BindingFlags.Instance)
                    ?.GetValue(pb) as VolcanicVent;
                _currentVentLabel.text = currentVent != null ? currentVent.name : "(none)";
            }
            catch
            {
                // swallow exceptions during assembly reloads / domain changes
            }
        }
    }
#endif

    #endregion DEBUG

}

