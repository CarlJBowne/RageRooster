using UnityEngine;
using SLS.StateMachineH;

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
    public Timer distanceCheckTimer = new(1f, true);

    private int currentDistance;
    private Timer attackTimer = new(100f, true);
    private Transform playerTransform;

    protected override void OnAwake()
    {
        distanceCheckTimer.action = UpdateDistance;
        attackTimer.action = DoAttack;
        playerTransform = Player.Transform;
        for (int i1 = 0; i1 < distances.Length; i1++)
            for (int i2 = 0; i2 < distances[i1].attacks.Length; i2++)
                distances[i1].attacksRandLength += distances[i1].attacks[i2].chance;
    }

    protected override void OnFixedUpdate()
    {
        distanceCheckTimer.Tick();
        attackTimer.Tick();
    }

    protected override void OnEnter(State prev, bool isFinal) => UpdateDistance();

    public void UpdateDistance()
    {
        float checkDistance = (playerTransform.position - transform.position).XZ().magnitude;
        int i = 0;
        for (; i < distances.Length-1; i++) 
            if (checkDistance < distances[i].higherDistance) 
                break;
        currentDistance = i;
        attackTimer.length = distances[currentDistance].timerTime;
    }

    public void DoAttack()
    {
        if (Machine.SignalManager.Locked) return;

        float diceRoll = Random.Range(0f, distances[currentDistance].attacksRandLength);

        int i = 0;
        float passedChances = 0;
        for (; i < distances[currentDistance].attacks.Length - 1;)
        {
            if (diceRoll < distances[currentDistance].attacks[i].chance + passedChances) break;

            i++;
            passedChances += distances[currentDistance].attacks[i].chance;
        }

        Machine.SendSignal(new(distances[currentDistance].attacks[i].signalName, 0));
    }
}