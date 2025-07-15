using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectRestructuring 
{
public class PRAssetPostprocessor : AssetPostprocessor
{
    private void OnPreprocessModel()
    {
        Debug.Log("Preprocessing a model now.");
    }
}
}