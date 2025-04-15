using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AAAAAA", menuName = "ScriptableObjects/EditorState")]
public class EditorState : SingletonScriptable<EditorState>
{

    private int loadFromSavePointID = -2;
    public static int LoadFromSavePointID
    {
        get => Get().loadFromSavePointID;
        set => Get().loadFromSavePointID = value;
    }

    public enum OnBuildStateMachineHandling
    {
        DoNothing,
        SetupIfNotSetup,
        SetupIfNotSetupAndSave,
        SetupRegardless,
        SetupRegardlessAndSave
    }
    public OnBuildStateMachineHandling onBuildStateMachineHandling;

}
