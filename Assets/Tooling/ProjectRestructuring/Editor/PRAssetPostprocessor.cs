using UnityEditor;
using UnityEngine;

namespace ProjectRestructuring 
{
    public class PRAssetPostprocessor : AssetPostprocessor // Not currently being used but leaving it here just in case.
    {
        private void OnPreprocessModel()
        {
            Debug.Log("Preprocessing a model now.");
        }
    }
}