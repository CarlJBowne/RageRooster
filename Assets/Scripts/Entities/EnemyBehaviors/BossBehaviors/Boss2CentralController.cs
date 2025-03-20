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

    private void Awake()
    {
        
    }

    private void FixedUpdate()
    {
        testTimer.Tick(() => { Pecky.attack1State.TransitionTo(); });
    }

}
