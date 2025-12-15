using System.Collections;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

[RequireComponent(typeof(Collider))]
public class RagdollHandler : MonoBehaviour, IEntityComponent
{
    public float minRagdollTime;
    public float maxRagdollTime;
    public float minRagdollVelocity;

    [SerializeField, RelatedComponent(true)] Collider rootBoneCollider;
    [SerializeField] Collider[] ragDollColliders = new Collider[11];
    [SerializeField, RelatedComponent(true)] Rigidbody rootRigidBody;
    [SerializeField] Rigidbody[] ragDollRigidBodies = new Rigidbody[11];

    [SerializeField, RelatedComponent(true)] Collider defaultCollider;
    [SerializeField, RelatedComponent] Rigidbody defaultRigidBody;

    [field: SerializeField, RelatedComponent] public Entity Entity { get; set; }
    [SerializeField, RelatedComponent] Grabbable grabbable;
    [SerializeField, RelatedComponent] Health health;

    public enum States
    {
        Off,
        On,
        Grabbed
    }
    private States _state = States.Off;

    private RigidbodyProfile defaultRigidbodyDefaults;
    private float poofTimer = -1;

    private void Reset()
    {
        ComponentConfig.Reset(this);
        IEntityComponent.Reset(this);
        enabled = false;
    }

    private void Awake()
    {
        IEntityComponent.Awake(this);
        if (ragDollRigidBodies == null || ragDollRigidBodies.Length == 0)
        {
            Debug.LogWarning($"RagdollHandler on {gameObject.name} has no ragdoll rigidbodies assigned. What the hell???");
            Destroy(this);
        }

        Physics.IgnoreCollision(defaultCollider, rootBoneCollider);
        if (defaultRigidBody) defaultRigidbodyDefaults = new(defaultRigidBody);

        // Ignore collisions between the interaction collider and ragdoll colliders so the proxy doesn't self-collide.
        for (int i = 0; i < ragDollColliders.Length; i++)
            if (ragDollColliders[i] != null)
                Physics.IgnoreCollision(defaultCollider, ragDollColliders[i]);
    }

    public void StateChangeReceiver(Entity.States state)
    {
        bool isRagdoll = state is Entity.States.Thrown or Entity.States.RagDoll or Entity.States.Grabbed;
        bool ragdollCollides = state is Entity.States.Thrown or Entity.States.RagDoll;

        enabled = isRagdoll;

        defaultCollider.isTrigger = enabled;
        rootBoneCollider.enabled = ragdollCollides;

        if (rootRigidBody == defaultRigidBody)
        {
            rootRigidBody.isKinematic = isRagdoll ? false : defaultRigidbodyDefaults.isKinematic;
            rootRigidBody.useGravity = isRagdoll ? true : defaultRigidbodyDefaults.useGravity;
        }
        else
        {
            defaultRigidBody.isKinematic = isRagdoll ? true : defaultRigidbodyDefaults.isKinematic;
            rootRigidBody.isKinematic = !isRagdoll;
        }

        for (int i = 0; i < ragDollColliders.Length; i++)
        {
            if (ragDollColliders[i] != null) ragDollColliders[i].enabled = isRagdoll && ragdollCollides;
            if (i < ragDollRigidBodies.Length && ragDollRigidBodies[i] != null) ragDollRigidBodies[i].isKinematic = !isRagdoll;
        }

        if (!isRagdoll) ragDollColliders[0].transform.Reset(scale: false);
    }
    /*
    private States state
    {
        get => _state;
        set
        {
            _state = value;
            if (Entity && value != States.Off) Entity.State = Entity.States.RagDoll;
            enabled = value != States.Off;

            defaultCollider.isTrigger = value != States.Off;
            rootBoneCollider.enabled = value == States.On;

            if (rootRigidBody == defaultRigidBody)
            {
                rootRigidBody.isKinematic = value != States.Off ? false : defaultRigidbodyDefaults.isKinematic;
                rootRigidBody.useGravity = value != States.Off ? true : defaultRigidbodyDefaults.useGravity;
            }
            else
            {
                defaultRigidBody.isKinematic = value != States.Off ? true : defaultRigidbodyDefaults.isKinematic;
                rootRigidBody.isKinematic = value == States.Off;
            }

            for (int i = 0; i < ragDollColliders.Length; i++)
            {
                if (ragDollColliders[i] != null) ragDollColliders[i].enabled = value == States.On;
                if (i < ragDollRigidBodies.Length && ragDollRigidBodies[i] != null) ragDollRigidBodies[i].isKinematic = value == States.Off;
            }

            if (value == States.Off) ragDollColliders[0].transform.Reset(scale: false);
        }
    }
    */


    private void FixedUpdate()
    {
        if(rootRigidBody != defaultRigidBody)
        {
            transform.position = rootBoneCollider.transform.position;
            rootBoneCollider.transform.localPosition = Vector3.zero;
        }

        if(poofTimer >= 0)
        {
            poofTimer += Time.fixedDeltaTime;
            if((poofTimer >= minRagdollTime && ragDollRigidBodies[0].linearVelocity.magnitude < minRagdollVelocity) || poofTimer >= maxRagdollTime)
            {
                Poof();
                poofTimer = -1;
            }
        }
    }

    public bool PoofCycle
    {
        get => poofTimer > 0;
        set
        {
            poofTimer = value ? 0 : -1;
        }
    }

    public void SetVelocity(Vector3 velocity)
    {
        defaultRigidBody.linearVelocity = velocity;
        for (int i = 0; i < ragDollRigidBodies.Length; i++)
            if (ragDollRigidBodies[i] != null) ragDollRigidBodies[i].linearVelocity = velocity;
    }

    // convenience alias for merged-proxy semantics
    public void IgnoreCollisionWith(Collider other, bool ignore = true)
    {
        //Physics.IgnoreCollision(rootBoneCollider, other, ignore);
        for (int i = 0; i < ragDollColliders.Length; i++)
            if (ragDollColliders[i] != null) Physics.IgnoreCollision(ragDollColliders[i], other, ignore);

        if (defaultCollider != null) Physics.IgnoreCollision(defaultCollider, other, ignore);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (Entity.State is Entity.States.Thrown or Entity.States.RagDoll) return;
        if (grabbable && !grabbable.enabled) grabbable.enabled = true;
        if (!PoofCycle) PoofCycle = true;

        if(other.TryGetComponent(out IAttackSource attack)) SetVelocity(attack.GetAttack().velocity);
    }

    public void Poof()
    {
        if(health != null) health.Destroy();
        else gameObject.SetActive(false);
    }

}

/// <summary>
/// A basic class for storing A Rigidbody's configured data.
/// </summary>
[System.Serializable]
public class RigidbodyProfile
{
    public bool isKinematic = false;
    public float mass = 1f;
    public float linearDamping = 0f;
    public float angularDamping = 0.05f;
    public bool useGravity = true;
    public RigidbodyInterpolation interpolation = RigidbodyInterpolation.None;
    public CollisionDetectionMode collisionDetectionMode = CollisionDetectionMode.Discrete;
    public LayerMask includeLayers;
    public LayerMask excludeLayers;

    public RigidbodyProfile(Rigidbody source)
    {
        if (source == null) return;
        mass = source.mass;
        linearDamping = source.linearDamping;
        angularDamping = source.angularDamping;
        useGravity = source.useGravity;
        interpolation = source.interpolation;
        collisionDetectionMode = source.collisionDetectionMode;
        isKinematic = source.isKinematic;
        includeLayers = source.includeLayers;
        excludeLayers = source.excludeLayers;
    }
    public void ApplyTo(Rigidbody rb)
    {
        if (rb == null) return;
        rb.mass = mass;
        rb.linearDamping = linearDamping;
        rb.angularDamping = angularDamping;
        rb.useGravity = useGravity;
        rb.interpolation = interpolation;
        rb.collisionDetectionMode = collisionDetectionMode;
        rb.isKinematic = isKinematic;
        rb.includeLayers = includeLayers;
        rb.excludeLayers = excludeLayers;
    }
}