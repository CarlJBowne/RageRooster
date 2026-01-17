using System.Collections;
using UnityEngine;

[RequireComponent(typeof(Collider))]
public class RagdollHandler : MonoBehaviour
{
    [SerializeField] float minCollisionTime;
    [SerializeField] float minRagdollTime;
    [SerializeField] float maxRagdollTime;
    [SerializeField] float minRagdollVelocity;

    [SerializeField, RelatedComponent(true)] Collider rootBoneCollider;
    [SerializeField] Collider[] ragDollColliders = new Collider[11];
    [SerializeField, RelatedComponent(true)] Rigidbody rootRigidBody;
    [SerializeField] Rigidbody[] ragDollRigidBodies = new Rigidbody[11];

    [SerializeField, RelatedComponent(true)] Collider defaultCollider;
    [SerializeField, RelatedComponent] Rigidbody defaultRigidBody;

    [SerializeField, RelatedComponent] EntityActivity entityActivity;
    [SerializeField, RelatedComponent] Grabbable grabbable;
    [SerializeField, RelatedComponent] Health health;

    public enum States
    {
        Off,
        Grabbed,
        Thrown,
        Ragdoll
    }
    private States _state = States.Off;

    private RigidbodyProfile defaultRigidbodyDefaults;
    private float timer = -1;


    private void Reset()
    {
        ComponentConfig.Reset(this);
        enabled = false;
    }

    private void Awake()
    {
        if(ragDollRigidBodies == null || ragDollRigidBodies.Length == 0)
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

    public States State
    {
        get => _state;
        set
        {
            _state = value;
            if (entityActivity && value != States.Off) entityActivity.CurrentState = EntityActivity.State.RagDoll;
            enabled = value != States.Off;

            defaultCollider.isTrigger = value != States.Off;
            rootBoneCollider.enabled = value > States.Grabbed;
            timer = value > States.Grabbed ? 0 : -1;

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
                if (ragDollColliders[i] != null) ragDollColliders[i].enabled = value > States.Grabbed;
                if (i < ragDollRigidBodies.Length && ragDollRigidBodies[i] != null) ragDollRigidBodies[i].isKinematic = value == States.Off;
            }

            if (value == States.Off) ragDollColliders[0].transform.Reset(scale: false);
        }
    }



    private void FixedUpdate()
    {
        if(rootRigidBody != defaultRigidBody)
        {
            transform.position = rootBoneCollider.transform.position;
            rootBoneCollider.transform.localPosition = Vector3.zero;
        }

        if(timer >= 0)
        {
            timer += Time.fixedDeltaTime;
            if((timer >= minRagdollTime && ragDollRigidBodies[0].linearVelocity.magnitude < minRagdollVelocity) || timer >= maxRagdollTime)
            {
                Poof();
                timer = -1;
            }
        }
    }



    public void SetVelocity(Vector3 velocity)
    {
        defaultRigidBody.linearVelocity = velocity;
        for (int i = 0; i < ragDollRigidBodies.Length; i++)
            if (ragDollRigidBodies[i] != null) ragDollRigidBodies[i].linearVelocity = velocity;
    }

    public void IgnoreCollisionWith(Collider other, bool ignore = true)
    {
        //Physics.IgnoreCollision(rootBoneCollider, other, ignore);
        for (int i = 0; i < ragDollColliders.Length; i++)
            if (ragDollColliders[i] != null) Physics.IgnoreCollision(ragDollColliders[i], other, ignore);

        if (defaultCollider != null) Physics.IgnoreCollision(defaultCollider, other, ignore);
    }

    private void OnTriggerEnter(Collider other)
    {
        if (State == States.Off) return;
        if (State is States.Thrown && timer > minCollisionTime)
        {
            State = States.Ragdoll;
            if (grabbable && !grabbable.enabled) grabbable.enabled = true;
            if (other.TryGetComponent(out IAttackSource attack)) SetVelocity(attack.GetAttack().velocity);
        }
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