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
            CreateButtonRow(new List<string>() {"Import New Asset", "Update Existing Asset"}, new List<Delegate>());
        }


        private void CreateLabel(string labelText)
        {
            GUIStyle labelStyle = new GUIStyle(GUI.skin.label);
            labelStyle.alignment = TextAnchor.MiddleCenter;
            labelStyle.fontStyle = FontStyle.Bold;
            GUILayout.Label(labelText, labelStyle);
        }

        void CreateButtonRow(List<String> labels, List<Delegate> behaviors)
        {
            GUILayout.BeginHorizontal();
            for (int i = 0; i < labels.Count; i++)
            {
                string label = labels[i];
                if (GUILayout.Button(label)) // Condition creates a button and responds to it being pressed.
                {
                    // if behavior matches, do that.
                    ButtonPlaceholderBehavior();
                }
            }
            GUILayout.EndHorizontal();
        }

        private void ButtonPlaceholderBehavior()
        {
            Debug.Log("Button has been pressed!");
        }
    }
}
