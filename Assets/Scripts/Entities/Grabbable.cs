using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(Collider),typeof(MeleeTarget))]
public class Grabbable : MonoBehaviour
{
    #region Config

    public int grabHealthMax;
    public float wiggleFreeTime;
    public Transform anchorPoint;
    public float AdditionalThrowDistance;

    public System.Action ForceRelease { get; set; }

    //Required Components
    public new Collider collider;
    public MeleeTarget meleeTarget;

    //Potential Components
    public Rigidbody rigidBody;
    public RagdollHandler ragdollHandler;
    public ThrownObjectAttack thrownObjectAttack;
    public EnemyHealth health;
    public ConstantMovement constantMovement;

    #endregion
    #region Data

    public enum State
    {
        Inactive = -1,
        Grabbable = 0,
        Grabbed = 1,
        Thrown = 2
    }
    public State state { get; protected set; } = State.Inactive;

    #endregion

    public static bool IsGrabbable(MeleeTarget target, out Grabbable result)
    {
        result = target == null ? null
            : target.TryGetComponent(out Grabbable grabbable) ? grabbable
            : target.TryGetComponent(out GrabbableIndirect indirect) ? indirect.Get()
            : null;

        return result != null && result.GetGrabbable();
    }
    private void Reset()
    {
        rigidBody = GetComponent<Rigidbody>();
        collider = GetComponent<Collider>();
        meleeTarget = GetComponent<MeleeTarget>();
        ragdollHandler = GetComponent<RagdollHandler>();
        health = GetComponent<EnemyHealth>();
        constantMovement = GetComponent<ConstantMovement>();
    }

    private void OnEnable()
    {
        state = State.Grabbable;
    }
    private void OnDisable()
    {
        state = State.Inactive;
    }

    public bool GetGrabbable()
    {
        bool result = enabled;

        if (health && health.GetCurrentHealth() > grabHealthMax) result = false;

        return result;
    }

    public void Grab()
    {
        state = State.Grabbed;
        IgnoreCollisionWith(Player.Collider);
    }

    public void Release(Vector3? throwVelocity = null)
    {
        state = State.Thrown;
        if (throwVelocity.HasValue) SetVelocity(throwVelocity.Value);

        enabled = false;
        if (thrownObjectAttack) thrownObjectAttack.onContactAction += () => { enabled = true; };

        Enum().Begin(collider);
        IEnumerator Enum()
        {
            yield return WaitFor.Frames(5);
            IgnoreCollisionWith(Player.Collider, false);
        }
    }

    public void SetVelocity(Vector3 velocity)
    {
        if (rigidBody != null) rigidBody.linearVelocity = velocity;
        else if (constantMovement != null)
        {
            constantMovement.Set(velocity);
            constantMovement.ResetDownwardVelocity();
        }
        if (ragdollHandler != null) ragdollHandler.SetVelocity(velocity);
    }

    public void IgnoreCollisionWith(Collider other, bool ignore = true)
    {
        if (collider != null)
            Physics.IgnoreCollision(collider, other, ignore);
        if (ragdollHandler != null) ragdollHandler.IgnoreCollisionWith(other, ignore);
    }

    public Vector3 HeldOffset => anchorPoint != null ? -anchorPoint.localPosition : Vector3.zero;




}
