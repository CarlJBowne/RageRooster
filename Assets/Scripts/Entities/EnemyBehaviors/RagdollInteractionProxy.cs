using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;

public class RagdollInteractionProxy : MonoBehaviour, IDamagable, IGrabbable
{
    public EnemyHealth health;
    public RagdollHandler ragDoll;

    [SerializeField] new Collider collider;
    [SerializeField] Rigidbody rb;
    public IGrabbable This => ragDoll;

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

    #region Interface Members
    public IGrabber Grabber => This.Grabber;
    public float AdditionalThrowDistance => This.AdditionalThrowDistance;
    //public float AdditionalHoldHeight => This.AdditionalHoldHeight;
    public void IgnoreCollisionWithThrower(Collider thrower, bool ignore = true) => Physics.IgnoreCollision(collider, thrower, ignore);
    public bool Grab(IGrabber grabber) => This.Grab(grabber);
    public void Throw(Vector3 velocity) => This.Throw(velocity);
    public void Release() => This.Release();
    public void SetVelocity(Vector3 velocity) => This.SetVelocity(velocity);
    public bool IsGrabbable => This.IsGrabbable;

    public Vector3 HeldOffset => This.HeldOffset;

    public Rigidbody rigidBody => This.rigidBody;

    #endregion
}
