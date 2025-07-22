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

        private void OnGUI() // Built-in override.
        {
            if (GUILayout.Button("Move Asset Test")) // Condition creates a button and responds to it being pressed.
            {
                DependencyFinder finder = new DependencyFinder();
                foreach (string i in finder.GetPrefabsToSort("Assets"))
                {
                    finder.OrganizeDependencies(i);
                }
            }
        }
    }
}
