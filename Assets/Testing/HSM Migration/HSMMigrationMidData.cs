using System.Collections;
using System.Collections.Generic;
using UnityEngine;

using OLD = SLS.StateMachineV3;
using NEW = SLS.StateMachineH;

public class HSMMigrationMidData : ScriptableObject
{

    public List<GameObject> machines = new();
    public List<HSMMigrationHelper> helpers = new();

}
