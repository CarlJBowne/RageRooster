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
    public ColorTintAnimation tintAnimator;

    private CoroutinePlus invincibility;
    private new Collider collider;
    private UIHUDSystem UI;
    private PlayerMovementBody body;
    private PlayerStateMachine machine;
    private PlayerRanged ranged;

    protected override void Awake()
    {
        base.Awake();
        collider = GetComponent<Collider>();
        UIHUDSystem.TryGet(out UI);
        TryGetComponent(out body);
        TryGetComponent(out machine);
        TryGetComponent(out ranged);
        Global.playerObject = this;
    }

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);
        if (tintAnimator) tintAnimator.BeginAnimation();
        if (health != 0)
        {
            if(ranged.aimingState) ranged.ExitAimingAux();
            CoroutinePlus.Begin(ref invincibility, InvinceEnum(invincibilityTime), this);
            damagable = false;
            if (attack.HasTag(Attack.Tag.Pit)) machine.Death(true);
            else if (attack.HasTag(Attack.Tag.Wham)) 
            {
                damageStateWham.TransitionTo();
                body.GroundStateChange(false);
                body.VelocitySet(y: 14);
            }
            else damageState.TransitionTo();
        }
        Global.Update(health);
    }

    protected override void OnHeal(int amount) => Global.Update(health);

    protected override void OnDeplete(Attack attack)
    {
        if(attack == Attack.Tag.Wham)
        {
            damageStateWham.TransitionTo();
            body.GroundStateChange(false);
            body.VelocitySet(y: 14);
        }
        else machine.Death();
    }

    private IEnumerator InvinceEnum(float time)
    {
        yield return new WaitForSeconds(time);
        damagable = true;
        collider.enabled = false;
        collider.enabled = true;

    }

    protected override bool OverrideDamageable(Attack attack) => !ConversationManager.instance.inDialogue;

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

        public static void HealToFull() => Update(maxHealth);

    }
}
