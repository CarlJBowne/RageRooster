using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditorInternal;
using UnityEngine;
using System.Reflection;
using Newtonsoft.Json.Linq;
using System.IO;

public class AssembliesManager
{


    public class AssembliesPostProcessor : AssetPostprocessor
    {
        private static bool doThis = false;

        private static void OnPostprocessAllAssets(string[] importedAssets, string[] deletedAssets, string[] movedAssets, string[] movedFromAssetPaths)
        {
            if(!doThis) return; //This completely doesn't work because the Assembly Definitions eventually become unable to be edited.
            AssemblyDefinitionAsset.Loaded = new(); 
            
            string[] foldersToCheck = {"Assets/Scripts"};

            string[] guids = AssetDatabase.FindAssets("t:AssemblyDefinitionAsset", foldersToCheck);

            List<string> bottomAssemblies = new();
            List<AssemblyDefinitionAsset> topAssemblies = new();

            for (int i = 0; i < guids.Length; i++)
            {
                string tryPath = AssetDatabase.GUIDToAssetPath(guids[i]);
                if (tryPath.StartsWith("Assets/Scripts/UtilityCore") || tryPath.StartsWith("Assets/Scripts/Services")) 
                    bottomAssemblies.Add(guids[i]);
                else topAssemblies.Add(AssemblyDefinitionAsset.Load(guids[i]));

            }

            for (int i = 0; i < topAssemblies.Count; i++)
            {
                for (int j = 0; j < bottomAssemblies.Count; j++)
                {
                    topAssemblies[i].References.Add(bottomAssemblies[j]);
                }
                topAssemblies[i].Save();
            }
        }
    }
}