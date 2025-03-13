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
    private int currentDistance;

    public Timer.Loop timer;
    private Transform playerTransform;

    public override void OnAwake()
    {
        playerTransform = Gameplay.Player.transform;
        for (int i = 0; i < distances.Length; i++)
            for (int j = 0; j < distances[i].attacks.Length; j++)
                distances[i].attacksRandLength += distances[i].attacks[j].chance;
    }

    public override void OnFixedUpdate()
    {
        timer.Tick(() =>
        {
            float activeDistance = (playerTransform.position - transform.position).XZ().magnitude;

            if (activeDistance > distances[currentDistance].higherDistance)
            {
                do currentDistance++;
                while (activeDistance > distances[currentDistance].higherDistance);
                timer.rate = distances[currentDistance].timerTime;
            }
            if (currentDistance != 0 && activeDistance <= distances[currentDistance-1].higherDistance)
            {
                do currentDistance--;
                while (currentDistance != 0 && activeDistance <= distances[currentDistance - 1].higherDistance);
                timer.rate = distances[currentDistance].timerTime;
            }

            if (!Machine.signalReady) return;

            float diceRoll = Random.Range(0f, distances[currentDistance].attacksRandLength);

            int i = 0;
            float passedChances = 0;
            for (; i < distances[currentDistance].attacks.Length-1; )
            {
                if (diceRoll < distances[currentDistance].attacks[i + 1].chance) break;

                i++;
                passedChances += distances[currentDistance].attacks[i].chance;
            }

            Machine.SendSignal(distances[currentDistance].attacks[i].signalName, addToQueue: false);

        });
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        timer.rate = distances[0].timerTime;
    }
}