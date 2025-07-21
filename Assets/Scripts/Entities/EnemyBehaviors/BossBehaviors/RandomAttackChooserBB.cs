using System.Collections.Generic;
using UnityEngine;
using SLS.StateMachineV3;

public class RandomAttackChooserBB : StateBehavior
{

    public Timer.OneTime chooseTimer;
    public State[] choiceStates;
    public float[] choiceChances;

    public override void OnFixedUpdate()
    {
        chooseTimer.Tick(() =>
        {
            float combinedChance = 0;
            for (int i = 0; i < choiceChances.Length; i++) combinedChance += choiceChances[i];

            float diceRoll = Random.Range(0f, combinedChance);

            int choice = 0;
            float passedChoiced = 0;
            for (; choice < choiceChances.Length-1;)
            {
                if(diceRoll < choiceChances[choice + 1] + passedChoiced)
                {
                    break;
                }

                choice++;
                passedChoiced += choiceChances[choice];
            }
            choiceStates[choice].TransitionTo();
        });
    }

    public override void OnEnter(State prev, bool isFinal)
    {
        chooseTimer.Begin();
    }



}