using EditorAttributes;
using JigglePhysics;
using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;
using DG.Tweening;

public class PlayerMovementBody : PlayerStateBehavior
{
    #region Config
    public int movementProjectionSteps;
    public float checkBuffer = 0.005f;
    public float maxSlopeNormalAngle = 20f;
    public PlayerAirborneMovement jumpState1;
    public PlayerWallJump wallJumpState;
    public PlayerAirborneMovement airChargeState;
    public Vector3 frontCheckDefaultOffset;
    public float frontCheckDefaultRadius;
    public bool Mario64StyleAntiVoid;
    public LayerMask nonVoidLayerMask;
    public State idleState;
    public State airNeutralState;

    #endregion

    #region Data

    public static PlayerMovementBody Get() => _instance;
    private static PlayerMovementBody _instance;

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public new CapsuleCollider collider;
    [HideInInspector] public JiggleRigBuilder jiggles;
    [HideInInspector] public Animator animator;

    [HideInEditMode, DisableInPlayMode] public Vector3 velocity;

    [HideInInspector] public bool baseMovability = true;
    [HideInInspector] public bool canJump = true;
    public bool grounded = true;
    public float movementModifier = 1;
    public float CurrentSpeed
    {
        get => currentSpeed;
        set => currentSpeed = value.Min(0);
    }
    [HideInEditMode, DisableInPlayMode, SerializeField] private float currentSpeed;
    [HideInEditMode, DisableInPlayMode] public int jumpPhase;
    //-1 = Inactive
    //0 = PreMinHeight
    //1 = PreMaxHeight
    //2 = SlowingDown
    //3 = Falling

    [HideInInspector] public Vector3 _currentDirection = Vector3.forward;

    public static IMovablePlatform currentAnchor;

    private VolcanicVent _currentVent;
    #endregion

    #region GetSet
    public Vector3 position { 
        get => rb.position;
        set => rb.MovePosition(value); }
    public Quaternion rotationQ 
    { get => rb.rotation; set => rb.rotation = value; }
    public Vector3 rotation 
    { 
        get => transform.eulerAngles; 
        set => transform.eulerAngles = value; 
    }

    public Vector3 center => position + collider.center;

    [HideInInspector] public Vector3 currentDirection
    {
        get => _currentDirection;
        set
        {
            if (_currentDirection == value) return;
            _currentDirection = value;
            playerMovementBody.rotationQ = Quaternion.LookRotation(value, Vector3.up);
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

    public void DirectionSet(float maxTurnSpeed, Vector3 target)
    {
        if (target == Vector3.zero) return; 
        currentDirection = Vector3.RotateTowards(currentDirection, target.normalized, maxTurnSpeed * Mathf.PI * Time.deltaTime, 1);
    }
    public void DirectionSet(float maxTurnSpeed) => DirectionSet(maxTurnSpeed, playerController.camAdjustedMovement);
    public void InstantDirectionChange(Vector3 target)
    {
        if (target.sqrMagnitude == 0) return;
        currentDirection = target;
    }

    public VolcanicVent currentVent
    {
        get => _currentVent;
        set
        {
            _currentVent = value;
            Machine.SendSignal(value != null ? "EnterVent" : "ExitVent", addToQueue: false, overrideReady: true);
        }
    }
    public bool isOverVent => _currentVent != null;



    #endregion GetSet



    public override void OnAwake()
    {
        TryGetComponent(out rb);
        TryGetComponent(out collider);
        TryGetComponent(out jiggles);
        TryGetComponent(out animator);
        currentDirection = Vector3.forward;
        _instance = this;
    }

    public override void OnFixedUpdate()
    {
        Vector3 prevPosition = rb.position;
        {
            Machine.animator.SetFloat("CurrentSpeed", currentSpeed);
            rb.velocity = Vector3.zero;
        }

        initVelocity = new Vector3(velocity.x * movementModifier, velocity.y, velocity.z * movementModifier);
        initNormal = Vector3.up; 

        if (PlayerStateMachine.DEBUG_MODE_ACTIVE && Input.Jump.IsPressed()) VelocitySet(y: 10f);

        if (velocity.y < 0.01f || grounded) 
        {
            if(GroundCheck(out groundHit))
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
            else if (grounded)
            {
                GroundStateChange(false);
                Machine.SendSignal("WalkOff", overrideReady: true);
            }
        }

        Move(initVelocity * Time.fixedDeltaTime, initNormal);

        Machine.freeLookCamera.transform.position += transform.position - prevPosition;
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

            if (step == movementProjectionSteps) return;

            if(!MoveForward(snapToSurface)) return;

            if (grounded)
            {
                //Runs into wall/to high incline.
                if (Mathf.Approximately(hit.normal.y, 0) || (hit.normal.y > 0 && !WithinSlopeAngle(hit.normal))) 
                    Stop(hit.normal);

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
                else if (vel.y < -1f && rb.DirectionCastAll(vel, vel.y.Abs(), checkBuffer, out RaycastHit[] downHits) && downHits.Length > 1)
                {
                    GroundStateChange(true, null);
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
                    if (Vector3.Dot(newNormal, vel.normalized.XZ()) <= -.75f)
                        stopped = true;
                }

            if (stopped && Machine.SendSignal("Bonk", overrideReady: true, addToQueue: false)) return;

            Vector3 newDir = leftover.ProjectAndScale(nextNormal) * (Vector3.Dot(leftover.normalized, nextNormal) + 1); 
            Move(newDir, nextNormal, step + 1);
        }
        else
        {

            if (step == movementProjectionSteps) return;
            if (!MoveForward(vel)) return;

            //Snap to ground when walking on a downward slope.
            if (grounded && initVelocity.y <= 0)
            {
                if (rb.DirectionCast(Vector3.down, 0.5f, checkBuffer, out RaycastHit groundHit))
                    rb.MovePosition(position + Vector3.down * groundHit.distance);
                else
                {
                    GroundStateChange(false);
                    Machine.SendSignal("WalkOff", overrideReady: true);
                }
            }
        }

        bool MoveForward(Vector3 offset)
        {
            if(Mario64StyleAntiVoid && !Physics.Raycast(transform.position + Vector3.up + offset, Vector3.down, 5000, nonVoidLayerMask, QueryTriggerInteraction.Collide))
            {
                velocity = Vector3.zero;
                return false;
            }
            else
            {
                rb.MovePosition(position + offset);
                return true;
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

        if (grounded)
        {
            jumpPhase = -1;

            Machine.SendSignal("Land", overrideReady: true);

            if (playerController.CheckJumpBuffer()) Machine.SendSignal("Jump");
        }

        currentAnchor = anchor == null || !anchor.transform.TryGetComponent(out IMovablePlatform movingAnchor) 
            ? null 
            : movingAnchor;

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

        jiggles.PrepareTeleport();
        rb.MovePosition(position + Vector3.down * hit.distance);
        jiggles.FinishTeleport();
    }

    public T CheckForTypeInFront<T>(Vector3 sphereOffset, float checkSphereRadius)
    {
        Collider[] results = Physics.OverlapSphere(position + transform.TransformDirection(sphereOffset),
                                                   checkSphereRadius);
        foreach (Collider r in results)
            if (r.TryGetComponent(out T result))
                return result;
        return default;
    }
    public T CheckForTypeInFront<T>()
    {
        Collider[] results = Physics.OverlapSphere(position + transform.TransformDirection(frontCheckDefaultOffset),
                                                   frontCheckDefaultRadius);
        foreach (Collider r in results)
            if (r.gameObject != gameObject && r.TryGetComponent(out T result))
                return result;
        return default;
    }

    public void ReturnToNeutral(bool doCrossFade = true)
    {
        if(GroundCheck())
        {
            idleState.TransitionTo();
            //animator.SetTrigger("ReturnToGroundNeutral");
            if (doCrossFade) animator.CrossFade("GroundBasic", .1f);
        }
        else 
            airNeutralState.TransitionTo();
    }

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

    public bool GroundCheck(out RaycastHit groundHit) => rb.DirectionCast(Vector3.down, checkBuffer, checkBuffer, out groundHit);
    public bool GroundCheck() => rb.DirectionCast(Vector3.down, checkBuffer, checkBuffer, out _);


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

public enum JumpPhase
{
    Inactive = -1,
    PreMinHeight = 0,
    PreMaxHeight = 1,
    SlowingDown = 2,
    Falling = 3
}