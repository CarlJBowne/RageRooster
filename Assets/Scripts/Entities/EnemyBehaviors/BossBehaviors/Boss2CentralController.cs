using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;

public class Boss2CentralController : Health
{
    public Boss2HeadStateMachine Pecky;
    public Boss2HeadStateMachine Slasher;
    public Boss2HeadStateMachine Stumpy;
    

    public UltEvents.UltEvent ResetBossEvent;
    public UltEvents.UltEvent FinishBossEvent;

    void Start() => gameObject.SetActive(false);

    private void OnEnable() => Gameplay.onPlayerRespawn += ResetBoss;

    public void ResetBoss()
    {
        ResetBossEvent?.Invoke();
        Gameplay.onPlayerRespawn -= ResetBoss;
    }

    public void FinishBoss() => FinishBossEvent?.Invoke();

    protected override bool OverrideDamageable(Attack attack)
    {
        return attack.HasTag("FromPlayer") && attack.HasTag("OnWeakSpot");
    }

    public void CheckIfBothKnocked()
    {
        if(Pecky.currentState == Boss2HeadStateMachine.knockedState && Slasher.currentState == Boss2HeadStateMachine.knockedState)
        {
            Invoke(nameof(StartStumpyVulnerable), 3f);
        }
    }
    private void StartStumpyVulnerable()
    {
        Stumpy.animator.CrossFade("Stumpy_Vulnerable", 0.2f);
        Stumpy.currentState = "Vulnerable";
    }

    public void VulnerableReturn()
    {
        Pecky.GoToIdle();
        Slasher.GoToIdle();
        Stumpy.GoToIdle();
    }

}