using RageRooster.RoomSystem;
using SLS.ISingleton;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "AAAAAA", menuName = "ScriptableObjects/EditorState")]
public class EditorState : SingletonAsset<EditorState>
{

    private int loadFromSavePointID = -2;
    public static int LoadFromSavePointID
    {
        get => Get().loadFromSavePointID;
        set => Get().loadFromSavePointID = value;
    }

    private TransitionDestination editorDestination = TransitionDestination.Default;
    public static TransitionDestination EditorDestination
    {
        get => Get().editorDestination;
        set => Get().editorDestination = value;
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
