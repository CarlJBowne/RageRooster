using EditorAttributes;
using SLS.StateMachineV2;
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
    public State groundedState;
    public State airborneState;
    public State fallState;
    public PlayerAirborn jumpState1;
    public PlayerAirborn jumpState2;
    public PlayerWallJump wallJumpState;
    public PlayerAirborn airChargeState;

    #endregion

    #region Data

    [HideInInspector] public Rigidbody rb;
    [HideInInspector] public new CapsuleCollider collider;

    [HideInInspector] public bool baseMovability = true;
    [HideInInspector] public bool canJump = true;
    public bool grounded = true;
    [HideInInspector] public bool secondJump;
    [DisableInPlayMode, DisableInEditMode] public float currentSpeed;
    [HideInInspector] public Vector3 _currentDirection = Vector3.forward; 

    [HideInInspector] public float coyoteTimeLeft;
    float tripleJumpTimeLeft;
    Transform anchorTransform;
    Vector3 prevAnchorPosition;

    [DisableInPlayMode, DisableInEditMode] public Vector3 velocity;
    #endregion

    #region GetSet
    //public Vector3 velocity { get => rb.velocity; set => rb.velocity = value; }
    public Vector3 position { get => rb.position; set => rb.position = value; }
    public Quaternion rotationQ { get => rb.rotation; set => rb.rotation = value; }
    public Vector3 rotation { get => transform.eulerAngles; set => transform.eulerAngles = value; }

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

        //collider.center = new Vector3(collider.center.x, (collider.height / 2) + skinDistance, collider.center.z);
    }

    public override void OnFixedUpdate()
    {
        {
            M.animator.SetFloat("CurrentSpeed", currentSpeed);
            rb.velocity = Vector3.zero;

            if (anchorTransform && !anchorTransform.gameObject.isStatic)
            {
                Vector3 anchorOffset = prevAnchorPosition - anchorTransform.position;
                prevAnchorPosition = anchorTransform.position;
                rb.MovePosition(rb.position - anchorOffset);
            }

            if (coyoteTimeLeft > 0) coyoteTimeLeft -= Time.deltaTime;

            if (groundedState.active && tripleJumpTimeLeft > 0)
            {
                tripleJumpTimeLeft -= Time.deltaTime;
                if (tripleJumpTimeLeft <= 0) secondJump = false;
            }



        } // NonMovement



        initVelocity = velocity;
        initNormal = Vector3.up;

        if (velocity.y <= 0.01f)
        {
            if(rb.DirectionCast(Vector3.down, checkBuffer, checkBuffer, out groundHit))
            {
                AddToQueuedHits(new(groundHit));
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
    }

    Vector3 initVelocity;
    Vector3 initNormal;
    RaycastHit groundHit;

    private void Move(Vector3 vel, Vector3 prevNormal, int step = 0)
    {
        if (rb.DirectionCast(vel.normalized, vel.magnitude, checkBuffer, out RaycastHit hit))
        {
            AddToQueuedHits(new(hit));
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
            if (prevNormal.y > 0 && hit.normal.y < 0) //If Floor First
                nextNormal = prevNormal.XZ().normalized;
            else if(prevNormal.y < 0 && hit.normal.y > 0) //If Cieling First
                nextNormal = hit.normal.XZ().normalized;

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

        if (!grounded)
        {
            coyoteTimeLeft = coyoteTime;
            LatchAnchor(null);
        }
        else tripleJumpTimeLeft = tripleJumpTime;
        if ((grounded && !groundedState.active) || (!grounded && !airborneState.active))
            TransitionTo(grounded ? groundedState : fallState);
        if (grounded && controller.CheckJumpBuffer()) BeginJump();

        return true;
    }

    private bool WithinSlopeAngle(Vector3 inNormal)
    {
        float A = Vector3.Angle(Vector3.up, inNormal);
        return A < maxSlopeNormalAngle;
    }

    public void BeginJump()
    {
        if (!controller.chargingState)
        {
            (secondJump ? jumpState2 : jumpState1).BeginJump();
            secondJump.Toggle();
        }
        else
        {
            airChargeState.BeginJump();
        }
        grounded = false;
        LatchAnchor(null);
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
        prevAnchorPosition = newAnchor.position;
    }


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