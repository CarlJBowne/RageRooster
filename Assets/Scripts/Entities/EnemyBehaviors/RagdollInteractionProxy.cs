using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RagdollInteractionProxy : MonoBehaviour, IDamagable, IGrabbable
{
    public EnemyHealth health;
    public RagdollHandler ragDoll;

    [SerializeField] new Collider collider;
    [SerializeField] Rigidbody rb;

    private void Awake()
    {
        for (int i = 0; i < ragDoll.ragDollColliders.Length; i++)
            Physics.IgnoreCollision(collider, ragDoll.ragDollColliders[i]);
    }

    public bool Damage(Attack attack) => health.Damage(attack);
    public bool GiveGrabbable(out Grabbable result)
    {
        result = ragDoll;
        return result != null;
    }

    public void SetRagdoll(bool value) => collider.enabled = value;

    private void OnTriggerEnter(Collider other)
    {
        if (other.isTrigger && !other.TryGetComponent(out IAttackSource _)) return;
        ragDoll.Contact(other.gameObject);
    }

    public void IgnoreCollisionWithThrower(Collider thrower, bool ignore = true) => Physics.IgnoreCollision(collider, thrower, ignore);
}
