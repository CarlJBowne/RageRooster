using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Boss2CentralController : MonoBehaviour
{
    public Boss2HeadStateMachine Pecky;
    public Boss2HeadStateMachine Slasher;
    public Boss2HeadStateMachine Stumpy;
    public Boss2Health health;

    Timer.Loop testTimer = new(10);
    Timer.Loop testTimer2 = new(15);

    private void Start()
    {
        Pecky.animator.Play("Idle_Pecky");
        Slasher.animator.Play("Idle_Slasher");
        Stumpy.animator.Play("Idle_Stumpy");
    }

    private void FixedUpdate()
    {
        testTimer.Tick(() => { Pecky.attack1State.TransitionTo(); });
        testTimer2.Tick(() => 
        { 
            Slasher.attack1State.TransitionTo(); 
        });
    }

}
