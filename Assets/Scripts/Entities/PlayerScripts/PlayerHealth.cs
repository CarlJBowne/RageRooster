using RageRooster.Systems.SaveSystem;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
public class PlayerHealth : Health
{
    #region Instance Variables

    public float invincibilityTime;
    public State damageState;
    public State damageStateWham;
    public ColorTintAnimation tintAnimator;
    public float inFallDownPitTime = 1;
    public float inDeathTime = 2;

    private CoroutinePlus invincibility;
    private new Collider collider;

    #endregion Instance Variables

    #region Instance Methods



    protected override void Awake()
    {
        base.Awake();
        collider = GetComponent<Collider>();
        Player.Health.updateHealth += HealthChangeCallback;
        Player.Health.updateMaxHealth += MaxHealthChangeCallback;

        //Global.playerObject = this;
    }

    private void OnDestroy()
    {
        Player.Health.updateHealth -= HealthChangeCallback;
        Player.Health.updateMaxHealth -= MaxHealthChangeCallback;
    }

    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);
        if (tintAnimator) tintAnimator.BeginAnimation();
        if (health != 0)
        {
            if (Player.StateMachine.Aiming) Player.Ranged.ExitAimingAux();
            CoroutinePlus.Begin(ref invincibility, InvinceEnum(invincibilityTime), this);
            damagable = false;
            if (attack == Attack.Tags.Pit)
            {
                Player.PitFall();
                damagable = true;
            }
            else if (attack == Attack.Tags.Wham)
            {
                damageStateWham.Enter();
                Player.MovementBody.UnLand();
                Player.MovementBody.VelocitySet(y: 14);
            }
            else damageState.Enter();
        }
        Player.Health.Current = health;
    }

    protected override void OnHeal(int amount) => Player.Health.Current = health;

    protected override void OnDeplete(Attack attack)
    {
        if (attack == Attack.Tags.Wham)
        {
            damageStateWham.Enter();
            Player.MovementBody.UnLand();
            Player.MovementBody.VelocitySet(y: 14);
        }
        else Player.Death();
    }

    private IEnumerator InvinceEnum(float time)
    {
        yield return new WaitForSeconds(time);
        damagable = true;
        collider.enabled = false;
        collider.enabled = true;

    }

    protected override bool OverrideDamageable(Attack attack)
    {
        if (Upgrades.Active.d_invincibility && attack != Attack.Tags.Pit) return false;

        if (ConversationManager.instance && ConversationManager.instance.inDialogue) return false;
        return true;
    }

    protected override void OverrideDamageValue(ref Attack attack)
    {
        if (attack.amount < 1) return;
        attack.amount = 1;
        if (attack == Attack.Tags.OnPlayerDouble) attack.amount = 2;
        else if (attack == Attack.Tags.OnPlayerNone) attack.amount = 0;
        else if (attack == Attack.Tags.OnPlayerTriple) attack.amount = 3;
        else if (attack == Attack.Tags.OnPlayerQuadruple) attack.amount = 4;

        //for (int i = 0; i < attack.oldTags.Length; i++)
        //{
        //    string iTag = attack.oldTags[i];
        //    if (iTag[0] == 'P' && 
        //        iTag.StartsWith("PlayerPoints=") && 
        //        int.TryParse(iTag[13..], out int result))
        //    {
        //        attack.amount = result;
        //        break;
        //    }
        //}
    }


    private void HealthChangeCallback()
    {
        if (health == Player.Health.Current) return;
        health = Player.Health.Current;
        if (health < 1) OnDeplete(default);
    }
    private void MaxHealthChangeCallback()
    {
        if (maxHealth == Player.Health.Max) return;
        maxHealth = Player.Health.Max;
    }

    public void DoAwake() => Awake();


    public override void Destroy()
    {
        //No.
    }

    #endregion Instance Methods
}
