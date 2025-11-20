using System;
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
    bool grabbed;

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
    public float AdditionalThrowDistance => This.AdditionalThrowDistance;
    //public float AdditionalHoldHeight => This.AdditionalHoldHeight;
    public void IgnoreCollisionWithThrower(Collider thrower, bool ignore = true) => Physics.IgnoreCollision(collider, thrower, ignore);
    public void Release(Vector3? velocity) => This.Release(velocity);
    public void SetVelocity(Vector3 velocity) => This.SetVelocity(velocity);
    public bool Grab() => throw new NotImplementedException();
    public void SetIgnoreCollision(Collider grabber, bool ignore = true) => throw new NotImplementedException();

    public bool IsGrabbable => This.IsGrabbable;

    GameObject IGrabbable.gameobject => gameObject;
    Transform IGrabbable.transform => transform;

    public Vector3 HeldOffset => This.HeldOffset;

    public Rigidbody rigidBody => This.rigidBody;

    Collider IGrabbable.collider => collider;

    bool IGrabbable.grabbed => grabbed;

    public Action ForceRelease { get => throw new NotImplementedException(); set => throw new NotImplementedException(); }



    #endregion
}
