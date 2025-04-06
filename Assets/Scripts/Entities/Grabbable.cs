using EditorAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Grabbable : MonoBehaviour, IGrabbable, IAttackSource
{
    #region Config

    public bool twoHanded;
    public float weight;
    public float wiggleFreeTime;
    public int maxHealthToGrab;
    public float additionalThrowDistance;
    [HideInEditMode, HideInPlayMode] public UltEvents.UltEvent<EntityState> GrabStateEvent;

    [FoldoutGroup("Entity State Change Events", nameof(defaultEvent),nameof(grabbedEvent),nameof(thrownEvent),nameof(bounceEvent))]
    public Void _EntityStateEvents;
    [HideInInspector] public UltEvents.UltEvent defaultEvent;
    [HideInInspector] public UltEvents.UltEvent grabbedEvent;
    [HideInInspector] public UltEvents.UltEvent thrownEvent;
    [HideInInspector] public UltEvents.UltEvent bounceEvent;

    public Attack thrownAttack = new(1, "Thrown");

    #endregion
    #region Data

    protected Grabber grabber;
    public bool grabbed => grabber != null;

    private new Collider collider;
    private Rigidbody rb;
    public EnemyHealth health { get; protected set; }
    public CoroutinePlus wiggleCoroutine;

    [HideInEditMode, DisableInPlayMode] public EntityState currentState;

    #endregion

    protected virtual void Awake()
    {
        collider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        health = GetComponent<EnemyHealth>();
        SetState(EntityState.Default);
    }

    public bool GiveGrabbable(out Grabbable result)
    {
        result = this;
        return true;
    }

    public Grabbable Grab(Grabber grabber)
    {
        this.grabber = grabber;
        SetState(EntityState.Grabbed);
        SetVelocity(Vector3.zero);
        IgnoreCollisionWithThrower();

        if (wiggleFreeTime > 0) wiggleCoroutine = new(WiggleEnum(), this);
        IEnumerator WiggleEnum()
        {
            yield return new WaitForSeconds(wiggleFreeTime);
            Release();
        }

        return this;
    }


    public void Throw(Vector3 velocity)
    {
        if (!grabbed) return;
        SetState(EntityState.Thrown);
        SetVelocity(velocity);
    }
    public void Release()
    {
        if (!grabbed) return; 
        IgnoreCollisionWithThrower(false);

        grabber = null;
        SetState(EntityState.Default);
        SetVelocity(Vector3.zero);
    } 

    public bool UnderThreshold() => !health || maxHealthToGrab < 0 || health.GetCurrentHealth() <= maxHealthToGrab;

    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);
    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);

    public virtual void Contact(GameObject target)
    {
        if(currentState == EntityState.Thrown)
        {
            SetState(EntityState.RagDoll);
            if (thrownAttack.amount > 0 && target.TryGetComponent(out IDamagable targetDamagable)) targetDamagable.Damage(GetAttack());
            IgnoreCollisionWithThrower(false);
            grabber = null;
        }
    }

    public virtual void SetState(EntityState newState)
    {
        currentState = newState;
        GrabStateEvent?.Invoke(currentState);

        switch (newState)
        {
            case EntityState.Default:
                rb.isKinematic = false;
                collider.enabled = true;
                break;
            case EntityState.Grabbed:
                rb.isKinematic = true;
                collider.enabled = false;
                break;
            case EntityState.Thrown:
                rb.isKinematic = false;
                collider.enabled = true;
                break;
            case EntityState.RagDoll:
                break;
            default:
                break;
        }

        (newState switch
        {
            EntityState.Grabbed => grabbedEvent,
            EntityState.Thrown => thrownEvent,
            EntityState.RagDoll => bounceEvent,
            _ => defaultEvent,
        })?.Invoke();
    }

    public virtual void SetVelocity(Vector3 velocity) => rb.velocity = velocity;

    public virtual void IgnoreCollisionWithThrower(bool ignore = true) => Physics.IgnoreCollision(collider, grabber.collider, ignore);

    public Attack GetAttack()
    {
        Attack result = thrownAttack;
        result.velocity = rb.velocity;
        return result;
    }
}

public interface IGrabbable
{
    public static bool Grab(GameObject target, out Grabbable result)
    {
        result = null;
        return target.TryGetComponent(out IGrabbable interfaceResult) &&
        interfaceResult.GiveGrabbable(out result) &&
        result.enabled &&
        result.UnderThreshold()
        ? true : false;
    }

    public bool GiveGrabbable(out Grabbable result);
}