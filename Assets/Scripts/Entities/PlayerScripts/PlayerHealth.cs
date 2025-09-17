using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerHealth : Health
{
    #region Instance Variables

    public float invincibilityTime;
    public State damageState;
    public State damageStateWham;
    public ColorTintAnimation tintAnimator;

    private CoroutinePlus invincibility;
    private new Collider collider;

    #endregion Instance Variables

    #region Instance Methods



    protected override void Awake()
    {
        base.Awake();
        collider = GetComponent<Collider>();
        Global.playerObject = this;
    }

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);
        if (tintAnimator) tintAnimator.BeginAnimation();
        if (health != 0)
        {
            if(Player.Ranged.aimingState) Player.Ranged.ExitAimingAux();
            CoroutinePlus.Begin(ref invincibility, InvinceEnum(invincibilityTime), this);
            damagable = false;
            if (attack.HasTag(Attack.Tag.Pit)) Player.StateMachine.Death(true);
            else if (attack.HasTag(Attack.Tag.Wham)) 
            {
                damageStateWham.Enter();
                Player.MovementBody.UnLand();
                Player.MovementBody.VelocitySet(y: 14);
            }
            else damageState.Enter();
        }
        Global.Update(health);
    }

    protected override void OnHeal(int amount) => Global.Update(health);

    protected override void OnDeplete(Attack attack)
    {
        if(attack == Attack.Tag.Wham)
        {
            damageStateWham.Enter();
            Player.MovementBody.UnLand();
            Player.MovementBody.VelocitySet(y: 14);
        }
        else Player.StateMachine.Death();
    }

    private IEnumerator InvinceEnum(float time)
    {
        yield return new WaitForSeconds(time);
        damagable = true;
        collider.enabled = false;
        collider.enabled = true;

    }

    protected override bool OverrideDamageable(Attack attack) => !ConversationManager.instance.inDialogue && !Upgrades.Active.d_invincibility;

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

    #endregion Instance Methods

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
