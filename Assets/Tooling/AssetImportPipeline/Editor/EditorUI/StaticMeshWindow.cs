using System;
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
            int spaceSize = 20;
            CreateImportButton("Model Path (must be .fbx!)", staticMesh.model, "fbx");
            GUILayout.Space(spaceSize);
            CreateImportButton("Diffuse path", staticMesh.PbrTextures.DiffuseMap, "png");
            CreateImportButton("Roughness path", staticMesh.PbrTextures.RoughnessMap, "png");
            CreateImportButton("Specular path", staticMesh.PbrTextures.SpecularMap, "png");
            CreateImportButton("Normal path", staticMesh.PbrTextures.NormalMap, "png");
            CreateImportButton("Height path", staticMesh.PbrTextures.HeightMap, "png");
            GUILayout.Space(20);

            GUILayout.BeginHorizontal();
            GUILayout.Label("");
            if (GUILayout.Button("Confirm final import", GUILayout.Width(200)))
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
            GUILayout.Label("");
            GUILayout.EndHorizontal();
        }

        void CreateImportButton(string header, AssetBase asset, string forceType = "")
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(header, GUILayout.Width(150));
            GUILayout.Label(""); // might do "|" or something idk
            GUILayout.Label(asset.assetPath);
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("Browse..."))
            {
                string path = EditorUtility.OpenFilePanel("Select File", "", forceType);
                if (!string.IsNullOrEmpty(path))
                {
                    asset.assetPath = path;
                }
            }
            if (GUILayout.Button("Clear", GUILayout.Width(50))) asset = (AssetBase)Activator.CreateInstance(asset.GetType()); // resets asset
            GUILayout.EndHorizontal();
        }
    }


}