using EditorAttributes;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

public class Grabbable : MonoBehaviour, IAttackSource
{
    #region Config

    public bool twoHanded;
    public float weight;
    public float wiggleFreeTime;
    public int maxHealthToGrab;
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

    private Grabber grabber;
    public bool grabbed => grabber != null;

    public new Collider collider {  get; private set; }
    public Rigidbody rb { get; private set; }
    public EnemyHealth health { get; private set; }
    public CoroutinePlus wiggleCoroutine;

    [HideInEditMode, DisableInPlayMode] public EntityState currentState;

    #endregion

    private void Awake()
    {
        collider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        health = GetComponent<EnemyHealth>();
    }

    public static bool Grab(GameObject target, out Grabbable result) => target.TryGetComponent(out result) && result.enabled && result.UnderThreshold() ? true : false;

    public Grabbable Grab(Grabber grabber)
    {
        this.grabber = grabber;
        SetState(EntityState.Grabbed);

        if (rb)
        {
            rb.isKinematic = true;
            rb.velocity = Vector3.zero;
        }
        collider.enabled = false;

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
        Physics.IgnoreCollision(collider, grabber.collider, true);
        SetState(EntityState.Thrown);

        if (rb)
        {
            rb.isKinematic = false;
            rb.velocity = velocity;
        }
        collider.enabled = true;
    }
    public void Release()
    {
        if (!grabbed) return;
        //Physics.IgnoreCollision(collider, grabber.collider, true);
        grabber = null;
        SetState(EntityState.Default);

        if (rb)
        {
            rb.isKinematic = false;
            rb.velocity = Vector3.zero;
        }
        collider.enabled = true;
    }

    public bool UnderThreshold() => !health || maxHealthToGrab < 0 || health.GetCurrentHealth() <= maxHealthToGrab;

    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);
    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);

    public virtual void Contact(GameObject target)
    {
        if(currentState == EntityState.Thrown)
        {
            SetState(EntityState.RagDoll);
            if (thrownAttack.amount > 0 && target.TryGetComponent(out Health health)) health.Damage(GetAttack());
            Physics.IgnoreCollision(collider, grabber.collider, false);
            grabber = null;
        }
    }

    public void SetState(EntityState newState)
    {
        currentState = newState;
        GrabStateEvent?.Invoke(currentState);

        (newState switch
        {
            EntityState.Grabbed => grabbedEvent,
            EntityState.Thrown => thrownEvent,
            EntityState.RagDoll => bounceEvent,
            _ => defaultEvent,
        })?.Invoke();
    }

    public Attack GetAttack()
    {
        Attack result = thrownAttack;
        if(rb) result.velocity = rb.velocity;
        return result;
    }
}
