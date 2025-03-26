using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using SLS.StateMachineV3;

public class Boss2HeadStateMachine : MonoBehaviour, IDamagable
{
    public FinalBossHead headID;

    public int damageUntilNewAttack;
    public int attackTimeLower;
    public int attackTimeHigher;

    [DisableInEditMode, DisableInPlayMode] public string currentState = "NULL";




    [HideInEditMode, HideInPlayMode] public Boss2CentralController controller;
    [HideInEditMode, HideInPlayMode] public Animator animator;
    [HideInEditMode, HideInPlayMode] public int damageTaken;
    [HideInEditMode, HideInPlayMode] public int individualDamageCounter;
    [HideInEditMode, HideInPlayMode] public Timer.Loop attackTimer;

    public const string idleState = "Idle";
    public const string attack1State = "Attack1";
    public const string attack2State = "Attack2";
    public const string attack3State = "Attack3";
    public const string knockedState = "KnockedOut";
    public const string defendState = "Defend";


    public bool Damage(Attack attack)
    {

        if(headID == FinalBossHead.Pecky)
        {
            if (currentState == knockedState) return false;
            if (attack.HasTag("ThrownRock"))
            {
                SetKnockedState(true);
                damageTaken++;
            }
        }
        else if(headID == FinalBossHead.Slasher)
        {
            if (currentState == knockedState) return false;
            if (individualDamageCounter < 5)
            {
                individualDamageCounter++;
                return true;
            }
            else if (attack.HasTag(Attack.Tag.Wham))
            {
                damageTaken++;
                SetKnockedState(true);
                individualDamageCounter = 0;
                return true;
            }
        }
        else if(headID == FinalBossHead.Stumpy)
        {
            if(currentState == "Attack2" && attack.HasTag(Attack.Tag.Egg))
            {
                individualDamageCounter++;
                if(individualDamageCounter >= 10)
                {
                    //DO STUN?
                }
                return true;
            }
            if(currentState == "Vulnerable")
            {
                if (individualDamageCounter < 5)
                {
                    animator.Play("DamagePause");
                    individualDamageCounter++;
                    return true;
                }
                else if (attack.HasTag(Attack.Tag.Wham))
                {
                    damageTaken++;
                    controller.Damage(new Attack(1, "FromPlayer", "OnWeakSpot"));
                    individualDamageCounter = 0;
                    controller.VulnerableReturn();
                    return true;
                }
            }
        }

        return false;
    }

    private void Awake()
    {
        animator = GetComponent<Animator>();
        controller = GetComponentInParent<Boss2CentralController>();
        attackTimer.rate = Random.Range(attackTimeLower, attackTimeHigher);
    }
    protected void FixedUpdate()
    {
        if (currentState == "Idle")
        {
            attackTimer.Tick(() =>
            {
                attackTimer.rate = Random.Range(attackTimeLower, attackTimeHigher);

                DoAttack(damageTaken < damageUntilNewAttack || Random.Range(0, 2) == 1 ? 1 : 2);
            });
        }
    }


    #region Controls (Animator Centric)

    public void SetState(string name) => currentState = name;
    public void HasReturnedToIdle()
    {
        currentState = idleState;
        individualDamageCounter = 0;
    }

    public void DoAttack(int number)
    {
        animator.SetTrigger($"Attack{number}");
        currentState = $"Attack{number}";
    }

    public void SetKnockedState(bool knocked)
    {
        if (knocked)
        {
            animator.CrossFade($"Knocked_{headID}", 0.2f);
            currentState = "KnockedOut";
            controller.CheckIfBothKnocked();
        }
        else
        {
            animator.SetTrigger("Next");
        }
    }

    public void SetNext() => animator.SetTrigger("Next");

    public void GoToIdle()
    {
        currentState = idleState;
        individualDamageCounter = 0;
        animator.CrossFade($"Idle_{headID}", .2f);
    }

    #endregion


    [Button]
    public void TestKnockOut() => SetKnockedState(true);


}

public enum FinalBossHead {Pecky, Slasher, Stumpy}