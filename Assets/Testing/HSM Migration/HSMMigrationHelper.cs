using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OLD = SLS.StateMachineV3;
using NEW = SLS.StateMachineH;


public class HSMMigrationHelper : MonoBehaviour
{
    public OLD.State oldState;
    public NEW.State newState;

    public List<HSMMigrationHelper> children = new();



    public void AddNewVersion(bool isStateMachine = false)
    {
        if (isStateMachine)
        {
            OLD.StateMachine oldStateMachine = GetComponent<OLD.StateMachine>();
            
            if(oldStateMachine.GetType() != typeof(OLD.StateMachine)) 
                gameObject.AddComponent<NEW.StateMachine>();
        }
        else gameObject.AddComponent<NEW.State>();
        foreach (var child in children) child.AddNewVersion();
    }

}

public static class HSMMigrationExtends
{
    public static HSMMigrationHelper AddMigrationHelper(this OLD.State state)
    {
        var result = state.gameObject.GetOrAddComponent<HSMMigrationHelper>();
        result.oldState = state;
        return result;
    }
}