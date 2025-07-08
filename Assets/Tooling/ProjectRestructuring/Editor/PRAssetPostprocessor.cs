using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

public class PRAssetPostprocessor : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        Debug.Log("Preprocessing a model now.");
    }
}
