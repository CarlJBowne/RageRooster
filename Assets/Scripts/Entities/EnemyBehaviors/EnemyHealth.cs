using EditorAttributes;
using SLS.StateMachineH;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class EnemyHealth : Health
{
    #region Config

    public float stunTime;
    public GameObject poofPrefab;
    [System.Obsolete]
    public Behaviour[] stunComponents;
    [SerializeField, RelatedComponent(true)] EntityActivity entityActivity;
    [SerializeField, RelatedComponent(true)] EnemyLootSpawner enemyLootSpawner;

    [RelatedComponent, SerializeField] ColorTintAnimation tintAnimator;
    [RelatedComponent, SerializeField] RagdollHandler ragdoll;

    public bool respawn;
    public float respawnTime;
    public UltEvents.UltEvent onDamageEvent;


    #endregion Config
    #region Data

    private Vector3 startPosition;


    #endregion Config


    private void Reset() => ComponentConfig.Reset(this);

    protected override void Awake() 
    { 
        base.Awake();
        startPosition = transform.position;
        if (TryGetComponent(out PoolableObject pool))
        {
            pool.onActivate += Respawn;
            respawn = false;
        }
        enemyLootSpawner = GetComponent<EnemyLootSpawner>();
    }

    #region DamageOverrides

    protected override bool OverrideDamageable(Attack attack) => !attack.tags.Contains(Attack.Tag.FromEnemy) || attack.tags.Contains(Attack.Tag.FriendlyFire);

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);

        if (ragdoll && ragdoll.enabled) ragdoll.SetVelocity(attack.velocity);
        else if (health != 0)
        {
            Stun(attack);
            if(tintAnimator) tintAnimator.BeginAnimation(); 
        }
    }

    protected override void OnDeplete(Attack attack)
    {
        depleteEvent?.Invoke();
        if (attack == Attack.Tag.Wham)
        {
            CoroutinePlus.Stop(ref stunRoutine);
            if (ragdoll)
            {
                ragdoll.enabled = true;
                ragdoll.SetVelocity(attack.velocity);
            }
            else Destroy();
        }
        else
        {
            Stun(attack);
            if (tintAnimator) tintAnimator.BeginAnimation();
        }
         
    }

    void Stun(Attack attack)
    {
        stunTimeLeft = stunTime * (attack == Attack.Tag.Wham ? 2 : 1);
        CoroutinePlus.Begin(ref stunRoutine, StunEnum(), this, false);

        IEnumerator StunEnum()
        {
            entityActivity.CurrentState = EntityActivity.State.Stunned;

            while(stunTimeLeft > 0)
            {
                stunTimeLeft -= Time.deltaTime;
                yield return null;
            }
            entityActivity.CurrentState = EntityActivity.State.Default;
            if(health <= 0)
            {
                if (ragdoll)
                {
                    ragdoll.enabled = true;
                    ragdoll.SetVelocity(attack.velocity);
                }
                else Destroy();
            }
        }
    }
    private CoroutinePlus stunRoutine;
    private float stunTimeLeft = 0;



    #endregion DamageOverrides


    public override void Destroy()
    {
        if (poofPrefab) Instantiate(poofPrefab);
        if (respawn)
        {
            gameObject.SetActive(false);
            Invoke(nameof(Respawn), respawnTime);
        }
        else if (PoolableObject.Is(gameObject)) PoolableObject.Is(gameObject).Disable();
        else Destroy(gameObject);
    }

    private void Respawn()
    {
        gameObject.SetActive(true);
        transform.position = startPosition;
        if (TryGetComponent(out StateMachine machine)) machine[0].Enter();
        entityActivity.enabled = false;
        transform.rotation = Quaternion.identity;
        health = maxHealth;
    }

}