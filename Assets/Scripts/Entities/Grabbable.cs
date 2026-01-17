using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[RequireComponent(typeof(Collider), typeof(MeleeTarget))]
public class Grabbable : MonoBehaviour
{
    #region Config

    public int grabHealthMax;
    public float wiggleFreeTime;
    public Transform anchorPoint;
    public float AdditionalThrowDistance;

    public System.Action ForceRelease { get; set; }

    //Required Components
    [RelatedComponent(true)] public new Collider collider;
    [RelatedComponent(true)] public MeleeTarget meleeTarget;

    //Potential Components
    [SerializeField, RelatedComponent] Rigidbody rigidBody;
    [SerializeField, RelatedComponent] RagdollHandler ragdollHandler;
    [SerializeField, RelatedComponent] ThrownObjectAttack thrownObjectAttack;
    [SerializeField, RelatedComponent] Health health;
    [SerializeField, RelatedComponent] ConstantMovement constantMovement;
    [SerializeField, RelatedComponent] EntityActivity entityActivity;

    #endregion
    #region Data

    public enum States
    {
        Inactive = -1,
        Grabbable = 0,
        Grabbed = 1,
        Thrown = 2
    }
    private States state = States.Inactive;
    private RigidbodyProfile rigidbodyProfile;

    #endregion

    public static bool IsGrabbable(MeleeTarget target, out Grabbable result)
    {
        result = target == null ? null
            : target.TryGetComponent(out Grabbable grabbable) ? grabbable
            : target.TryGetComponent(out GrabbableIndirect indirect) ? indirect.Get()
            : null;

        return result != null && result.GetGrabbable();
    }
    void Reset() => ComponentConfig.Reset(this);// Auto-fill common components in editor

    void Awake() => rigidbodyProfile = rigidBody != null ? new(rigidBody) : null;

    void OnEnable() { if (State is not States.Grabbable) State = States.Grabbable; }
    void OnDisable() { if (State is States.Grabbable) State = States.Inactive; }

    public bool GetGrabbable()
    {
        bool result = enabled;

        if (health && health.GetCurrentHealth() > grabHealthMax) result = false;

        return result;
    }

    public void Grab()
    {
        State = States.Grabbed;
        if (entityActivity) entityActivity.CurrentState = EntityActivity.State.Grabbed;
        if (ragdollHandler) ragdollHandler.State = RagdollHandler.States.Grabbed;
        else if (rigidBody)
        {
            rigidBody.isKinematic = true;
            collider.enabled = false;
        }
    }

    public void Throw(Vector3 throwVelocity)
    {
        State = States.Thrown;
        if (entityActivity) entityActivity.CurrentState = EntityActivity.State.Thrown;

        if (thrownObjectAttack)
        {
            thrownObjectAttack.onContactAction += () =>
            {
                State = States.Grabbable;
            };
        }
        else
        {
            PostThrowStateEnum().Begin(this);
            IEnumerator PostThrowStateEnum()
            {
                yield return new WaitForSeconds(1f);
                State = States.Grabbable;
            }
        }

        if (ragdollHandler) ragdollHandler.State = RagdollHandler.States.Thrown;
        else if (rigidBody)
        {
            rigidBody.isKinematic = rigidbodyProfile.isKinematic;
            collider.enabled = true;
        }
        SetVelocity(throwVelocity);
    }

    public void Release()
    {
        State = States.Grabbable;
        if (entityActivity) entityActivity.CurrentState = EntityActivity.State.Default;
        if (ragdollHandler) ragdollHandler.State = RagdollHandler.States.Off;
        else if (rigidBody)
        {
            rigidBody.isKinematic = rigidbodyProfile.isKinematic;
            collider.enabled = true;
        } 
    }

    public States State
    {
        get => state;
        private set
        {
            if (value == state) return;

            States prev = state;
            state = value;

            enabled = value == States.Grabbable;
            meleeTarget.enabled = value == States.Grabbable;

            if (value > States.Grabbable || prev > States.Grabbable)
            {
                if (collider != null)
                    Physics.IgnoreCollision(collider, Player.Collider, value > States.Grabbable);
                if (ragdollHandler != null) ragdollHandler.IgnoreCollisionWith(Player.Collider, value > States.Grabbable);
            }
        }
    }

    public void SetVelocity(Vector3 velocity)
    {
        if (ragdollHandler)
        {
            ragdollHandler.SetVelocity(velocity);
        }
        else if (rigidBody)
        {
            rigidBody.linearVelocity = velocity;
        }
        else if (constantMovement)
        {
            constantMovement.Set(velocity);
            constantMovement.ResetDownwardVelocity();
        }
    }

    public Vector3 HeldOffset => anchorPoint != null ? -anchorPoint.localPosition : Vector3.zero;




}
