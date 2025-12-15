using EditorAttributes;
using SLS.StateMachineH;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI;

public class EnemyHealth : Health, IEntityComponent
{
    #region Config

    public float stunTime;
    public GameObject poofPrefab;
    [System.Obsolete]
    public Behaviour[] stunComponents;

    [field: SerializeField, RelatedComponent(true)] public Entity Entity { get; set; }
    [SerializeField, RelatedComponent(true)] EnemyLootSpawner enemyLootSpawner;

    [RelatedComponent, SerializeField] ColorTintAnimation tintAnimator;
    [RelatedComponent, SerializeField] RagdollHandler ragdoll;

    public float respawnTime = 0;
    public UltEvents.UltEvent onDamageEvent;


    #endregion Config
    #region Data

    private Vector3 startPosition;


    #endregion Config


    private void Reset()
    {
        ComponentConfig.Reset(this);
        IEntityComponent.Reset(this);
    }

    protected override void Awake() 
    { 
        base.Awake();
        IEntityComponent.Awake(this);
        startPosition = transform.position;
        if (TryGetComponent(out PoolableObject pool))
        {
            pool.onActivate += Respawn;
            respawnTime = 0;
        }
        enemyLootSpawner = GetComponent<EnemyLootSpawner>();
    }

    #region DamageOverrides

    protected override bool OverrideDamageable(Attack attack) => !attack.tags.Contains(Attack.Tag.FromEnemy) || attack.tags.Contains(Attack.Tag.FriendlyFire);

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);

        if (ragdoll && Entity.State is Entity.States.RagDoll) ragdoll.SetVelocity(attack.velocity);
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
                Entity.State = Entity.States.RagDoll;
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
            Entity.State = Entity.States.Stunned;

            while(stunTimeLeft > 0)
            {
                stunTimeLeft -= Time.deltaTime;
                yield return null;
            }
            Entity.State = Entity.States.Default;
            if(health <= 0)
            {
                if (ragdoll)
                {
                    Entity.State = Entity.States.RagDoll;
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
        if (respawnTime > 0)
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
        Entity.State = Entity.States.Default;
        transform.rotation = Quaternion.identity;
        health = maxHealth;
    }

    public void StateChangeReceiver(Entity.States state)
    {
        enabled = state switch
        {
            Entity.States.Inactive => false,
            Entity.States.Default => true,
            Entity.States.Stunned => false,
            Entity.States.Grabbed => false,
            Entity.States.Thrown => false,
            Entity.States.RagDoll => false,
            _ => enabled,
        };
    }
}