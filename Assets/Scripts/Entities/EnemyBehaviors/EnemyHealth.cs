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

    [ToggleGroup("Ragdoll", nameof(minRagdollTime), nameof(maxRagdollTime), nameof(minRagdollVelovity))]
    public bool ragDoll;

    [HideInInspector] public float minRagdollTime;
    [HideInInspector] public float maxRagdollTime;
    [HideInInspector] public float minRagdollVelovity;

    [ToggleGroup("SingleRespawn", nameof(respawnTime))]
    public bool respawn;
    [HideInInspector] public float respawnTime;
    

    #endregion Config
    #region Data

    [HideInEditMode, DisableInPlayMode] public EntityState currentState;
    private Grabbable grabbable;
    private Rigidbody rb;
    private CoroutinePlus stunEnum;
    private float stunTimeLeft = 0;
    private float ragDollTimer;
    private Vector3 startPosition;


    #endregion Config

    protected override void Awake() 
    { 
        base.Awake();
        startPosition = transform.position;
        if (TryGetComponent(out grabbable)) grabbable.GrabStateEvent += SetEntityState;
        enemyLootSpawner = FindObjectOfType<EnemyLootSpawner>();
    }

    private void Update()
    {
        if (currentState == EntityState.RagDoll)
        {
            ragDollTimer += Time.deltaTime;
            if (ragDollTimer > minRagdollTime && (rb.velocity.magnitude < minRagdollVelovity || ragDollTimer > maxRagdollTime))
                Destroy();
        }
    }

    #region DamageOverrides

    protected override bool OverrideDamageable(Attack attack) => attack.tags.Contains(Attack.Tag.FromEnemy) && !attack.tags.Contains(Attack.Tag.FriendlyFire);

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);

        if (stunTimeLeft == 0) stunEnum = new(StunEnum(), this);
        stunTimeLeft += stunTime * (attack.HasTag(Attack.Tag.Wham) ? 2 : 1);

        IEnumerator StunEnum()
        {
            SetCompsActive(false);

            yield return null;
            while (stunTimeLeft > 0)
            {
                stunTimeLeft -= Time.deltaTime;
                yield return null;
            }
            stunTimeLeft = 0;

            if (health <= 0)
            {
                if (ragDoll) SetEntityState(EntityState.RagDoll);
                else Destroy();
            }
            else SetCompsActive(true);
        }
    }

    protected override void OnDeplete(Attack attack)
    {
        depleteEvent?.Invoke();
        if (attack.HasTag(Attack.Tag.Wham))
        {
            StopCoroutine(stunEnum);
            if (ragDoll)
            {
                SetEntityState(EntityState.RagDoll);
                rb.velocity = attack.velocity;
            }
            else Destroy();
        }
    }

    #endregion DamageOverrides


    public void Destroy()
    {
        if (poofPrefab) Instantiate(poofPrefab);
        if (enemyLootSpawner != null) enemyLootSpawner.SpawnLoot(transform.position);
        if(respawn)
        {
            gameObject.SetActive(false);
            Invoke(nameof(Respawn), respawnTime);
        }
        if (PoolableObject.Is(gameObject)) PoolableObject.Is(gameObject).Disable();
        else Destroy(gameObject);
    }

    private void SetEntityState(EntityState newState)
    {
        if (currentState == newState) return;
        currentState = newState;
        if (grabbable) grabbable.currentState = newState;
        switch (newState)
        {
            case EntityState.Default:
                ragDollTimer = 0;
                rb = this.GetOrAddComponent<Rigidbody>();
                rb.isKinematic = true;
                rb.useGravity = false;
                rb.gameObject.layer = Layers.Enemy;
                SetCompsActive(true);
                break;
            case EntityState.Grabbed:
                SetCompsActive(false);
                break;
            case EntityState.Thrown:

                break;
            case EntityState.RagDoll:
                ragDollTimer = 0;
                rb = this.GetOrAddComponent<Rigidbody>();
                rb.isKinematic = false;
                rb.useGravity = true;
                rb.gameObject.layer = Layers.NonSolid;
                SetCompsActive(false);
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