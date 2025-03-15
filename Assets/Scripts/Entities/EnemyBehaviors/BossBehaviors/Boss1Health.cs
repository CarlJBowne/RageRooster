using UnityEngine;
using SLS.StateMachineV3;
using EditorAttributes;

public class Boss1Health : Health
{

    public int phase2Trigger;
    public UltEvents.UltEvent phase2Event;
    public UltEvents.UltEvent phase3Event;
    public State phase1IdleState;
    public State jumpState;
    public State phase1Charge;
    public State phase3Charge;
    public Transform phase2StartPos;
    public Transform phase3StartPos;

    [HideInEditMode] public int bossPhase = 1;
    private bool phase2TriggerTriggered;
    private int stunCounter = 0;
    private Animator animator;
    private MovementAnimator moveAnim;
    private Vector3 respawnPoint;

    protected override void Awake()
    {
        base.Awake();
        animator = GetComponent<Animator>();
        moveAnim = GetComponent<MovementAnimator>();
        respawnPoint = transform.position;
    }

    protected override bool OverrideDamageable(Attack attack)
    {
        if (attack.HasTag("OnWeakSpot") && attack.HasTag("GroundSlam")) return true;
        if (attack.HasTag("InEyes") && attack.HasTag("Egg"))
        {
            stunCounter++;
            if (stunCounter == 3) Enrage();
        }
        return false;
    }


    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);

        if (!phase2TriggerTriggered && GetCurrentHealth() <= phase2Trigger) BeginPhase2();
    }

    public void BeginPhase2()
    {
        moveAnim.SetTarget(phase2StartPos);
        jumpState.TransitionTo();
        phase2TriggerTriggered = true;
        damagable = false;
        bossPhase = 2;
    }

    public void BeginPhase3()
    {
        moveAnim.SetTarget(phase3StartPos);
        jumpState.TransitionTo();
        damagable = true;
        bossPhase = 3;
    }
    public void DoPhaseLand()
    {
        Transform dest = bossPhase switch
        {
            2 => phase2StartPos,
            3 => phase3StartPos,
            _ => null
        };
        GetComponent<Rigidbody>().MovePosition(dest.position + Vector3.up * 10f);
    }
    public void EndPhaseLand()
    {
        (bossPhase switch
        {
            2 => phase2Event,
            3 => phase3Event,
            _ => null
        })?.Invoke();
        moveAnim.SetTarget(Gameplay.Player.transform);
    }

    public void Enrage()
    {
        stunCounter = 0;
        (bossPhase switch { 
            1 => phase1Charge, 
            3 => phase3Charge, 
            _ => null 
        }).TransitionTo();
    }

    public void ResetBoss()
    {
        GetComponent<Rigidbody>().MovePosition(respawnPoint);
        gameObject.SetActive(false);
        health = maxHealth;
        phase2TriggerTriggered = false;
        phase1IdleState.TransitionTo();
    }



}