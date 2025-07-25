using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace AssetImportPipeline
{
    public class Toolbar : EditorWindow
    {
        const string AssetImportPipeline = "Asset Import Pipeline";
        const string menuItem = RageRoosterTooling.Constants.RibbonRoot + AssetImportPipeline;

        [MenuItem(menuItem)]
        public static void ShowWindow()
        {
            GetWindow<Toolbar>(AssetImportPipeline);
        }

        private void OnGUI() // Built-in override.
        {
            CreateLabel("Static Mesh");
            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Import New Asset"))
            {
                Debug.Log("Importing a new static mesh!");
                StaticMeshWindow.ShowWindow();
            }
            if (GUILayout.Button("Update Existing Asset"))
            {
                Debug.Log("Updating an existing static mesh!");
            }
            GUILayout.EndHorizontal();
        }


        private void CreateLabel(string labelText)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label(labelText, labelStyle);
        }
    }
}
