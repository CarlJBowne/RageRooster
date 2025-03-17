using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EditorState : SingletonScriptable<EditorState>
{

    private int loadFromSavePointID = -2;
    public static int LoadFromSavePointID
    {
        get => Get().loadFromSavePointID;
        set => Get().loadFromSavePointID = value;
    }
}
