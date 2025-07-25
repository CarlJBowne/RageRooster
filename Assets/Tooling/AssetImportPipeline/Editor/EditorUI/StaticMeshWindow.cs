using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;


namespace AssetImportPipeline
{
    public class StaticMeshWindow : EditorWindow
    {
        static StaticMeshWindow window; // To avoid any potential lifecycle issues with the window. Hopefully not necessary?
        StaticMesh staticMesh = new StaticMesh();
        public static void ShowWindow()
        {
            window = GetWindow<StaticMeshWindow>("Importing Asset (Example)");
        }


        void OnGUI()
        {
            GUILayout.Label("Model Path(Must be .fbx!!)");

            GUILayout.Label(staticMesh.model.assetPath);

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse..."))
            {
                string path = EditorUtility.OpenFilePanel("Select File", "", "fbx");
                if (!string.IsNullOrEmpty(path))
                {
                    staticMesh.model.assetPath = path;
                }
            }
            if (GUILayout.Button("Clear")) staticMesh.model = new Model();
            GUILayout.EndHorizontal();



            if (GUILayout.Button("Confirm final import"))
            {
                bool confirm = EditorUtility.DisplayDialog(
                    "Confirm Import",
                    "Good work! All strictly necessary information has been entered. :)\n\nAre you ready to import this asset, or would you like to make more adjustments?",
                    "I'm ready",
                    "Go Back"
                );

                if (confirm)
                {
                    Close();
                }
            }
        }
    }


}