using SLS.StateMachineV3;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHealth : Health
{
    public float invincibilityTime;
    public State damageState;
    public State damageStateWham;

    private Coroutine invincibility;
    private new Collider collider;
    private UIHUDSystem UI;
    private PlayerMovementBody body;
    private PlayerStateMachine machine;

    protected override void Awake()
    {
        base.Awake();
        collider = GetComponent<Collider>();
        UIHUDSystem.TryGet(out UI);
        TryGetComponent(out body);
        TryGetComponent(out machine);
        Global.playerObject = this;
    }

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);
        if(health != 0)
        {
            invincibility = StartCoroutine(InvinceEnum(invincibilityTime));
            damagable = false;
            if (attack.HasTag(Attack.Tag.Pit)) Gameplay.SpawnPlayer();
            if (!attack.HasTag(Attack.Tag.Wham))
                damageState.TransitionTo();
            else
            {
                damageStateWham.TransitionTo();
                body.GroundStateChange(false);
                body.VelocitySet(y: 14);
            }
        }
        UpdateHealth();
    }

    protected override void OnHeal(int amount) => UpdateHealth();

    protected override void OnDeplete(Attack attack) => UI.StartCoroutine(DeathEnum());

    private IEnumerator InvinceEnum(float time)
    {
        yield return new WaitForSeconds(time);
        damagable = true;
        collider.enabled = false;
        collider.enabled = true;

    }

    public void UpdateHealth() => Global.Update(health);

    private IEnumerator DeathEnum()
    {
        gameObject.SetActive(false);

        yield return new WaitForSeconds(2);

        Gameplay.SpawnPlayer();
        gameObject.SetActive(true);
        health = maxHealth;
        UpdateHealth();
    }

    protected override void OverrideDamageValue(ref Attack attack)
    {
        if (attack.amount < 1) return;
        attack.amount = 1;
        for (int i = 0; i < attack.tags.Length; i++)
        {
            string iTag = attack.tags[i];
            if (iTag[0] == 'P' && 
                iTag.StartsWith("PlayerPoints=") && 
                int.TryParse(iTag[13..], out int result))
            {
                attack.amount = result;
                break;
            }
        }
    }

    public static class Global
    {
        public static int currentHealth;
        public static int maxHealth;

        public static PlayerHealth playerObject;
        public static UIHUDSystem UI;

        public static void Update(int current)
        {
            currentHealth = current;
            playerObject.health = current;

            UI.UpdateHealth(current, maxHealth);
        }
        public static void UpdateMax(int max)
        {
            currentHealth = max;
            maxHealth = max;

            playerObject.health = max;
            playerObject.maxHealth = max;
            UI.UpdateHealth(max, max);

            GlobalState.maxHealth = max;
        }

    }
}
