using UnityEngine;
using SLS.StateMachineV3;
using EditorAttributes;

public class Boss1Health : Health
{

    public int phase2Trigger;
    public UltEvents.UltEvent phase2Event;
    public UltEvents.UltEvent phase3Event;
    public State phase1Charge;
    public State phase3Charge;

    [HideInEditMode] public int bossPhase = 0;
    private bool phase2TriggerTriggered;
    private int stunCounter = 0;


    protected override bool OverrideDamageable(Attack attack)
    {
        if (attack.HasTag("OnWeakSpot")) return false;
        if (attack.HasTag("InEyes") && attack.HasTag("Egg"))
        {
            stunCounter++;
            if (stunCounter == 3) Stun();
        }
        return true;
    }


    protected override void OnDamage(Attack attack)
    {
        damageEvent?.Invoke(attack.amount);

        if (!phase2TriggerTriggered && GetCurrentHealth() < phase2Trigger) BeginPhase2();
    }

    public void BeginPhase2()
    {
        phase2Event?.Invoke();
        damagable = false;
    }

    public void BeginPhase3()
    {
        phase3Event?.Invoke();
        damagable = true;
        bossPhase = 3;
    }

    public void Stun()
    {
        stunCounter = 0;
    }





}