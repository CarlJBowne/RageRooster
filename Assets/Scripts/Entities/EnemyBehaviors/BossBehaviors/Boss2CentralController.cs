using EditorAttributes;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations.Rigging;
using UnityEngine.SceneManagement;

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

    [Button]
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


    public void Death()
    {
        Pecky.SetState("Dead");
        Slasher.SetState("Dead");
        Stumpy.SetState("Dead");

        Pecky.animator.CrossFade("Dead", .2f);
        Slasher.animator.CrossFade("Dead", .2f);
        Stumpy.animator.CrossFade("Dead", .2f);


        Enum().Begin(Overlay.OverMenus);
        IEnumerator Enum()
        {
            yield return new WaitForSecondsRealtime(4f);

            yield return Overlay.OverMenus.BasicFadeOutWait(2f);

            yield return ZoneManager.UnloadAll();
            yield return new WaitForSecondsRealtime(.1f);
            Gameplay.DESTROY(areYouSure: true);

            AsyncOperation S = SceneManager.LoadSceneAsync("EndingScene", LoadSceneMode.Single);
            yield return new WaitUntil(() => S.isDone);

            yield return Overlay.OverMenus.BasicFadeInWait(1.5f);
        }

    }

}