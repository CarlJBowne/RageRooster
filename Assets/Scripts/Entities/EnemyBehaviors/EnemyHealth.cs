using EditorAttributes;
using SLS.StateMachineV3;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyHealth : Health
{
    public float stunTime;
    public GameObject poofPrefab;
    public Behaviour[] disableComponents;
    public EnemyLootSpawner enemyLootSpawner;

    private Grabbable grabbable;
    private AttackSourceSingle thrownAttackSource;
    private float stunTimeLeft = 0;

    protected override void Awake() 
    { 
        base.Awake();
        startPosition = transform.position;
        TryGetComponent(out grabbable);
        if (TryGetComponent(out thrownAttackSource)) thrownAttackSource.enabled = false;
        grabbable.GrabStateEvent.AddListener(OnGrabState);
        enemyLootSpawner = FindObjectOfType<EnemyLootSpawner>();
    }


    #region Damage

    protected override bool OverrideDamage(Attack attack) => attack.tags.Contains(Attack.Tag.FromEnemy) && !attack.tags.Contains(Attack.Tag.FriendlyFire);

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);

        if(stunTimeLeft == 0) stunEnum = StartCoroutine(StunEnum());
        stunTimeLeft += stunTime * (attack.HasTag(Attack.Tag.Wham) ? 2 : 1);
    }

    protected override void OnDeplete(Attack attack)
    {
        depleteEvent?.Invoke();
        if (attack.HasTag(Attack.Tag.Wham))
        {
            StopCoroutine(stunEnum);
            if (ragDoll) Ragdoll(attack);
            else Destroy();
        }
    }

    private Coroutine stunEnum;
    private IEnumerator StunEnum()
    {
        SetCompsActive(false);

        yield return null;
        while (stunTimeLeft > 0)
        {
            stunTimeLeft -= Time.deltaTime;
            yield return null;
        }
        stunTimeLeft = 0;

        if (health <= 0 && !hasRagdolled) Destroy();
        else SetCompsActive(true);
    }

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

    #endregion Damage

    #region Ragdoll

    [ToggleGroup("Ragdoll", nameof(minRagdollTime), nameof(maxRagdollTime), nameof(minRagdollVelovity))]
    public bool ragDoll;

    [HideInInspector] public float minRagdollTime;
    [HideInInspector] public float maxRagdollTime;
    [HideInInspector] public float minRagdollVelovity;

    [HideInInspector] public bool projectile;
    [HideInInspector] public bool hasRagdolled;
    private bool hasHitSomething;
    private Rigidbody rb;
    private float ragDollTimer;

    public void Ragdoll(Attack attack)
    {
        SetRagDoll(true);
        rb.velocity = attack.velocity;
    }

    private void SetRagDoll(bool value)
    {
        if(value == hasRagdolled) return;
        ragDollTimer = 0;
        hasRagdolled = value;
        if (hasRagdolled && thrownAttackSource) thrownAttackSource.enabled = true;
        hasHitSomething = false;
        rb = this.GetOrAddComponent<Rigidbody>();
        rb.isKinematic = !value;
        rb.useGravity = value;
        rb.gameObject.layer = value ? Layers.NonSolid : Layers.Enemy;
        SetCompsActive(!value);
    }

    private void SetCompsActive(bool value)
    {
        if (disableComponents.Length > 0)
            foreach (Behaviour B in disableComponents)
                if (B != null) B.enabled = value;
    }

    private void Update()
    {
        if (hasRagdolled)
        {
            ragDollTimer += Time.deltaTime;
            if (!(projectile && !hasHitSomething))
                if (ragDollTimer > minRagdollTime && (rb.velocity.magnitude < minRagdollVelovity || ragDollTimer > maxRagdollTime))
                    Destroy();
        }
    }

    #endregion Ragdoll


    #region Thrown

    public void Contact(GameObject target)
    {
        if(hasRagdolled) hasHitSomething = true;
    }

    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);
    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);

    private void OnGrabState(bool value)
    {
        if (value)
        {
            health = 0;
            SetCompsActive(false);
        }
        if (hasRagdolled && value) hasRagdolled = false;
    }

    #endregion Thrown

    #region Respawn

    [ToggleGroup("SingleRespawn", nameof(respawnTime))]
    public bool respawn;
    [HideInInspector] public float respawnTime;
    private Vector3 startPosition;

    private void Respawn()
    {
        gameObject.SetActive(true);
        transform.position = startPosition;
        if (TryGetComponent(out StateMachine machine)) machine.TransitionState(machine[0]);
        if (hasRagdolled)
        {
            SetRagDoll(false);
            transform.rotation = Quaternion.identity;
            health = maxHealth;
        }
    }

    #endregion Respawn
}