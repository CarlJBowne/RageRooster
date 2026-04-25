using System;
using System.Collections;
using System.Collections.Generic;
using EditorAttributes;
using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using Unity.VisualScripting;
using UnityEditor;
using UnityEngine;

/// <summary>
/// Potential Central, Overridable system for Collide and Slide movement. Currently unused and in development.
/// </summary>
[System.Serializable]
public class CollideAndSlide
{
    /*
    #region Config
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
    [Tooltip("An unconventional means of hopefully avoiding falling through the floor. If true, the player will check if there is any ground below them before moving, and if not, their velocity will be set to 0 to prevent them from moving further. This is jank, but it might help with some edge cases and it doesn't require any extra components or setup.")]
    public bool mario64StyleAntiVoid;
    public LayerMask nonVoidLayerMask;
    #endregion

    #region Components
    Rigidbody RB;
    CapsuleCollider Collider;
    Transform transform;
    #endregion

    #region Delegates

    public delegate Vector3 GetVector3Delegate();
    public delegate void SetVector3Delegate(Vector3 value);
    public delegate bool GetBoolDelegate();
    public delegate void SetBoolDelegate(bool value);

    public GetVector3Delegate GetPosition;
    public SetVector3Delegate SetPosition;
    public GetVector3Delegate GetVelocity;
    public SetVector3Delegate SetVelocity;
    public GetVector3Delegate GetDirection;
    public SetVector3Delegate SetDirection;
    public GetBoolDelegate GetGrounded;
    public SetBoolDelegate SetGrounded;

    public Vector3 Position
    {
        get => GetPosition();
        set => SetPosition(value);
    }
    public Vector3 Velocity
    {
        get => GetVelocity();
        set => SetVelocity(value);
    }
    public Vector3 Direction
    {
        get => GetDirection();
        set => SetDirection(value);
    }
    public bool Grounded
    {
        get => GetGrounded();
        set => SetGrounded(value);
    }

    #endregion

    #region Data

    Vector3 initVelocity;

    #endregion

    public virtual void Initialize()
    {

    }

    /// <summary>
    /// The Collide and Slide Algorithm.
    /// </summary>
    /// <param name="vel">Input Velocity.</param>
    /// <param name="prevNormal">The Normal of the previous Step.</param>
    /// <param name="step">The current step. Starts at 0.</param>
    public virtual void MoveRecursive(Vector3 vel, Vector3 prevNormal, int step = 0, bool testString = false)
    {
        if (testString) moveTestString += $"Step {step}: {vel}\n";

        if (step == 0 && vel.y <= 0)
        {
            bool tryGround = GroundCheck(out var groundRes);
            if (Grounded && !tryGround) UnLand();
            else if (!Grounded && tryGround) Land(groundRes);
        }

        if (RB.DirectionCast(vel.normalized, vel.magnitude, groundCheckBuffer, out RaycastHit hit))
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
            if (scaleByDot) newDir *= Vector3.Dot(leftover.normalized, nextNormal) + 1;
            MoveRecursive(newDir, nextNormal, step + 1);
        }
        else
        {
            if (testString) moveTestString += "No Hit\n";

            if (step == movementProjectionSteps) return;
            if (!MoveForward(vel)) return;

            //Snap to ground when walking on a downward slope.
            if (Grounded && initVelocity.y <= 0)
            {
                if (RB.DirectionCast(Vector3.down, 0.5f, groundCheckBuffer, out RaycastHit groundHit))
                {
                    // Make sure the hit is under the character's feet (not beside it).
                    // Compute the bottom-center point of the capsule in world space.
                    Vector3 bottomCenter = GetPosition() + Collider.center - Vector3.up * (Collider.height * 0.5f - Collider.radius);
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

    public string moveTestString = "";

    public virtual bool StopForward(ref Vector3 nextNormal, Vector3 newNormal)
    {
        nextNormal = newNormal.XZ().normalized;
        return true;
    }
    public virtual bool MoveForward(Vector3 offset)
    {
        if (!Mario64StyleAntiVoidCheck(offset))
        {
            SetPosition(GetPosition() + offset);
            return false;
        }
        else
        {
            SetVelocity(Vector3.zero);
            return true;
        }
    }

    void WalkOff()
    {
        //UnLand();
        //Machine.SendSignal(new("WalkOff", ignoreLock: true));
    }



    bool Mario64StyleAntiVoidCheck(Vector3 offset) => mario64StyleAntiVoid &&
        !Physics.Raycast(GetPosition() + Vector3.up + offset, Vector3.down, 5000, nonVoidLayerMask, QueryTriggerInteraction.Collide);

    public void Land(AnchorPoint groundHit)
    {
        bool wasntGrounded = jumpState != JumpState.Grounded;
        bool objectChange = anchorPoint.transform != groundHit.transform;
        doubleJump.allowDoubleJump = true;

        if (!wasntGrounded && !objectChange) return;

        jumpState = JumpState.Grounded;
        anchorPoint = groundHit;
        velocity.y = 0;

        if (objectChange)
        {
            movingAnchor?.SetPlayerInfluence(false);
            movingAnchor = anchorPoint.transform.GetComponent<IMovablePlatform>();
            movingAnchor?.SetPlayerInfluence(true);
        }

        if (wasntGrounded)
        {
            LandEvent?.Invoke();
            Machine.SendSignal(new("Land", ignoreLock: true));
            if (playerController.CheckJumpBuffer()) Machine.SendSignal("Jump");
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

    */
}
