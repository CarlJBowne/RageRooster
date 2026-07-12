using EditorAttributes;
using SLS.StateMachineH;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using Utilities.ObjectPooling;
using static UnityEngine.Rendering.DebugUI;

public class EnemyHealth : Health
{
    #region Config

    public float stunTime;
    public GameObject poofPrefab;
    [System.Obsolete]
    public Behaviour[] stunComponents;
    [SerializeField, RelatedComponent(true)] EntityActivity entityActivity;
    [SerializeField, RelatedComponent()] SLS.StateMachineH.StateMachine hierarchicalMachine;
    [SerializeField, RelatedComponent()] Unity.VisualScripting.StateMachine visualMachine;
    [SerializeField, RelatedComponent(true)] EnemyLootSpawner enemyLootSpawner;

    [RelatedComponent, SerializeField] ColorTintAnimation tintAnimator;
    [RelatedComponent, SerializeField] RagdollHandler ragdoll;

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
        else if (health != 0)
        {
            Stun(attack);
            if (tintAnimator) tintAnimator.BeginAnimation();
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
            if (health <= 0)
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
        gameObject.SetActive(true);
        transform.position = startPosition;
        if (hierarchicalMachine) hierarchicalMachine[0].Enter();
        if (visualMachine) visualMachine.enabled = true;
        entityActivity.enabled = true;
        transform.rotation = Quaternion.identity;
        health = maxHealth;
        if (ragdoll) ragdoll.State = RagdollHandler.States.Off;
    }

}