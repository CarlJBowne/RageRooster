using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

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
    [RelatedComponent(true)] public new Collider collider;
    [RelatedComponent(true)] public MeleeTarget meleeTarget;

    //Potential Components
    [RelatedComponent] public Rigidbody rigidBody;
    [RelatedComponent] public RagdollHandler ragdollHandler;
    [RelatedComponent] public ThrownObjectAttack thrownObjectAttack;
    [RelatedComponent] public Health health;
    [RelatedComponent] public ConstantMovement constantMovement;
    [RelatedComponent] public EntityActivity entityActivity;

    #endregion
    #region Data

    public enum State
    {
        Inactive = -1,
        Grabbable = 0,
        Grabbed = 1,
        Thrown = 2
    }
    private State _state = State.Inactive;

    #endregion

    public static bool IsGrabbable(MeleeTarget target, out Grabbable result)
    {
        result = target == null ? null
            : target.TryGetComponent(out Grabbable grabbable) ? grabbable
            : target.TryGetComponent(out GrabbableIndirect indirect) ? indirect.Get()
            : null;

        return result != null && result.GetGrabbable();
    }
    private void Reset() => ComponentConfig.Reset(this);// Auto-fill common components in editor

    private void OnEnable() { if (state is not State.Grabbable) state = State.Grabbable; }
    private void OnDisable() { if(state is State.Grabbable) state = State.Inactive; }

    public bool GetGrabbable()
    {
        bool result = enabled;

        if (health && health.GetCurrentHealth() > grabHealthMax) result = false;

        return result;
    }

    public void Grab()
    {
        state = State.Grabbed;
        if (entityActivity) entityActivity.CurrentState = EntityActivity.State.Grabbed;
    }

    public void Throw(Vector3 throwVelocity)
    {
        state = State.Thrown;
        if (entityActivity) entityActivity.CurrentState = EntityActivity.State.Thrown;

        if (thrownObjectAttack)
        {
            thrownObjectAttack.onContactAction += () =>
            {
                state = State.Grabbable;
            };
        }
        else
        {
            PostThrowStateEnum().Begin(this);
            IEnumerator PostThrowStateEnum()
            {
                yield return new WaitForSeconds(1f);
                state = State.Grabbable;
            } 
        }

        if (ragdollHandler) ragdollHandler.enabled = true;
        SetVelocity(throwVelocity);
    }

    public void Release()
    {
        state = State.Grabbable;
        if (entityActivity) entityActivity.CurrentState = EntityActivity.State.Default;
    }

    public State state
    {
        get => _state;
        private set
        {
            if (value == _state) return;

            State prev = _state;
            _state = value;

            enabled = value == State.Grabbable;
            meleeTarget.enabled = value == State.Grabbable;

            if (value > State.Grabbable || prev > State.Grabbable)
            {
                if (collider != null)
                    Physics.IgnoreCollision(collider, Player.Collider, value > State.Grabbable);
                if (ragdollHandler != null) ragdollHandler.IgnoreCollisionWith(Player.Collider, value > State.Grabbable);
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
