using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OLD = SLS.StateMachineV3;
using NEW = SLS.StateMachineH;

public class HSMMigrationMidData : ScriptableObject
{
    public List<HSMMachine> workingMachines;


}

[System.Serializable]
public class HSMMachine : Prefab
{
    public HSMMachine(string path, bool openForEditing = false) : base(path, openForEditing){}

    public HSMMigrationHelper topHelper;

    public int phase = 0;

}
