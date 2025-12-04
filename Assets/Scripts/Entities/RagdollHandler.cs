using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RagdollHandler : MonoBehaviour
{
    public float minRagdollTime;
    public float maxRagdollTime;
    public float minRagdollVelocity;

    [SerializeField, Tooltip("Should have exaclty 11 for basic Humanoid Bodies")] Collider[] ragDollColliders = new Collider[11];
    [SerializeField, Tooltip("Should have exaclty 11 for basic Humanoid Bodies")] Rigidbody[] ragDollRigidBodies = new Rigidbody[11];

    [SerializeField, RelatedComponent(true)] Collider defaultCollider;

    [SerializeField, RelatedComponent] Rigidbody defaultRigidBody;
    [SerializeField, RelatedComponent] EntityActivity entityActivity;
    [SerializeField, RelatedComponent] Grabbable grabbable;
    [SerializeField, RelatedComponent] Health enemyHealth;

    private RigidbodyProfile defaultRigidbodyDefaults;
    private float poofTimer = -1;

    private void Reset()
    {
        ComponentConfig.Reset(this);
        //rootBoneCollider = null;
    }

    private void Awake()
    {
        if(ragDollRigidBodies == null || ragDollRigidBodies.Length == 0)
        {
            Debug.LogWarning($"RagdollHandler on {gameObject.name} has no ragdoll rigidbodies assigned. What the hell???");
            Destroy(this);
        }

        if (defaultRigidBody) defaultRigidbodyDefaults = new(defaultRigidBody);

        // Ignore collisions between the interaction collider and ragdoll colliders so the proxy doesn't self-collide.
        for (int i = 0; i < ragDollColliders.Length; i++)
        {
            if (ragDollColliders[i] != null)
            {
                Physics.IgnoreCollision(defaultCollider, ragDollColliders[i]);
            }
        }

        // initialize
        enabled = false;
    }

    private void OnEnable() => EnabledSet(true);

    private void OnDisable() => EnabledSet(false);

    private void EnabledSet(bool value)
    {
        base.enabled = value;
        if (entityActivity && value) entityActivity.CurrentState = EntityActivity.State.RagDoll;

        defaultCollider.isTrigger = value;
        //rootBoneCollider.enabled = value;

        //If true, make non-kinematic, otherwise restore default from profile.
        defaultRigidBody.isKinematic = value || defaultRigidbodyDefaults.isKinematic; 
        //defaultRigidBody.useGravity = value || defaultRigidbodyDefaults.useGravity;

        for (int i = 0; i < ragDollColliders.Length; i++)
        {
            if (ragDollColliders[i] != null) ragDollColliders[i].enabled = value;
            if (i < ragDollRigidBodies.Length && ragDollRigidBodies[i] != null) ragDollRigidBodies[i].isKinematic = !value;
        }

        if (!value) ragDollColliders[0].transform.Reset(scale: false);

        PoofCycle = value;
    }




    private void FixedUpdate()
    {
        transform.position = ragDollRigidBodies[0].transform.position;
        ragDollRigidBodies[0].transform.localPosition = Vector3.zero;

        if(poofTimer >= 0)
        {
            poofTimer += Time.fixedDeltaTime;
            if(poofTimer <= 0 && ragDollRigidBodies[0].linearVelocity.magnitude < minRagdollVelocity)
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
            if (!enabled) return;
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
        if (!enabled) return;
        if (grabbable && !grabbable.enabled) grabbable.enabled = true;

        if(other.TryGetComponent(out IAttackSource attack))
        {
            SetVelocity(attack.GetAttack().velocity);
        }
    }

    public void Poof()
    {
        if(enemyHealth != null) enemyHealth.Destroy();
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