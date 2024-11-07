using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHealth : Health
{
    public float invincibilityTime;

    private Coroutine invincibility;
    private new Collider collider;
    [System.Obsolete, HideInInspector]
    public AttackConstant queuedAttack;

    protected override void Awake()
    {
        base.Awake();
        collider = GetComponent<Collider>();
    }

    protected override void OnDamage(int amount)
    {
        base.OnDamage(amount);
        invincibility = StartCoroutine(InvinceEnum(invincibilityTime));
        damagable = false;
    }

    private IEnumerator InvinceEnum(float time)
    {
        yield return new WaitForSeconds(time);
        InvincEnd();
    }

    private void InvincEnd()
    {
        damagable = true;
        collider.enabled = false;
        collider.enabled = true;
        //if(queuedAttack != null) queuedAttack.BeginAttack(this);
    }

    private void InvincEndDontWork()
    {
        CapsuleCollider caps = GetComponent<CapsuleCollider>();
        float radius = caps.radius + 0.05f;
        Vector3 point1 = transform.position + caps.center - (Vector3.up * ((caps.height / 2) - caps.radius));
        Vector3 point2 = transform.position + caps.center + (Vector3.up * ((caps.height / 2) - caps.radius));

        Collider[] colls = Physics.OverlapCapsule(point1, point2, radius, GetComponent<Rigidbody>().includeLayers, QueryTriggerInteraction.Collide); //Fix Getting Rigidbody's useful layers

        for (int i = 0; i < colls.Length; i++)
        {
            if(colls[i].TryGetComponent(out AttackSource soc))
            {
                if (soc.IsSelfCollider()) soc.BeginAttack(gameObject);
                break;
            }
            else if(colls[i].TryGetComponent(out AttackHitBox box))
            {
                box.ManualBeginAttack(gameObject);
                break;
            }
        }
    }


}
