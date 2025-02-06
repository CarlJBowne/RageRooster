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
    public float coyoteTime = 0.5f;
    public float tripleJumpTime = 0.3f;
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
    [DisableInPlayMode, DisableInEditMode] public float currentSpeed;
    [DisableInPlayMode, DisableInEditMode] public int jumpPhase;
    //-1 = Inactive
    //0 = PreMinHeight
    //1 = PreMaxHeight
    //2 = SlowingDown
    //3 = Falling
    [HideInInspector] public Vector3 _currentDirection = Vector3.forward; 

    [HideInInspector] public float coyoteTimeLeft;
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
        //M.physicsCallbacks += Collision;
    }

    public override void OnFixedUpdate()
    {
        Vector3 prevPosition = rb.position;
        {
            M.animator.SetFloat("CurrentSpeed", currentSpeed);
            rb.velocity = Vector3.zero;

            if (anchorTransform && !anchorTransform.gameObject.isStatic)
            {
                Vector3 anchorOffset = prevAnchorPosition - anchorTransform.localPosition;
                prevAnchorPosition = anchorTransform.localPosition;
                rb.MovePosition(rb.position - anchorOffset);
            }

            if (coyoteTimeLeft > 0) coyoteTimeLeft -= Time.deltaTime;



        } // NonMovement

        initVelocity = velocity;
        initNormal = Vector3.up;

        if (PlayerStateMachine.DEBUG_MODE_ACTIVE && Input.Jump.IsPressed()) VelocitySet(y: 10f);

        if (velocity.y <= 0.01f ||(sGrounded && velocity.y > 0.1f)) 
        {
            if(rb.DirectionCast(Vector3.down, checkBuffer, checkBuffer, out groundHit))
            {
#if UNITY_EDITOR
                AddToQueuedHits(new(groundHit));
#endif
                initNormal = groundHit.normal;
                if (WithinSlopeAngle(groundHit.normal))
                {
                    GroundStateChange(true);
                    LatchAnchor(groundHit.transform);
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
            rb.MovePosition(position + snapToSurface);

            if (step == movementProjectionSteps) return;

            if (grounded && hit.normal.y > 0 && !WithinSlopeAngle(hit.normal))
                nextNormal = prevNormal.XZ().normalized;

            if(!grounded && vel.y < 0 && hit.normal.y > 0) 
                if (WithinSlopeAngle(hit.normal))
                {
                    GroundStateChange(true);
                    LatchAnchor(hit.transform);
                    leftover.y = 0;
                }
                else leftover = leftover.ProjectAndScale(hit.normal.XZ().normalized);

            //Floor Ceiling Lock
            if (prevNormal.y > 0 && hit.normal.y < 0) //Hit Floor First
                nextNormal = (prevNormal.y != prevNormal.magnitude ? prevNormal : hit.normal).XZ().normalized;
            else if(prevNormal.y < 0 && hit.normal.y > 0) //Hit Cieling First
                nextNormal = (hit.normal.y != hit.normal.magnitude ? hit.normal : prevNormal).XZ().normalized;

            Vector3 newDir = leftover.ProjectAndScale(nextNormal) * (Vector3.Dot(leftover.normalized, nextNormal) + 1); 
            Move(newDir, nextNormal, step + 1);
        }
        else
        {
            rb.MovePosition(position + vel);
            if (grounded && initVelocity.y <= 0 && rb.DirectionCast(Vector3.down, 0.5f, checkBuffer, out RaycastHit groundHit)) 
                rb.MovePosition(position + Vector3.down * groundHit.distance);
        }
    }

    public bool GroundStateChange(bool input)
    {
        if (input == grounded || rb.velocity.y > 0.01f) return false;
        grounded = input;

        if (grounded)
        {
            jumpPhase = -1;
            if (!sGrounded.active)TransitionTo(sGrounded);
            if (controller.CheckJumpBuffer()) BeginJump();
        }
        else
        {
            if(!sAirborne.active && !sCharge.active) TransitionTo(sFall);
            else if(sCharge.active && !sAirChargeFall.active) TransitionTo(sAirChargeFall);

            coyoteTimeLeft = coyoteTime;
            LatchAnchor(null);
        }

        return true;
    }

    private bool WithinSlopeAngle(Vector3 inNormal)
    {
        float A = Vector3.Angle(Vector3.up, inNormal);
        return A < maxSlopeNormalAngle;
    }

    public void BeginJump()
    {
        if (!sCharge.active) jumpState1.BeginJump();
        else airChargeState.BeginJump();
        grounded = false;
        LatchAnchor(null);
    }

    private void OnCollisionEnter(Collision collision)
    {
        if ((jumpState1 || airChargeState) && Vector3.Dot(collision.GetContact(0).normal, Vector3.down) > 0.75f)
        {
            sFall.TransitionTo();
            VelocitySet(y: 0);
        }
    }

    public void LatchAnchor(Transform newAnchor)
    {
        if(newAnchor == null)
        {
            anchorTransform = null;
            return;
        }
        if (anchorTransform == newAnchor || newAnchor.gameObject.isStatic) return;
        anchorTransform = newAnchor;
        prevAnchorPosition = newAnchor.localPosition;
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