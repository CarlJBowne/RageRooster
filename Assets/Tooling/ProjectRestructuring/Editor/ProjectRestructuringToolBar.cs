using System.Collections;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace ProjectRestructuring
{
    public class ProjectRestructuringToolBar : EditorWindow
    {
        [MenuItem("Rage Rooster Tooling/Project Restructuring/Open Tool UI")]
        public static void ShowWindow()
        {
            GetWindow<ProjectRestructuringToolBar>("Project Restructuring");
        }

        DependencyFinder finder = new DependencyFinder();

        private void OnGUI() // Built-in override.
        {
            if (GUILayout.Button("Test Button")) // Condition creates a button and responds to it being pressed.
            {
                Debug.Log("The Test Button has been pressed!");
            }
            if (GUILayout.Button("Move Asset Test")) // Condition creates a button and responds to it being pressed.
            {
                foreach (string i in finder.GetPrefabsToSort())
                {
                    finder.OrganizeDependencies(i);
                }
            }
        }
    }
}
