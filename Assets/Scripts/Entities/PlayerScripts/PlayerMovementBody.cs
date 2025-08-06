using EditorAttributes;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PlayerMovementBody : CharacterMovementBody
{
    #region Config
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


    private VolcanicVent _currentVent;
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
    }
    public void DirectionSet(float maxTurnSpeed) => DirectionSet(playerController.camAdjustedMovement, maxTurnSpeed);
    public void InstantDirectionChange(Vector3 target)
    {
        if (target.sqrMagnitude == 0) return;
        direction = target;
    }

    public VolcanicVent currentVent
    {
        get => _currentVent;
        set
        {
            _currentVent = value;
            Machine.SendSignal(new(value != null ? "EnterVent" : "ExitVent", 0,  true));
        }
    }
    public bool isOverVent => _currentVent != null;


    public new Vector3 direction
    {
        get => base.direction;
        private set
        {
            if (base.direction == value) return;
            base.direction = value;
            RotationQ = Quaternion.LookRotation(value, Vector3.up);
        }
    }


    #endregion GetSet

    //public PlayerTestScript playerTestScript;

    protected override void Awake()
    {
        TryGetComponent(out animator);
        direction = Vector3.forward;
    }

    protected override void FixedUpdate()
    {
        Machine.animator.SetFloat("CurrentSpeed", currentSpeed);
        if (PlayerStateMachine.DEBUG_MODE_ACTIVE && Input.Jump.IsPressed()) VelocitySet(y: 10f);

        base.FixedUpdate();
    }


    protected override bool StopForward(ref Vector3 nextNormal, Vector3 newNormal)
    {
        nextNormal = newNormal.XZ().normalized;
        return Machine.SendSignal(new("Bonk", 0, true));
    }
    protected override bool MoveForward(Vector3 offset)
    {
        if (Mario64StyleAntiVoid && !Physics.Raycast(transform.position + Vector3.up + offset, Vector3.down, 5000, nonVoidLayerMask, QueryTriggerInteraction.Collide))
        {
            velocity = Vector3.zero;
            return false;
        }
        else return base.MoveForward(offset);
    }
    protected override void WalkOff()
    {
        UnLand();
        Machine.SendSignal(new("WalkOff", ignoreLock: true));
    } 


    public override void Land(BodyAnchor groundHit)
    {
        if (JumpState == JumpState.Grounded) return;
        JumpState = JumpState.Grounded;
        anchorPoint.Update(groundHit);
        LandEvent?.Invoke();
        Machine.SendSignal(new("Land", ignoreLock: true));
        if (playerController.CheckJumpBuffer()) Machine.SendSignal("Jump");
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

    public void ReturnToNeutral(bool doCrossFade = true)
    {
        if(GroundCheck(out _))
        {
            idleState.Enter();
            //animator.SetTrigger("ReturnToGroundNeutral");
            if (doCrossFade) animator.CrossFade("GroundBasic", .1f);
        }
        else 
            airNeutralState.Enter();
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