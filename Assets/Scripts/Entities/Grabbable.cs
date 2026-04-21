using DG.Tweening;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;

[RequireComponent(typeof(Collider), typeof(Target))]
public class Grabbable : MonoBehaviour
{
    /// <summary>
    /// The possible results of a grab interaction.
    /// </summary>
    public enum GrabResult
    {
        /// <summary>
        /// Successful grab. Actually results in Grabbing.
        /// </summary>
        Success,
        /// <summary>
        /// The target was suddenly out of range or made intangible.
        /// </summary>
        Missed,
        /// <summary>
        /// The target was not grabbable (too heavy, invulnerable, etc).
        /// </summary>
        Blocked,
        /// <summary>
        /// The target was, in truth, just an interactable that allowed Grabbing to activate it.
        /// </summary>
        Passthrough
    }

    #region Config

    public bool grabbablePublic = true;
    public int grabHealthMax;
    public float wiggleFreeTime;
    public Transform anchorPoint;
    public float AdditionalThrowDistance;

    public System.Action ForceRelease { get; set; }

    //Required Components
    [RelatedComponent(true)] public new Collider collider;
    [RelatedComponent(true)] public Target target;

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

    public static GrabResult Attempt(GameObject target, Action<Grabbable> success, Action miss, Action blocked)
    {
        target.TryGetComponent(out Grabbable grabbable);
        if (grabbable == null && target.TryGetComponent(out GrabbableIndirect ind)) grabbable = ind.Get();

        if (grabbable != null)
        {
            Grabbable.GrabResult grabResult = grabbable.GetGrabbable();
            if (grabResult == Grabbable.GrabResult.Success)
            {
                success?.Invoke(grabbable);
                return GrabResult.Success;
            }
            else if (grabResult == Grabbable.GrabResult.Missed)
            {
                miss?.Invoke();
                return GrabResult.Missed;
            }
            else if (grabResult == Grabbable.GrabResult.Blocked)
            {
                blocked?.Invoke();
                return GrabResult.Blocked;
            }
        }

        if (grabbable == null && target.TryGetComponent(out GrabbableSwitch @switch)) return GrabResult.Passthrough;

        miss?.Invoke();
        return GrabResult.Missed;
    }
    public static void Attempt(GameObject target, out GrabResult grabResult, out Grabbable grabbable)
    {
        target.TryGetComponent(out grabbable);
        if (grabbable == null && target.TryGetComponent(out GrabbableIndirect ind)) grabbable = ind.Get();

        if (grabbable != null) grabResult = grabbable.GetGrabbable();
        else if (grabbable == null && target.TryGetComponent(out GrabbableSwitch @switch))
        {
            @switch.Invoke();
            grabResult = GrabResult.Passthrough;
        }
        else grabResult = GrabResult.Missed;
    }


    void Reset() => ComponentConfig.Reset(this);// Auto-fill common components in editor

    void Awake() => rigidbodyProfile = rigidBody != null ? new(rigidBody) : null;

    void OnEnable() { if (State is not States.Grabbable) State = States.Grabbable; }
    void OnDisable() { if (State is States.Grabbable) State = States.Inactive; }

    public GrabResult GetGrabbable()
    {
        var result = GrabResult.Success;

        if (health && health.GetCurrentHealth() > grabHealthMax) result = GrabResult.Blocked;

        return result;
    }

    public GrabResult Grab()
    {
        var res = GetGrabbable();
        if (res != GrabResult.Success) return res;

        State = States.Grabbed;
        if (entityActivity) entityActivity.State = EntityActivity.States.Grabbed;
        if (ragdollHandler) ragdollHandler.State = RagdollHandler.States.Grabbed;
        else if (rigidBody)
        {
            rigidBody.isKinematic = true;
            collider.enabled = false;
        }

        return res;
    }

    public void Throw(Vector3 throwVelocity)
    {
        State = States.Thrown;
        if (entityActivity) entityActivity.State = EntityActivity.States.Thrown;

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
        if (entityActivity) entityActivity.State = EntityActivity.States.Default;
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
            target.enabled = value == States.Grabbable;

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
