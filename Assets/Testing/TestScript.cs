using EditorAttributes;
using FMOD.Studio;
using RageRooster.RoomSystem;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TestScript : MonoBehaviour
{

    public SLS.StateMachineH.SerializedDictionary.SerializedDictionary<string, int> testDict1 = new();
    public SLS.StateMachineH.SerializedDictionary.SerializedDictionary<string, bool> testDict2 = new();
    public SLS.StateMachineH.SerializedDictionary.SerializedDictionary<string, GameObject> testDict3 = new();

}
