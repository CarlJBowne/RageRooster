using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineH;

public class RandomAttackChooserBB : StateBehavior
{

    public float chooseTimer;
    public State[] choiceStates;
    public float[] choiceChances;
    private Timer timer;

    protected override void OnFixedUpdate() => timer.Tick();

    protected override void OnAwake()
    {
        timer = new Timer(chooseTimer, true, () =>
        {
            float combinedChance = 0;
            for (int i = 0; i < choiceChances.Length; i++) combinedChance += choiceChances[i];

            float diceRoll = Random.Range(0f, combinedChance);

            int choice = 0;
            float passedChoiced = 0;
            for (; choice < choiceChances.Length - 1;)
            {
                if (diceRoll < choiceChances[choice + 1] + passedChoiced)
                {
                    break;
                }

                choice++;
                passedChoiced += choiceChances[choice];
            }
            choiceStates[choice].Enter();
        }).StartUpdate();
    }


}