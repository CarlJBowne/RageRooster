using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using EditorAttributes;
using SLS.StateMachineV3;
using UnityEngine.Animations.Rigging;

public class Boss2HeadStateMachine : MonoBehaviour, IDamagable
{
    public FinalBossHead headID;

    public int damageUntilNewAttack;
    public int attackTimeLower;
    public int attackTimeHigher;
    public ColorTintAnimation damageTint;
    public Color minorDamageTint;
    public Color majorDamageTint;

    [DisableInEditMode, DisableInPlayMode] public string currentState = "NULL";

    public ObjectPool projectilePool;
    [Range(0f, 1f)] public float projectileChance = 1f/3f;


    [HideInEditMode, HideInPlayMode] public Boss2CentralController controller;
    [HideInEditMode, HideInPlayMode] public Animator animator;
    [HideInEditMode, HideInPlayMode] public RigBuilder rigBuilder;
    [HideInEditMode, HideInPlayMode] public int damageTaken;
    [HideInEditMode, HideInPlayMode] public int individualDamageCounter;
    [HideInEditMode, HideInPlayMode] public Timer.Loop attackTimer;



    //public Boss2RigRebuilder rigRebuilder;
    //public GameObject meshCollection;

    public const string idleState = "Idle";
    public const string attack1State = "Attack1";
    public const string attack2State = "Attack2";
    public const string attack3State = "Attack3";
    public const string knockedState = "KnockedOut";
    public const string defendState = "Defend";



    private void Awake()
    {
        animator = GetComponent<Animator>();
        rigBuilder = GetComponent<RigBuilder>();
        controller = GetComponentInParent<Boss2CentralController>();
        attackTimer.rate = Random.Range(attackTimeLower, attackTimeHigher);
    }

    private void OnEnable()
    {
        StartCoroutine(Enum());
        IEnumerator Enum()
        {
            yield return null;
            animator.Rebind();
            rigBuilder.Build();
            animator.Update(0f);
            GoToIdle();
        }
    }


    protected void FixedUpdate()
    {
        projectilePool.Update();
        if (hitStunCoroutine) return;
        if (currentState == "Idle")
        {
            if(headID != FinalBossHead.Stumpy)
            {
                if (controller.Stumpy.currentState != "LavaBreath")
                    attackTimer.Tick(() =>
                    {
                        attackTimer.rate = Random.Range(attackTimeLower, attackTimeHigher);
                        DoAttack((damageTaken < damageUntilNewAttack || Random.Range(0, 3) != 0) ? 1 : 2);
                    });
            }
            else if(damageTaken >= damageUntilNewAttack && 
                (controller.Pecky.currentState == idleState || controller.Pecky.currentState == knockedState) && 
                (controller.Slasher.currentState == idleState || controller.Slasher.currentState == knockedState))
                attackTimer.Tick(() =>
                {
                    attackTimer.rate = Random.Range(attackTimeLower, attackTimeHigher);
                    BeginLavaBreath();
                });

        }
    }


    public bool Damage(Attack attack) 
    {

        if (headID == FinalBossHead.Pecky)
        {
            if (currentState == knockedState) return false;
            if (attack.HasTag("ThrownRock"))
            {
                SetKnockedState(true);
                damageTaken++;
                DoDamageTint(true);
                return true;
            }
            else if (currentState == idleState && attack == "Egg") attackTimer.rate -= 1f;
        }
        else if (headID == FinalBossHead.Slasher)
        {
            if (currentState == knockedState || attack.HasTag("ThrownRock") || !attack.HasTag("FromPlayer")) return false;
            if (currentState == idleState && attack == "Egg") attackTimer.rate -= 1f;
            else if (individualDamageCounter < 5)
            {
                individualDamageCounter++;
                DoDamageTint(false);
                HitStun();
                return true;
            }
            else
            {
                animator.enabled = true;
                damageTaken++;
                DoDamageTint(true);
                SetKnockedState(true);
                individualDamageCounter = 0;
                return true;
            }
        }
        else if (headID == FinalBossHead.Stumpy)
        {
            if (currentState == "Charging" && attack.HasTag(Attack.Tag.Egg))
            {
                individualDamageCounter++;
                DoDamageTint(false);
                if (individualDamageCounter >= 5)
                {
                    animator.SetTrigger("Stun");
                    currentState = "Stunned";
                    individualDamageCounter = 0;
                }
                return true;
            }
            if (currentState == "Vulnerable")
            {
                if (individualDamageCounter < 5)
                {
                    HitStun();
                    individualDamageCounter++;
                    DoDamageTint(false);
                    return true;
                }
                else if (attack.HasTag(Attack.Tag.Wham))
                {
                    animator.enabled = true;
                    damageTaken++;
                    DoDamageTint(true);
                    controller.Damage(new Attack(1, "FromPlayer", "OnWeakSpot"));
                    individualDamageCounter = 0;
                    controller.VulnerableReturn();
                    return true;
                }
            }
        }

        return false;
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

    public void SpawnProjectile()
    {
        
        if(headID == FinalBossHead.Pecky && Random.Range(0f, 1f) <= projectileChance)
        {
            PoolableObject projectile = projectilePool.Pump();
            if(projectile != null)
            {
                projectile.transform.position -= Direction.front;
                projectile.rb.velocity = Direction.upBack;
            }
        }
        else if(headID == FinalBossHead.Slasher)
        {
            PoolableObject projectile = projectilePool.Pump();
            projectile.transform.localScale = .1f * Direction.one;
        }
        else if (headID == FinalBossHead.Stumpy)
        {

        }
    }


    [Button]
    public void TestKnockOut() => SetKnockedState(true);

    public void BeginLavaBreath()
    {
        currentState = "Charging";
        animator.CrossFade("LavaBreath", 0.2f);
    }

    private void HitStun()
    {
        if (hitStunCoroutine) hitStunCoroutine.StopAuto();
        hitStunCoroutine = new(HitStunC(), this);
        IEnumerator HitStunC()
        {
            animator.enabled = false;
            yield return new WaitForSeconds(.5f);
            animator.enabled = true;
        }
    }
    private CoroutinePlus hitStunCoroutine;

    public void EnableHead()
    {
        enabled = true;
        animator.enabled = true;
        //meshCollection.SetActive(true);
        GoToIdle();
    }


    public void DoDamageTint(bool major = true)
    {
        if (damageTint == null) return;
        damageTint.SetTintColor(major ? majorDamageTint : minorDamageTint);
        damageTint.BeginAnimation();
    }
}

public enum FinalBossHead {Pecky, Slasher, Stumpy}