using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : Health, IAttacker
{
    public float stunTime;
    public GameObject poofPrefab;
    public Behaviour[] disableComponents;

    private float stunTimeLeft = 0;

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);

        if(stunTimeLeft == 0) stunEnum = StartCoroutine(StunEnum());
        stunTimeLeft += stunTime * (attack.wham ? 2 : 1);
    }

    protected override void OnDeplete(Attack attack)
    {
        depleteEvent?.Invoke();
        if (attack.wham)
        {
            StopCoroutine(stunEnum);
            if (ragDoll) Ragdoll(attack);
            else Destroy();
        }
    }

    private Coroutine stunEnum;
    private IEnumerator StunEnum()
    {
        SetStun(true);

        yield return null;
        while (stunTimeLeft > 0)
        {
            stunTimeLeft -= Time.deltaTime;
            yield return null;
        }
        stunTimeLeft = 0;

        if (health <= 0 && !hasRagdolled) Destroy();
        else SetStun(false);
    }

    public void SetStun(bool value)
    {
        if(disableComponents.Length > 0) 
            foreach (Behaviour B in disableComponents) 
                if(B != null) B.enabled = !value;

    }

    public void Destroy()
    {
        if(poofPrefab) Instantiate(poofPrefab);
        if (PoolableObject.Is(gameObject)) PoolableObject.Is(gameObject).Disable();
        else Destroy(gameObject);
    }


    [ToggleGroup("Ragdoll", nameof(minRagdollTime), nameof(maxRagdollTime), nameof(minRagdollVelovity))]
    public bool ragDoll;

    [HideInInspector] public float minRagdollTime;
    [HideInInspector] public float maxRagdollTime;
    [HideInInspector] public float minRagdollVelovity;
    public Attack thrownAttack;

    [HideInInspector] public bool projectile;
    [HideInInspector] public bool hasRagdolled;
    private bool hasHitSomething;
    private Rigidbody rb;

    public void Ragdoll(Attack attack)
    {
        rb = this.GetOrAddComponent<Rigidbody>();
        rb.isKinematic = false;
        rb.useGravity = true;
        hasRagdolled = true;
        hasHitSomething = false;
        rb.gameObject.layer = Layers.NonSolid;
        if (disableComponents.Length > 0) 
            foreach (Behaviour B in disableComponents) 
                if (B != null) B.enabled = false;
        rb.velocity = (attack.source as MonoBehaviour).transform.TransformDirection(attack.velocity);

        StartCoroutine(Ragdolling());
    }

    public IEnumerator Ragdolling()
    {
        float elapsedTime = 0;
        while (true) { yield return null;

            elapsedTime += Time.deltaTime;

            if(projectile && !hasHitSomething) continue;
            if (elapsedTime > minRagdollTime && (rb.velocity.magnitude < minRagdollVelovity || elapsedTime > maxRagdollTime))
                break;
        }
        Destroy();
    }

    public void Contact(GameObject target)
    {
        if (!hasRagdolled || target.layer == Layers.Player) return;
        hasHitSomething = true;
        (this as IAttacker).BeginAttack(target, thrownAttack, rb.velocity);
    }

    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);
    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);

}