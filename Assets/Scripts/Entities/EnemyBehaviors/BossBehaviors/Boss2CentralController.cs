using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss2CentralController : MonoBehaviour
{
    public Boss2HeadStateMachine Pecky;
    public Boss2HeadStateMachine Slasher;
    public Boss2HeadStateMachine Stumpy;
    public Boss2Health health;

    public UltEvents.UltEvent ResetBossEvent;
    public UltEvents.UltEvent FinishBossEvent;

    private void Start()
    {
        Pecky.animator.Play("Idle_Pecky");
        Slasher.animator.Play("Idle_Slasher");
        Stumpy.animator.Play("Idle_Stumpy");
    }

    private void FixedUpdate()
    {

    }

    public void ResetBoss() => ResetBossEvent?.Invoke();

    public void FinishBoss() => FinishBossEvent?.Invoke();

}
