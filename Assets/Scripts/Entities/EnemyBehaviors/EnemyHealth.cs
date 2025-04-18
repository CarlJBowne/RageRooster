using EditorAttributes;
using SLS.StateMachineV3;
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
    public Behaviour[] disableComponents;
    public EnemyLootSpawner enemyLootSpawner;

    public ColorTintAnimation tintAnimator;
    private RagdollHandler ragdoll;

    public bool respawn;
    public float respawnTime;
    

    #endregion Config
    #region Data

    [HideInEditMode, DisableInPlayMode] public EntityState currentState;
    private CoroutinePlus stunCO;
    private float stunTimeLeft = 0;
    private Vector3 startPosition;


    #endregion Config

    protected override void Awake() 
    { 
        base.Awake();
        startPosition = transform.position;
        if (TryGetComponent(out ragdoll)) ragdoll.GrabStateEvent += SetEntityState;
        enemyLootSpawner = GetComponent<EnemyLootSpawner>();
    }

    #region DamageOverrides

    protected override bool OverrideDamageable(Attack attack) => !attack.tags.Contains(Attack.Tag.FromEnemy) || attack.tags.Contains(Attack.Tag.FriendlyFire);

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);

        if(currentState is EntityState.RagDoll) ragdoll.SetVelocity(attack.velocity);
        else if (currentState is EntityState.Default && health != 0)
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
            CoroutinePlus.Stop(ref stunCO);
            if (ragdoll)
            {
                SetEntityState(EntityState.RagDoll);
                ragdoll.SetVelocity(attack.velocity);
            }
            else Destroy();
        }
        else if (currentState is EntityState.Default)
        {
            Stun(attack);
            if (tintAnimator) tintAnimator.BeginAnimation();
        }

    }

    void Stun(Attack attack)
    {
        if (stunTimeLeft == 0)
        {
            CoroutinePlus.Begin(ref stunCO, StunEnum(), this);
            stunTimeLeft += stunTime * (attack == Attack.Tag.Wham ? 2 : 1);
        }
        else
        {
            stunTimeLeft += stunTime * (attack == Attack.Tag.Wham ? 2 : 1);
        }

        IEnumerator StunEnum()
        {
            SetCompsActive(false);

            yield return null;
            float storedTimeLeft = 0;
            while (stunTimeLeft > 0)
            {
                storedTimeLeft = stunTimeLeft;
                yield return WaitFor.Seconds(storedTimeLeft);
                stunTimeLeft -= storedTimeLeft;
            }
            stunTimeLeft = 0;

            if (health <= 0)
            {
                if (ragdoll)
                {
                    SetEntityState(EntityState.RagDoll);
                    ragdoll.SetVelocity(-transform.forward);
                }
                else Destroy();
            }
            else SetCompsActive(true);
        }
    }


    #endregion DamageOverrides


    public void Destroy()
    {
        if (poofPrefab) Instantiate(poofPrefab);
        if (enemyLootSpawner != null) enemyLootSpawner.SpawnLoot(transform.position);
        if (respawn)
        {
            gameObject.SetActive(false);
            Invoke(nameof(Respawn), respawnTime);
        }
        else if (PoolableObject.Is(gameObject)) PoolableObject.Is(gameObject).Disable();
        else Destroy(gameObject);
    }

    private void SetEntityState(EntityState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        if(ragdoll) ragdoll.SetState(newState);
        switch (newState)
        {
            case EntityState.Default:
                SetCompsActive(true);
                stunTimeLeft = 0;
                break;
            case EntityState.Grabbed:
                SetCompsActive(false);
                break;
            case EntityState.Thrown:

                break;
            case EntityState.RagDoll:
                SetCompsActive(false);
                if (!ragdoll) Destroy();
                break;
        }
    }

    private void SetCompsActive(bool value)
    {
        if (disableComponents.Length > 0)
            foreach (Behaviour B in disableComponents)
                if (B != null) B.enabled = value;
    }

    private void Respawn()
    {
        gameObject.SetActive(true);
        transform.position = startPosition;
        if (TryGetComponent(out StateMachine machine)) machine.TransitionState(machine[0]);
        SetEntityState(EntityState.Default);
        transform.rotation = Quaternion.identity;
        health = maxHealth;
    }

}
public enum EntityState
{
    Default,
    Grabbed,
    Thrown,
    RagDoll
}