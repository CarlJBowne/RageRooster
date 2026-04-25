using EditorAttributes;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.InputSystem.LowLevel;

[RequireComponent(typeof(MeleeTarget)), System.Obsolete]
public class Grabbable_Obsolete : MonoBehaviour, IGrabbable_Obsolete, IAttackSource
{
    //New

    //Old
    #region Config

    public Transform anchorPoint;
    public float weight;
    public float wiggleFreeTime;
    public int maxHealthToGrab;
    public float additionalThrowDistance;
    public float additionalHoldHeight;
    public GameObject selectIcon;

    [HideInEditMode, HideInPlayMode] public UltEvents.UltEvent<EntityActivity.States> GrabStateEvent;

    [FoldoutGroup("Entity State Change Events", nameof(defaultEvent), nameof(grabbedEvent), nameof(thrownEvent), nameof(bounceEvent))]
    public Void _EntityStateEvents;
    [HideInInspector] public UltEvents.UltEvent defaultEvent;
    [HideInInspector] public UltEvents.UltEvent grabbedEvent;
    [HideInInspector] public UltEvents.UltEvent thrownEvent;
    [HideInInspector] public UltEvents.UltEvent bounceEvent;

    public Attack thrownAttack = new(1, new());

    #endregion
    #region Data

    public bool grabbed;

    public new Collider collider { get; private set; }

    private Rigidbody rb;
    public EnemyHealth health { get; protected set; }

    public Coroutine wiggleCoroutine;

    [SerializeField, HideInEditMode, DisableInPlayMode] protected EntityActivity.States currentState;


    #endregion
    #region Interface Getters
    public IGrabbable_Obsolete This => this;
    Transform IGrabbable_Obsolete.transform => transform;
    GameObject IGrabbable_Obsolete.gameobject => gameObject;
    bool IGrabbable_Obsolete.grabbed => grabbed;


    public float AdditionalThrowDistance => additionalThrowDistance;
    public float AdditionalHoldHeight => additionalHoldHeight;
    public virtual bool IsGrabbable => gameObject.activeInHierarchy && UnderThreshold() && currentState != EntityActivity.States.Grabbed && currentState != EntityActivity.States.Thrown;

    public virtual Rigidbody rigidBody => rb;

    public Vector3 HeldOffset => anchorPoint != null ? -anchorPoint.localPosition : Vector3.zero;

    public System.Action ForceRelease { get; set; }

    #endregion


    protected virtual void Awake()
    {
        collider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        health = GetComponent<EnemyHealth>();
        State = EntityActivity.States.Default;
    }

    public bool Grab()
    {
        grabbed = true;
        State = EntityActivity.States.Grabbed;
        SetVelocity(Vector3.zero);

        if (wiggleFreeTime > 0) wiggleCoroutine = new(WiggleEnum(), this);
        IEnumerator WiggleEnum()
        {
            yield return new WaitForSeconds(wiggleFreeTime);
            ForceRelease?.Invoke();
        }

        return this;
    }

    public void Throw(Vector3 velocity)
    {
        if (!grabbed) return;
        State = EntityActivity.States.Thrown;
        SetVelocity(velocity);
    }
    public void Release()
    {

        if (!grabbed) return;

        State = EntityActivity.States.Default;
        SetVelocity(Vector3.zero);
    }

    public void Release(Vector3? velocity = null)
    {
        if (!grabbed) return;
        State = EntityActivity.States.Default;
        SetVelocity(velocity ?? Vector3.zero);
    }


    public bool UnderThreshold() => !health || maxHealthToGrab < 0 || health.GetCurrentHealth() <= maxHealthToGrab;

    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);
    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);

    public virtual void Contact(GameObject target)
    {
        if (currentState == EntityActivity.States.Thrown && target != PlayerInteracter.ThisGameObject)
        {
            State = EntityActivity.States.RagDoll;
            if (thrownAttack.amount > 0 && target.TryGetComponent(out IDamagable targetDamagable)) targetDamagable.Damage(this.GetAttack());
        }
    }

    public virtual EntityActivity.States State
    {
        get => currentState;
        set
        {
            if (currentState == value) return;
            currentState = value;
            GrabStateEvent?.Invoke(currentState);

            switch (currentState)
            {
                case EntityActivity.States.Default:
                    rigidBody.isKinematic = false;
                    collider.enabled = true;
                    break;
                case EntityActivity.States.Grabbed:
                    rigidBody.isKinematic = true;
                    collider.enabled = false;
                    break;
                case EntityActivity.States.Thrown:
                    rigidBody.isKinematic = false;
                    collider.enabled = true;
                    break;
                case EntityActivity.States.RagDoll:
                    break;
                default:
                    break;
            }

            (currentState switch
            {
                EntityActivity.States.Grabbed => grabbedEvent,
                EntityActivity.States.Thrown => thrownEvent,
                EntityActivity.States.RagDoll => bounceEvent,
                _ => defaultEvent,
            })?.Invoke();
        }
    }


    public virtual void SetVelocity(Vector3 velocity) => rigidBody.linearVelocity = velocity;

    public Attack GetAttack()
    {
        Attack result = thrownAttack;
        result.velocity = rigidBody.linearVelocity;
        return result;
    }

    public virtual void SetIgnoreCollision(Collider grabber, bool ignore = true) => Physics.IgnoreCollision(collider, grabber, ignore);


    [ContextMenu("Replace With New")]
    public void ReplaceWithNew()
    {
        Grabbable grabbable = gameObject.AddComponent<Grabbable>();
        grabbable.anchorPoint = anchorPoint;
        grabbable.wiggleFreeTime = wiggleFreeTime;
        grabbable.grabHealthMax = maxHealthToGrab;
        grabbable.AdditionalThrowDistance = additionalThrowDistance;

        if (this is RagdollHandler_Obsolete ragdoll) ragdoll.ReplaceWithNew();
    }

}
