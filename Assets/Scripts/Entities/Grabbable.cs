using EditorAttributes;
using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Events;
using UnityEngine.InputSystem.LowLevel;

public class Grabbable : MonoBehaviour, IGrabbable, IAttackSource
{
    #region Config

    public Transform anchorPoint;
    public float weight;
    public float wiggleFreeTime;
    public int maxHealthToGrab;
    public float additionalThrowDistance;
    public float additionalHoldHeight;
    public GameObject selectIcon;

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

    private IGrabber _Grabber;
    public bool grabbed => Grabber != null;

    private new Collider collider;
    private Rigidbody rb;
    public EnemyHealth health { get; protected set; }

    public CoroutinePlus wiggleCoroutine;

    [SerializeField, HideInEditMode, DisableInPlayMode] protected EntityState currentState;


    #endregion
    #region Interface Getters
    public IGrabbable This => this;
    public IGrabber Grabber { get => _Grabber; }
    Transform IGrabbable.transform { get => transform; }
    public float AdditionalThrowDistance => additionalThrowDistance;
    public float AdditionalHoldHeight => additionalHoldHeight;
    public virtual bool IsGrabbable => gameObject.activeInHierarchy && UnderThreshold() && currentState != EntityState.Grabbed && currentState != EntityState.Thrown;

    public virtual Rigidbody rigidBody => rb;

    public Vector3 HeldOffset => anchorPoint != null ? -anchorPoint.localPosition : Vector3.zero;

    public virtual bool Selected
    {
        get => (UnityEngine.Object)PlayerInteracter.SelectedGrabbable == this;
        set { if (selectIcon != null) selectIcon.SetActive(value); }
    }

    #endregion


    protected virtual void Awake()
    {
        collider = GetComponent<Collider>();
        rb = GetComponent<Rigidbody>();
        health = GetComponent<EnemyHealth>();
        State = EntityState.Default;
    }

    public bool Grab(IGrabber grabber)
    {
        _Grabber = grabber;
        State = EntityState.Grabbed;
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
        State = EntityState.Thrown;
        SetVelocity(velocity);
    }
    public void Release()
    {
        if (!grabbed) return; 
        IgnoreCollisionWithThrower(false);

        _Grabber = null;
        State = EntityState.Default;
        SetVelocity(Vector3.zero);
    } 

    public bool UnderThreshold() => !health || maxHealthToGrab < 0 || health.GetCurrentHealth() <= maxHealthToGrab;

    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);
    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);

    public virtual void Contact(GameObject target)
    {
        if(currentState == EntityState.Thrown && target != PlayerInteracter.ThisGameObject)
        {
            State = EntityState.RagDoll;
            if (thrownAttack.amount > 0 && target.TryGetComponent(out IDamagable targetDamagable)) targetDamagable.Damage(this.GetAttack());
            IgnoreCollisionWithThrower(false);
            _Grabber = null;
        }
    }

    // Replaced SetState method with virtual property State acting as a setter method
    public virtual EntityState State
    {
        get => currentState;
        set
        {
            if (currentState == value) return;
            currentState = value;
            GrabStateEvent?.Invoke(currentState);

            switch (currentState)
            {
                case EntityState.Default:
                    rigidBody.isKinematic = false;
                    collider.enabled = true;
                    PlayerInteracter.UpdateGrabbables();
                    break;
                case EntityState.Grabbed:
                    rigidBody.isKinematic = true;
                    collider.enabled = false;
                    PlayerInteracter.LostGrabbable(this);
                    IgnoreCollisionWithThrower(true);
                    break;
                case EntityState.Thrown:
                    rigidBody.isKinematic = false;
                    collider.enabled = true;
                    IgnoreCollisionWithThrower(true);
                    break;
                case EntityState.RagDoll:
                    IgnoreCollisionWithThrower(false);
                    PlayerInteracter.UpdateGrabbables();
                    break;
                default:
                    break;
            }

            (currentState switch
            {
                EntityState.Grabbed => grabbedEvent,
                EntityState.Thrown => thrownEvent,
                EntityState.RagDoll => bounceEvent,
                _ => defaultEvent,
            })?.Invoke();
        }
    }

    public virtual void SetVelocity(Vector3 velocity) => rigidBody.linearVelocity = velocity;

    public virtual void IgnoreCollisionWithThrower(bool ignore = true) => Physics.IgnoreCollision(collider, Grabber.ownerCollider, ignore);

    public Attack GetAttack()
    {
        Attack result = thrownAttack;
        result.velocity = rigidBody.linearVelocity; 
        return result;
    }

    private void OnDisable() => PlayerInteracter.LostGrabbable(this);

}
