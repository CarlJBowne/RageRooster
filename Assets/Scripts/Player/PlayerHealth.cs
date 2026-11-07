using RageRooster.Core.Save;
using SLS.GeneralUtilities.Events;
using SLS.StateMachineH;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace RageRooster.Player
{
    [DefaultExecutionOrder(ExecutionOrders.PlayerSystems)]
    public class PlayerHealth : Health
    {
        protected override IntSyncable MaxSourceStat => Player.Stats.MaxHealth;
        //public static IntSyncableClamped Viewer;

        public float invincibilityTime;
        public State damageState;
        public State damageStateWham;
        public ColorTintAnimation tintAnimator;
        public float inFallDownPitTime = 1;
        public float inDeathTime = 2;

        private Coroutine invincibility;
        private new Collider collider;

        protected override void Awake()
        {
            base.Awake();
            collider = GetComponent<Collider>();
            events.Add(PlayerStats.Active.MaxHealth.Subscribe(SetMaxAndFill));

            //Viewer = new();
            //Viewer &= Current;
            //Viewer.Max = Max;
            //events.Add(OnValueChanged.Subscribe(Viewer.SetValue));
            //events.Add(OnMaxChanged.Subscribe(Viewer.SetMax));
        }

        protected override void OnDamage(Attack attack)
        {
            damageEvent?.Invoke(attack.amount);
            if (tintAnimator) tintAnimator.BeginAnimation();
            if (Current != 0)
            {
                if (Player.StateMachine.Aiming) Player.Ranged.ExitAimingAux();
                Coroutine.Begin(ref invincibility, InvinceEnum(invincibilityTime), this);
                damagable = false;
                if (attack == Attack.Tags.Pit)
                {
                    Player.Respawn();
                    damagable = true;
                }
                else if (attack == Attack.Tags.Wham)
                {
                    damageStateWham.Enter();
                    Player.MovementBody.Anchor.DeAnchor();
                    Player.MovementBody.Velocity.y = 14;
                }
                else damageState.Enter();
            }
        }


        protected override void OnDeplete(Attack attack)
        {
            if (attack == Attack.Tags.Wham)
            {
                damageStateWham.Enter();
                Player.MovementBody.Anchor.DeAnchor();
                Player.MovementBody.Velocity.y = 14;
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

        protected override bool Override(Attack attack)
        {
            if (!base.Override(attack)) return false;

            if (Player.Stats.d_invincibility && attack != Attack.Tags.Pit) return false;

            if (ConversationManager.instance && ConversationManager.instance.inDialogue) return false;

            attack.amount = 1;
            if (attack == Attack.Tags.OnPlayerDouble) attack.amount = 2;
            else if (attack == Attack.Tags.OnPlayerNone)
            {
                attack.amount = 0;
                return false;
            }
            else if (attack == Attack.Tags.OnPlayerTriple) attack.amount = 3;
            else if (attack == Attack.Tags.OnPlayerQuadruple) attack.amount = 4;

            return true;
        }

        public void DoAwake() => Awake();

        public override void Destroy()
        {
            //No.
        }

    }
}