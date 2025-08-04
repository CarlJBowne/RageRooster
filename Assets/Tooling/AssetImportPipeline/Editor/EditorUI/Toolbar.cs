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
        int selectedIndex = 0;
        private void OnGUI() // Built-in override.
        {
            List<string> options = new List<string>() { "Static Mesh", "FakeTestOption" };
            selectedIndex = EditorGUILayout.Popup("Asset Type", selectedIndex, options.ToArray());

            if (selectedIndex == 0)
            {
                if (GUILayout.Button("Open 'Static Mesh Import' window"))
                {
                    Debug.Log("Importing a new static mesh!");
                    StaticMeshWindow.ShowWindow();
                }
            }
            else GUILayout.Label("Sorry, this hasn't been implemented.");
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
