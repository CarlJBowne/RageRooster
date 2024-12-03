using EditorAttributes;
using SLS.StateMachineV2;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyHealth : Health, IAttacker
{
    public float stunTime;
    public GameObject poofPrefab;
    public Behaviour[] disableComponents;

    private Grabbable grabbable;
    private float stunTimeLeft = 0;

    protected override void Awake() 
    { 
        base.Awake();
        startPosition = transform.position;
        TryGetComponent(out grabbable);
        grabbable.GrabStateEvent.AddListener(OnGrabState);
    }


    #region Damage

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
        if (poofPrefab) Instantiate(poofPrefab);
        if (respawn)
        {
            gameObject.SetActive(false);
            Invoke(nameof(Respawn), respawnTime);
        }
        else if (PoolableObject.Is(gameObject)) PoolableObject.Is(gameObject).Disable();
        else Destroy(gameObject);
    }

    #endregion Damage

    #region Ragdoll

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
    private float ragDollTimer;

    public void Ragdoll(Attack attack)
    {
        SetRagDoll(true);
        rb.velocity = (attack.source as MonoBehaviour).transform.TransformDirection(attack.velocity);
    }

    private void SetRagDoll(bool value)
    {
        if(value == hasRagdolled) return;
        ragDollTimer = 0;
        hasRagdolled = value;
        hasHitSomething = false;
        rb = this.GetOrAddComponent<Rigidbody>();
        rb.isKinematic = !value;
        rb.useGravity = value;
        rb.gameObject.layer = value ? Layers.NonSolid : Layers.Enemy;
        if (disableComponents.Length > 0)
            foreach (Behaviour B in disableComponents)
                if (B != null) B.enabled = !value;
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
        if (!hasRagdolled || target.layer == Layers.Player) return;
        hasHitSomething = true;
        (this as IAttacker).BeginAttack(target, thrownAttack, rb.velocity);
    }

    private void OnCollisionEnter(Collision collision) => Contact(collision.gameObject);
    private void OnTriggerEnter(Collider other) => Contact(other.gameObject);

    private void OnGrabState(bool value)
    {
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
        if (TryGetComponent(out StateMachine machine)) machine.TransitionState(machine.topLevelStates[0]);
        if (hasRagdolled)
        {
            SetRagDoll(false);
            transform.rotation = Quaternion.identity;
            health = maxHealth;
        }
    }

    #endregion Respawn
}