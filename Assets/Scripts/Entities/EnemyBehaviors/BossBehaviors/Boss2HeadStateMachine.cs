using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;

public class Boss2HeadStateMachine : StateMachine, IDamagable
{
    public FinalBossHead headID;

    public int damageUntilNewAttack;
    public int attackTimeLower;
    public int attackTimeHigher;

    public State idleState;
    public State guardingState;
    public State knockedState;
    public State attack1State;
    public State attack2State;

    [HideInInspector] public Boss2CentralController controller;
    [HideInInspector] public Animator animator;
    [HideInInspector] public int damageTaken;
    [HideInInspector] public int individualDamageCounter;
    [HideInInspector] public Timer.Loop attackTimer;


    public bool Damage(Attack attack)
    {

        if(headID == FinalBossHead.Pecky)
        {
            if (knockedState) return false;
            if (attack.HasTag("ThrownRock"))
            {
                knockedState.TransitionTo();
                damageTaken++;
            }
        }
        else if(headID == FinalBossHead.Slasher)
        {
            if (knockedState) return false;
            if (individualDamageCounter < 5)
            {
                individualDamageCounter++;
                return true;
            }
            else if (attack.HasTag("Wham"))
            {
                damageTaken++;
                knockedState.TransitionTo();
                individualDamageCounter = 0;
                return true;
            }
        }
        else if(headID == FinalBossHead.Stumpy)
        {
            if(attack2State && attack.HasTag("Egg"))
            {
                individualDamageCounter++;
                if(individualDamageCounter >= 10)
                {
                    knockedState.TransitionTo();
                }
                return true;
            }
        }

        return false;
    }

    protected override void Initialize()
    {
        animator = GetComponent<Animator>();
        controller = GetComponentInParent<Boss2CentralController>();
    }

    protected override void FixedUpdate()
    {
        base.FixedUpdate();
        if (idleState)
        {
            attackTimer.Tick(() =>
            {
                attackTimer.rate = Random.Range(attackTimeLower, attackTimeHigher);

                (damageTaken < damageUntilNewAttack || Random.Range(1, 4) < 3 
                    ? attack1State 
                    : attack2State)
                    .TransitionTo();

            });
        }
    }
}

public enum FinalBossHead {Pecky, Slasher, Stumpy}