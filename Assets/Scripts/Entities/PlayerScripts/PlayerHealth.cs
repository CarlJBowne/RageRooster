using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHealth : Health
{
    public float invincibilityTime;

    private Coroutine invincibility;
    private new Collider collider;
    private UIHUDSystem UI;
    private Rigidbody rb;
    private PlayerStateMachine machine;


    protected override void Awake()
    {
        base.Awake();
        collider = GetComponent<Collider>();
        UIHUDSystem.TryGet(out UI);
        TryGetComponent(out rb);
        TryGetComponent(out machine);
        UpdateHealth();
    }

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);
        if(health != 0)
        {
            invincibility = StartCoroutine(InvinceEnum(invincibilityTime));
            damagable = false;
            if (attack.name == "Pit") Gameplay.SpawnPlayer();
        }
        UpdateHealth();
    }

    protected override void OnDeplete(Attack attack) => UI.StartCoroutine(DeathEnum());

    private IEnumerator InvinceEnum(float time)
    {
        yield return new WaitForSeconds(time);
        damagable = true;
        collider.enabled = false;
        collider.enabled = true;

    }

    public void UpdateHealth() => UI.UpdateHealth(health, maxHealth);

    public void AddMaxHealth(int value = 1)
    {
        maxHealth += value;
        UpdateHealth();
    }

    private IEnumerator DeathEnum()
    {
        gameObject.SetActive(false);

        yield return new WaitForSeconds(2);

        Gameplay.SpawnPlayer();
        gameObject.SetActive(true);
        health = maxHealth;
        UpdateHealth();
    }

}
