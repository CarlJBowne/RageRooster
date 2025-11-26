using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RagdollHandler : MonoBehaviour
{
    /// <summary>
    /// Whether this Entity is currently in ragdoll state.
    /// </summary>
    public new bool enabled
    {
        get => base.enabled;
        set
        {
            base.enabled = value;


            for (int i = 0; i < ragDollColliders.Length; i++)
            {
                if (ragDollColliders[i] != null) ragDollColliders[i].enabled = value;
                if (i < ragDollRigidBodies.Length && ragDollRigidBodies[i] != null) ragDollRigidBodies[i].isKinematic = !value;
            }


            if (!value) ragDollColliders[0].transform.Reset(scale: false);

            defaultCollider.isTrigger = value;

            defaultRigidbody.isKinematic = value || defaultRigidbodyDefaults.isKinematic;

            if(value) Enum().Begin(this);
            IEnumerator Enum()
            {
                float timer = 0f;
                while (timer < maxRagdollTime)
                {
                    timer += Time.deltaTime;
                    yield return null;
                    if (timer > minRagdollTime && ragDollRigidBodies[0].linearVelocity.magnitude < minRagdollVelocity) break;
                }
                Poof();
            }
        }
    }

    public float minRagdollTime;
    public float maxRagdollTime;
    public float minRagdollVelocity;

    public Collider[] ragDollColliders;
    public Rigidbody[] ragDollRigidBodies;

    //Required Components
    [RelatedComponent(true)]
    public Collider defaultCollider;

    // Optional Components
    [RelatedComponent]
    public Rigidbody defaultRigidbody;
    [RelatedComponent]
    public Grabbable grabbable;
    [RelatedComponent]
    public Health enemyHealth;

    private RigidbodyProfile defaultRigidbodyDefaults;

    private void Reset() => ComponentConfig.Reset(this);// Auto-fill common components in editor//if (grabbable == null) TryGetComponent(out grabbable);

    private void Awake()
    {
        if(ragDollRigidBodies == null || ragDollRigidBodies.Length == 0)
        {
            Debug.LogWarning($"RagdollHandler on {gameObject.name} has no ragdoll rigidbodies assigned. What the hell???");
            Destroy(this);
        }

        if (defaultRigidbody) defaultRigidbodyDefaults = new(defaultRigidbody);

        // Ignore collisions between the interaction collider and ragdoll colliders so the proxy doesn't self-collide.
        if (defaultCollider != null && ragDollColliders != null)
        {
            for (int i = 0; i < ragDollColliders.Length; i++)
            {
                if (ragDollColliders[i] != null)
                    Physics.IgnoreCollision(defaultCollider, ragDollColliders[i]);
            }
        }

        // initialize
        enabled = false;
    }

    private void FixedUpdate()
    {
        transform.position = ragDollRigidBodies[0].transform.position;
        ragDollRigidBodies[0].transform.localPosition = Vector3.zero;
    }




    public void SetVelocity(Vector3 velocity)
    {
        for (int i = 0; i < ragDollRigidBodies.Length; i++)
            if (ragDollRigidBodies[i] != null) ragDollRigidBodies[i].linearVelocity = velocity;
    }

    // convenience alias for merged-proxy semantics
    public void IgnoreCollisionWith(Collider other, bool ignore = true)
    {
        if (ragDollColliders != null)
        {
            for (int i = 0; i < ragDollColliders.Length; i++)
                if (ragDollColliders[i] != null) Physics.IgnoreCollision(ragDollColliders[i], other, ignore);

            if (defaultCollider != null) Physics.IgnoreCollision(defaultCollider, other, ignore);
        }
        else if (grabbable != null)
        {
            grabbable.IgnoreCollisionWith(other, ignore);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
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