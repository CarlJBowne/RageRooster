using EditorAttributes;
using SLS.EditorUtilities.ComponentHeaders;
using SLS.StateMachineH;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using SLS.ObjectUtilities;

public class EnemyHealth : Health
{
    #region Config

    public float stunTime;
    public GameObject poofPrefab;
    [System.Obsolete]
    public Behaviour[] stunComponents;
    [SerializeField, HeaderItem(true)] EntityActivity entityActivity;
    [SerializeField, HeaderItem()] SLS.StateMachineH.StateMachine hierarchicalMachine;
    [SerializeField, HeaderItem()] Unity.VisualScripting.StateMachine visualMachine;
    [SerializeField, HeaderItem(true)] EnemyLootSpawner enemyLootSpawner;

    [HeaderItem, SerializeField] ColorTintAnimation tintAnimator;
    [HeaderItem, SerializeField] RagdollHandler ragdoll;

    public UltEvents.UltEvent onDamageEvent;

    private bool damaged;


    #endregion Config


    private void Reset() => HeaderItemAttribute.Reset(this);

    protected override void Awake()
    {
        base.Awake();
        if (TryGetComponent(out Spawnable pool))
        {
            pool.onDeactivate += OnSpawn;
        }
        enemyLootSpawner = GetComponent<EnemyLootSpawner>();
    }

    #region DamageOverrides

    protected override bool OverrideDamageable(Attack attack) => attack != Attack.Tags.Enemy || attack == Attack.Tags.FriendlyFire;

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);

        if (ragdoll && ragdoll.State != RagdollHandler.States.Off) ragdoll.SetVelocity(attack.velocity);
        else if (Current != 0)
        {
            Stun(attack);
            if (tintAnimator) tintAnimator.BeginAnimation();
        }

        if (!damaged)
        {
            damaged = true;
            if(Spawnable.IsASpawnable(gameObject, out Spawnable spawnable)) 
                spawnable.SetAlterations(()=>
                {
                    damagable = false;
                    InstantFill() ;
                });
        }
    }

    protected override void OnDeplete(Attack attack)
    {
        base.OnDeplete(attack);
        if (visualMachine) visualMachine.enabled = false;
        if (attack == Attack.Tags.Wham)
        {
            Coroutine.Stop(ref stunRoutine);
            if (ragdoll)
            {
                ragdoll.State = RagdollHandler.States.Thrown;
                ragdoll.SetVelocity(attack.velocity);
            }
            else Destroy();
        }
        else
        {
            Stun(attack);
            if (tintAnimator) tintAnimator.BeginAnimation();
        }
        damaged = false;
    }

    void Stun(Attack attack)
    {
        stunTimeLeft = stunTime * (attack == Attack.Tags.Wham ? 2 : 1);
        Coroutine.Begin(ref stunRoutine, StunEnum(), this, false);

        IEnumerator StunEnum()
        {
            entityActivity.State = EntityActivity.States.Stunned;

            while (stunTimeLeft > 0)
            {
                stunTimeLeft -= Time.deltaTime;
                yield return null;
            }
            entityActivity.State = EntityActivity.States.Default;
            if (Current <= 0)
            {
                if (ragdoll)
                {
                    ragdoll.State = RagdollHandler.States.Ragdoll;
                    ragdoll.SetVelocity(attack.velocity);
                }
                else Destroy();
            }
        }
    }
    private Coroutine stunRoutine;
    private float stunTimeLeft = 0;



    #endregion DamageOverrides


    public override void Destroy()
    {
        Spawnable.DestroyOrDisable(gameObject);
        if (poofPrefab) Instantiate(poofPrefab);
    }

    private void OnSpawn()
    {
        if (hierarchicalMachine) hierarchicalMachine[0].Enter();
        if (visualMachine) visualMachine.enabled = true;
        entityActivity.enabled = true;
        InstantFill();
        if (ragdoll) ragdoll.State = RagdollHandler.States.Off;
    }

}