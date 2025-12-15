using DG.Tweening;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SocialPlatforms;
using static UnityEngine.Rendering.DebugUI;

[RequireComponent(typeof(Collider),typeof(MeleeTarget))]
public class Grabbable : MonoBehaviour, IEntityComponent
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
    [field: SerializeField, RelatedComponent] public Entity Entity { get; set; }

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


    public bool GetGrabbable()
    {
        bool result = enabled;

        if (health && health.GetCurrentHealth() > grabHealthMax) result = false;

        return result;
    }

    public void Grab()
    {
        Entity.State = Entity.States.Grabbed;
        if(rigidBody) rigidBody.isKinematic = true;
    }

    public void Throw(Vector3 throwVelocity)
    {
        Entity.State = Entity.States.Thrown;
        if (Entity) Entity.State = Entity.States.Thrown;

        if (thrownObjectAttack)
        {
            thrownObjectAttack.onContactAction += () =>
            {
                Entity.State = Entity.States.RagDoll;
            };
        }
        else
        {
            PostThrowStateEnum().Begin(this);
            IEnumerator PostThrowStateEnum()
            {
                yield return new WaitForSeconds(1f);
                enabled = true;
            } 
        }

        Entity.State = Entity.States.Thrown;
        if (rigidBody) rigidBody.isKinematic = rigidbodyProfile.isKinematic;
        SetVelocity(throwVelocity);
    }

    public void Release()
    {
        Entity.State = Entity.States.Default;
        if(rigidBody) rigidBody.isKinematic = rigidbodyProfile.isKinematic;
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

    public void StateChangeReceiver(Entity.States state)
    {
        

        enabled = state is not Entity.States.Grabbed or Entity.States.Thrown;
        meleeTarget.enabled = state is not Entity.States.Grabbed or Entity.States.Thrown;

        if (state is Entity.States.Grabbed || activeState is Entity.States.Grabbed)
        {
            if (collider != null)
                Physics.IgnoreCollision(collider, Player.Collider, state is Entity.States.Grabbed);
            if (ragdollHandler != null) ragdollHandler.IgnoreCollisionWith(Player.Collider, state is Entity.States.Grabbed);
        }

        activeState = state;
    }

    private Entity.States activeState;

    public Vector3 HeldOffset => anchorPoint != null ? -anchorPoint.localPosition : Vector3.zero;




}
