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
            if (GUILayout.Button("Test Button")) // Condition creates a button and responds to it being pressed.
            {
                Debug.Log("Button has been pressed!");
            }
        }
    }
}
