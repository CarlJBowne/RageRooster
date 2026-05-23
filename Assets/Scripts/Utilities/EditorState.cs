using RageRooster.RoomSystem;
 
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Utilities.Singletons;

[CreateAssetMenu(fileName = "AAAAAA", menuName = "ScriptableObjects/EditorState")]
public class EditorState : GlobalAsset<EditorState>
{

    private int loadFromSavePointID = -2;
    public static int LoadFromSavePointID
    {
        get => Get.loadFromSavePointID;
        set => Get.loadFromSavePointID = value;
    }

    private Destination editorDestination = Destination.Null;
    public static Destination EditorDestination
    {
        get => Get.editorDestination;
        set => Get.editorDestination = value;
    }
    private AreaAsset editorDestinationArea = null;
    public static AreaAsset EditorDestinationArea
    {
        get => Get.editorDestinationArea;
        set => Get.editorDestinationArea = value;
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
