using UnityEngine;
using SLS.StateMachineV3;

public class DistanceAttackChooserBB : StateBehavior
{

    public DistancePhase[] distances;
    [System.Serializable]
    public class DistancePhase
    {
        public float higherDistance;
        public float timerTime;
        public PossibleAttack[] attacks;
        [System.Serializable]
        public struct PossibleAttack
        {
            public string signalName;
            public float chance;
        }
        [HideInInspector] public float attacksRandLength;
    }
    public Timer.Loop distanceCheckTimer;

    private int currentDistance;
    private Timer.Loop attackTimer = new(100f);
    private Transform playerTransform;

    public override void OnAwake()
    {
        playerTransform = Gameplay.Player.transform;
        for (int i1 = 0; i1 < distances.Length; i1++)
            for (int i2 = 0; i2 < distances[i1].attacks.Length; i2++)
                distances[i1].attacksRandLength += distances[i1].attacks[i2].chance;
    }

    public override void OnFixedUpdate()
    {
        distanceCheckTimer.Tick(UpdateDistance);
        attackTimer.Tick(DoAttack);
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        UpdateDistance();
    }

    public void UpdateDistance()
    {
        float checkDistance = (playerTransform.position - transform.position).XZ().magnitude;
        int i = 0;
        for (; i < distances.Length-1; i++) 
            if (checkDistance < distances[i].higherDistance) 
                break;
        if(i != currentDistance)
        {
            currentDistance = i;
            attackTimer.rate = distances[currentDistance].timerTime;
        }
    }

    public void DoAttack()
    {
        if (!Machine.signalReady) return;

        float diceRoll = Random.Range(0f, distances[currentDistance].attacksRandLength);

        int i = 0;
        float passedChances = 0;
        for (; i < distances[currentDistance].attacks.Length - 1;)
        {
            if (diceRoll < distances[currentDistance].attacks[i].chance + passedChances) break;

            i++;
            passedChances += distances[currentDistance].attacks[i].chance;
        }

        Machine.SendSignal(distances[currentDistance].attacks[i].signalName, addToQueue: false);
    }
}