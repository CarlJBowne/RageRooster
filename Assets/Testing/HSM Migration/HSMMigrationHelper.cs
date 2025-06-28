using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OLD = SLS.StateMachineV3;
using NEW = SLS.StateMachineH;


public class HSMMigrationHelper : MonoBehaviour
{
    public OLD.StateMachine oldStateMachine;
    public NEW.StateMachine newStateMachine;
    public OLD.State oldState;
    public NEW.State newState;






    public void AddNewVersion()
    {
        if (oldStateMachine != null)
        {
            gameObject.AddComponent<NEW.StateMachine>();


        }
        else if ( oldState != null )
        {
            gameObject.AddComponent<NEW.State>();


        }
    }















}

public static class EEEEEEEEEEEEEEEEEEEEEEE
{
    public static HSMMigrationHelper AddNewVersion(this OLD.State state)
    {
        var result = state.gameObject.GetOrAddComponent<HSMMigrationHelper>();
        result.oldState = state;
        if(result.oldState is OLD.StateMachine oldSM) result.oldStateMachine = oldSM;
        return result;
    }
}